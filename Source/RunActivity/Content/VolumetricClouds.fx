// Depth-limited 3D cloud integration. Quarter-resolution, 20 view samples,
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
float2 LowPixelSize;
sampler Depth = sampler_state { Texture = (DepthTexture); MinFilter = Point; MagFilter = Point; MipFilter = None; AddressU = Clamp; AddressV = Clamp; };
sampler Clouds = sampler_state { Texture = (CloudTexture); MinFilter = Point; MagFilter = Point; MipFilter = None; AddressU = Clamp; AddressV = Clamp; };
sampler3D Noise = sampler_state { Texture = (NoiseTexture); MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Wrap; AddressV = Wrap; AddressW = Wrap; };
struct Input { float3 Position : POSITION0; float2 UV : TEXCOORD0; };
struct Output { float4 Position : SV_POSITION; float2 UV : TEXCOORD0; };
Output VS(Input v) { Output o; o.Position = float4(v.Position, 1); o.UV = v.UV; return o; }

float Density(float3 p)
{
    float h = (p.y - CloudLayer.x) / CloudLayer.y;
    if (h <= 0 || h >= 1) return 0;
    float3 coord = (p + WorldOffset + float3(CloudLayer.w * 7, 0, CloudLayer.w * 3)) / 32768;
    float shape = tex3Dlod(Noise, float4(coord, 0)).r * 0.65;
    shape += tex3Dlod(Noise, float4(coord * 2.0 + 0.17, 0)).r * 0.25;
    shape += tex3Dlod(Noise, float4(coord * 8.0 + 0.39, 0)).r * 0.10;
    float coverage = lerp(0.72, 0.28, CloudLayer.z);
    return saturate((shape - coverage) * 7) * smoothstep(0, 0.18, h) * (1 - smoothstep(0.65, 1, h));
}

float4 March(Output v) : SV_TARGET
{
    float4 farPoint = mul(float4(v.UV * float2(2,-2) + float2(-1,1), 1, 1), InverseViewProjection);
    float3 ray = normalize(farPoint.xyz / farPoint.w - CameraPosition);
    float depth = tex2D(Depth, v.UV).r;
    depth = depth > 0 ? depth : 40000;
    // Handle horizontal rays and cameras inside/above the layer explicitly.
    if (abs(ray.y) < 0.0001 || CloudLayer.z < 0.02) return 0;
    float a = (CloudLayer.x - CameraPosition.y) / ray.y;
    float b = (CloudLayer.x + CloudLayer.y - CameraPosition.y) / ray.y;
    float start = max(0, min(a, b));
    float finish = min(min(max(a,b), depth), 18000);
    if (finish <= start) return 0;
    float stepSize = min((finish - start) / 20, 250);
    // Stable spatial jitter reduces slicing without temporal shimmer/history.
    float jitter = frac(sin(dot(floor(v.Position.xy), float2(12.9898,78.233))) * 43758.5453);
    float distance = start + stepSize * jitter;
    float transmittance = 1;
    float3 radiance = 0;
    float cosSun = dot(ray, SunDirection);
    float phase = 0.65 + 0.8 * pow(saturate(cosSun), 8);
    [loop] for (int i = 0; i < 20; i++)
    {
        if (distance >= finish || transmittance < 0.015) break;
        float3 p = CameraPosition + ray * distance;
        float density = Density(p);
        if (density > 0.001) {
            float lightDepth = Density(p + SunDirection * 160) * 160 + Density(p + SunDirection * 420) * 260;
            float direct = exp(-lightDepth * 0.006);
            float3 ambient = lerp(float3(0.025,0.03,0.045), float3(0.28,0.36,0.48), CloudLight.x);
            float3 sunlight = float3(1.0,0.94,0.82) * direct * phase * CloudLight.x * CloudLight.w;
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
