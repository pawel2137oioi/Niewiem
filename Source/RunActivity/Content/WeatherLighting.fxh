// Shared analytic light colors. Inputs are bounded solar elevation/day/twilight
// factors, independent of whether the sun is rising or setting.
float3 WeatherSunTint(float sunHeight)
{
    return lerp(float3(1.0, 0.28, 0.075), float3(1.0, 0.94, 0.82),
        smoothstep(-0.04, 0.30, sunHeight));
}

float3 WeatherAmbient(float daylight, float twilight)
{
    return lerp(float3(0.012, 0.020, 0.045), float3(0.50, 0.56, 0.64), daylight)
        + float3(0.045, 0.022, 0.018) * twilight * (1 - daylight);
}
