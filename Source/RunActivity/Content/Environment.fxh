// Shared atmospheric controls. No extra render targets or texture samples.
// x: strength, y: daylight, z: clear sky, w: high-quality directional scattering.
float4 Environment;
// x: twilight envelope, y: weather visibility of the sun,
// z: ground-layer extinction per metre, w: reciprocal layer height.
float4 EnvironmentWeather;
float EnvironmentGroundLevel;

float EnvironmentDayStrength()
{
    return Environment.x * Environment.y;
}

float EnvironmentAtmosphereStrength()
{
    return Environment.x * max(Environment.y, EnvironmentWeather.x * 0.35);
}

float EnvironmentTwilightStrength()
{
    return Environment.x * EnvironmentWeather.x * EnvironmentWeather.y * (0.2 + 0.8 * Environment.z);
}

float3 EnvironmentHorizonColor(float3 fogColor)
{
    // Use the weather's fog color as the base, with a cool daylight aerial tint.
    float3 dayColor = lerp(fogColor, fogColor * float3(0.83, 0.94, 1.12),
        EnvironmentDayStrength() * Environment.z);
    return lerp(dayColor, fogColor * float3(1.5, 0.78, 0.38), EnvironmentTwilightStrength());
}

float EnvironmentLayerDensity(float cameraHeight, float surfaceHeight)
{
    float h0 = max(cameraHeight, 0) * EnvironmentWeather.w;
    float h1 = max(surfaceHeight, 0) * EnvironmentWeather.w;
    float delta = h1 - h0;
    float cameraDensity = exp(-h0);
    // Avoid cancellation and division by zero for horizontal rays.
    if (abs(delta) < 0.01)
        return cameraDensity * (1 - delta * 0.5 + delta * delta / 6);
    return saturate((cameraDensity - exp(-h1)) / delta);
}

float EnvironmentFogAmount(float3 relativePosition, float cameraHeight, float legacyFog)
{
    float strength = EnvironmentAtmosphereStrength();
    if (strength <= 0) return legacyFog;
    float distanceM = length(relativePosition);
    // World-space distance avoids fog changing with FOV/aspect ratio.
    // Existing weather fog always dominates; never remove route visibility limits.
    float haze = (1 - exp2(-distanceM * 0.00012)) * strength;
    if (EnvironmentWeather.z > 0) {
        float height = cameraHeight - EnvironmentGroundLevel;
        float density = EnvironmentLayerDensity(height, height + relativePosition.y);
        float transmittance = exp(-distanceM * density * EnvironmentWeather.z * strength);
        haze = 1 - (1 - haze) * transmittance;
    }
    return max(legacyFog, saturate(haze));
}

float3 EnvironmentLitColor(float3 textureColor, float3 legacyColor, float directLight, float nightTexture)
{
    if (EnvironmentDayStrength() <= 0 || nightTexture >= 1) return legacyColor;
    float sunlight = saturate(directLight);
    // Work in linear light: skylight remains in shadow, warm directional light
    // carries the lit surfaces. Convert back before blending legacy materials.
    float3 albedo = pow(max(textureColor, 0), 2.2);
    float3 skyLight = float3(0.09, 0.13, 0.20);
    float3 sunLight = float3(1.65, 1.48, 1.18) * sunlight;
    float3 lit = pow(max(albedo * (skyLight + sunLight), 0), 1 / 2.2);
    float amount = EnvironmentDayStrength() * Environment.z * EnvironmentWeather.y * (1 - nightTexture);
    return lerp(legacyColor, lit, amount);
}

float3 EnvironmentScatterColor(float3 fogColor, float3 ray, float3 sunDirection)
{
    float forward = saturate(dot(ray, sunDirection));
    // A bounded lobe replaces expensive volumetric ray marching.
    float lobe = forward * forward;
    lobe *= lobe;
    lobe *= lobe;
    float lowSun = 1 - saturate(sunDirection.y * 3);
    float3 sunTint = lerp(float3(1.0, 0.96, 0.86), float3(1.0, 0.68, 0.38), lowSun);
    return lerp(EnvironmentHorizonColor(fogColor), sunTint,
        lobe * 0.35 * EnvironmentAtmosphereStrength() * Environment.z * EnvironmentWeather.y * Environment.w);
}
