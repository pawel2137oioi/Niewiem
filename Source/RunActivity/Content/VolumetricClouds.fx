// Depth-limited cloud integration with filtered, adaptive two-point quadrature,
// two light samples; no per-frame procedural texture generation.
texture DepthTexture;
texture CloudTexture;
texture3D NoiseTexture;
float4x4 InverseViewProjection;
float3 CameraPosition;
float3 WorldOffset;
float3 SunDirection;
float3 FogColor;
float4 CloudLayer; // bottom, thickness, cloud cover, time
float4 CloudLight; // daylight, clear sky, strength, sun visibility
float CloudTwilight;
float2 CloudSampling; // maximum intervals, desired interval length (metres)
float2 LowPixelSize;
sampler Depth = sampler_state { Texture = (DepthTexture); MinFilter = Point; MagFilter = Point; MipFilter = None; AddressU = Clamp; AddressV = Clamp; };
sampler Clouds = sampler_state { Texture = (CloudTexture); MinFilter = Point; MagFilter = Point; MipFilter = None; AddressU = Clamp; AddressV = Clamp; };
sampler3D CloudNoise = sampler_state { Texture = (NoiseTexture); MinFilter = Linear; MagFilter = Linear; MipFilter = Linear; AddressU = Wrap; AddressV = Wrap; AddressW = Wrap; };
#include "CloudDensity.fxh"
#include "WeatherLighting.fxh"
struct Input { float3 Position : POSITION0; float2 UV : TEXCOORD0; };
struct Output { float4 Position : SV_POSITION; float2 UV : TEXCOORD0; };
Output VS(Input v) { Output o; o.Position = float4(v.Position, 1); o.UV = v.UV; return o; }

float Density(float3 p, float lod)
{
    float h = (p.y - CloudLayer.x) / CloudLayer.y;
    if (h <= 0 || h >= 1) return 0;
    float mass = CloudMassAtLod(p, WorldOffset, CloudLayer.w, lod);
    return CloudAmount(mass, CloudLayer.z) * smoothstep(0, 0.16, h) * (1 - smoothstep(0.70, 1, h));
}

float4 March(Output v) : SV_TARGET
{
    float4 farPoint = mul(float4(v.UV * float2(2,-2) + float2(-1,1), 1, 1), InverseViewProjection);
    float3 ray = normalize(farPoint.xyz / farPoint.w - CameraPosition);
    // A pixel's footprint on the cloud slab grows rapidly at grazing angles.
    // Ray-angle spread alone misses the derivative of the slab intersection.
    float3 rayDx = ddx(ray), rayDy = ddy(ray);
    float safeY = (ray.y >= 0 ? 1 : -1) * max(abs(ray.y),0.0001);
    float slabSpread = max(length(rayDx - ray * (rayDx.y / safeY)),
        length(rayDy - ray * (rayDy.y / safeY)));
    float depth = tex2D(Depth, v.UV).r;
    depth = depth > 0 ? depth : 40000;
    // Handle horizontal rays and cameras inside/above the layer explicitly.
    if (abs(ray.y) < 0.0001 || CloudLayer.z < 0.02) return 0;
    float a = (CloudLayer.x - CameraPosition.y) / ray.y;
    float b = (CloudLayer.x + CloudLayer.y - CameraPosition.y) / ray.y;
    float start = max(0, min(a, b));
    float finish = min(min(max(a,b), depth), 18000);
    if (finish <= start) return 0;
    // Short/steep rays need fewer intervals. Long horizon rays get a larger
    // budget and filtered density, instead of aliasing into horizontal layers.
    int intervals = (int)clamp(ceil((finish - start) / CloudSampling.y), 12, CloudSampling.x);
    float stepSize = (finish - start) / intervals;
    float baseLod = max(0, log2(max(1, stepSize / 128)));
    float distance = start + stepSize * 0.5;
    float transmittance = 1;
    float3 radiance = 0;
    float cosSun = dot(ray, SunDirection);
    float phase = 0.65 + 0.8 * pow(saturate(cosSun), 8);
    [loop] for (int i = 0; i < 128; i++)
    {
        if (i >= intervals || transmittance < 0.015) break;
        float3 p = CameraPosition + ray * distance;
        float lod = max(baseLod, log2(max(1, slabSpread * distance / 128)));
        // Two Gauss points average each interval without screen-space noise.
        float3 delta = ray * (stepSize * 0.288675);
        float density = (Density(p - delta, lod) + Density(p + delta, lod)) * 0.5;
        density *= 1 - smoothstep(14000,18000,distance);
        if (density > 0.001) {
            float lightDepth = Density(p + SunDirection * 160, lod) * 160 + Density(p + SunDirection * 420, lod) * 260;
            float direct = exp(-lightDepth * 0.004);
            // Diffuse skylight/multiple-scattering approximation keeps dense
            // daylight clouds bright instead of resembling dark blue smoke.
            float3 ambient = WeatherAmbient(CloudLight.x, CloudTwilight);
            float3 sunlight = WeatherSunTint(SunDirection.y) * direct * phase
                * max(CloudLight.x, CloudTwilight * 0.40) * CloudLight.w;
            float attenuation = exp(-density * stepSize * 0.006);
            float3 color = lerp(ambient + sunlight, FogColor, saturate(distance / 22000));
            radiance += transmittance * (1 - attenuation) * color;
            transmittance *= attenuation;
        }
        distance += stepSize;
    }
    float strength = CloudLight.z;
    return float4(radiance * strength, (1 - transmittance) * strength);
}

float4 Composite(Output v) : SV_TARGET
{
    // Depth-aware upsampling prevents low-resolution clouds bleeding over a
    // nearby train, trees or a mountain silhouette.
    float center = tex2D(Depth, v.UV).r;
    center = center > 0 ? center : 40000;
    float4 farPoint = mul(float4(v.UV * float2(2,-2) + float2(-1,1),1,1),InverseViewProjection);
    float3 ray = normalize(farPoint.xyz / farPoint.w - CameraPosition);
    if (abs(ray.y) < 0.0001) return 0;
    float a = (CloudLayer.x - CameraPosition.y) / ray.y;
    float b = (CloudLayer.x + CloudLayer.y - CameraPosition.y) / ray.y;
    if (min(max(a,b),center) <= max(0,min(a,b))) return 0;
    float2 lowPosition = v.UV / LowPixelSize - 0.5;
    float2 basePixel = floor(lowPosition);
    float2 fraction = frac(lowPosition);
    float4 result = 0;
    float total = 0;
    [unroll] for (int y = 0; y < 2; y++)
    [unroll] for (int x = 0; x < 2; x++) {
        float2 uv = (basePixel + float2(x,y) + 0.5) * LowPixelSize;
        float sampleDepth = tex2D(Depth, uv).r;
        sampleDepth = sampleDepth > 0 ? sampleDepth : 40000;
        float spatial = (x == 0 ? 1-fraction.x : fraction.x) * (y == 0 ? 1-fraction.y : fraction.y);
        float weight = spatial / (1 + abs(sampleDepth - center) * 0.1);
        result += tex2D(Clouds, uv) * weight;
        total += weight;
    }
    return result / max(total, 0.0001);
}
technique MarchClouds { pass P { VertexShader = compile vs_4_0 VS(); PixelShader = compile ps_4_0 March(); } }
technique CompositeClouds { pass P { VertexShader = compile vs_4_0 VS(); PixelShader = compile ps_4_0 Composite(); } }
