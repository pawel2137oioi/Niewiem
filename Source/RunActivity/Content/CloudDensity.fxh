// Shared by cloud rays and scenery shadows. RGB stores coherent periodic
// value noise at three scales; one filtered lookup replaces three white-noise
// lookups. Keep the field anchored in world space while the camera moves.
float CloudMass(float3 position, float3 offset, float time)
{
    float3 coord = (position + offset + float3(time * 7, 0, time * 3)) / 8192;
    float3 field = tex3Dlod(CloudNoise, float4(coord, 0)).rgb;
    return dot(field, float3(0.68, 0.24, 0.08));
}

float CloudAmount(float mass, float coverage)
{
    float threshold = lerp(0.70, 0.26, saturate(coverage));
    return smoothstep(threshold, threshold + 0.14, mass);
}
