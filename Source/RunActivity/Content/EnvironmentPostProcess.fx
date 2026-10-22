// Open Rails: scene-only filmic grading and reduced-resolution bloom.
texture SceneTexture;
texture BloomTexture;
float2 SampleStep;
float2 BlurDirection;
float4 Grade; // strength, daylight, bloom strength, FXAA
float ColorGradeStrength;

sampler Scene = sampler_state { Texture = (SceneTexture); MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Clamp; AddressV = Clamp; };
sampler Bloom = sampler_state { Texture = (BloomTexture); MinFilter = Linear; MagFilter = Linear; MipFilter = None; AddressU = Clamp; AddressV = Clamp; };
struct Input { float3 Position : POSITION0; float2 UV : TEXCOORD0; };
struct Output { float4 Position : SV_POSITION; float2 UV : TEXCOORD0; };
Output VS(Input v) { Output o; o.Position = float4(v.Position, 1); o.UV = v.UV; return o; }

float3 Filmic(float3 x)
{
    return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
}

float4 Extract(Output v) : SV_TARGET
{
    float3 c = (tex2D(Scene, v.UV + SampleStep * float2(-1,-1)).rgb
              + tex2D(Scene, v.UV + SampleStep * float2(1,-1)).rgb
              + tex2D(Scene, v.UV + SampleStep * float2(-1,1)).rgb
              + tex2D(Scene, v.UV + SampleStep * float2(1,1)).rgb) * 0.25;
    float brightness = max(c.r, max(c.g, c.b));
    // Reserve bloom for bright highlights; ordinary sky/foliage should retain
    // contrast instead of contributing a broad milky veil.
    float knee = saturate((brightness - 0.80) / 0.35);
    return float4(c * knee * knee, 1);
}

float4 Blur(Output v) : SV_TARGET
{
    float3 c = tex2D(Scene, v.UV).rgb * 0.227027;
    c += (tex2D(Scene, v.UV + BlurDirection * 1.384615).rgb
        + tex2D(Scene, v.UV - BlurDirection * 1.384615).rgb) * 0.316216;
    c += (tex2D(Scene, v.UV + BlurDirection * 3.230769).rgb
        + tex2D(Scene, v.UV - BlurDirection * 3.230769).rgb) * 0.070270;
    return float4(c, 1);
}

float4 Composite(Output v) : SV_TARGET
{
    float4 source = tex2D(Scene, v.UV);
    if (Grade.w > 0.5) {
        float3 nw = tex2D(Scene,v.UV + SampleStep * float2(-1,-1)).rgb;
        float3 ne = tex2D(Scene,v.UV + SampleStep * float2(1,-1)).rgb;
        float3 sw = tex2D(Scene,v.UV + SampleStep * float2(-1,1)).rgb;
        float3 se = tex2D(Scene,v.UV + SampleStep * float2(1,1)).rgb;
        float3 luma = float3(0.299,0.587,0.114);
        float nwl=dot(nw,luma), nel=dot(ne,luma), swl=dot(sw,luma), sel=dot(se,luma), mid=dot(source.rgb,luma);
        float2 direction = float2(-((nwl+nel)-(swl+sel)),(nwl+swl)-(nel+sel));
        float reduction = max((nwl+nel+swl+sel)*0.03125,0.0078125);
        direction = clamp(direction/(min(abs(direction.x),abs(direction.y))+reduction),-8,8)*SampleStep;
        float3 a = (tex2D(Scene,v.UV + direction * -0.166667).rgb + tex2D(Scene,v.UV + direction * 0.166667).rgb)*0.5;
        float3 b = a*0.5 + (tex2D(Scene,v.UV - direction*0.5).rgb + tex2D(Scene,v.UV + direction*0.5).rgb)*0.25;
        float low=min(mid,min(min(nwl,nel),min(swl,sel))), high=max(mid,max(max(nwl,nel),max(swl,sel)));
        source.rgb = dot(b,luma)<low || dot(b,luma)>high ? a : b;
    }
    float3 sceneLinear = pow(max(source.rgb, 0), 2.2);
    // Grade in linear light, retain black at night, protect bright sky/clouds.
    float luminance = dot(sceneLinear, float3(0.2126, 0.7152, 0.0722));
    float3 balance = lerp(float3(0.91, 0.98, 1.08), float3(1.04, 1.01, 0.96), saturate(luminance * 3));
    float3 graded = Filmic(sceneLinear * balance * lerp(0.85, 1.05, Grade.y));
    float grey = dot(graded, float3(0.2126, 0.7152, 0.0722));
    graded = lerp(grey.xxx, graded, 1.04);
    graded = pow(saturate(graded), 1 / 2.2);
    float3 color = lerp(source.rgb, graded, ColorGradeStrength * lerp(0.25, 1, Grade.y));
    color += tex2D(Bloom, v.UV).rgb * Grade.z * Grade.x * lerp(0.3, 1, Grade.y);
    return float4(saturate(color), source.a);
}

technique ExtractBloom { pass P { VertexShader = compile vs_4_0 VS(); PixelShader = compile ps_4_0 Extract(); } }
technique BlurBloom { pass P { VertexShader = compile vs_4_0 VS(); PixelShader = compile ps_4_0 Blur(); } }
technique CompositeScene { pass P { VertexShader = compile vs_4_0 VS(); PixelShader = compile ps_4_0 Composite(); } }
