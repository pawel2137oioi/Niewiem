using System;

namespace Orts.Viewer3D
{
    /// <summary>Bounded, per-frame parameters shared by scenery and sky effects.</summary>
    internal readonly struct EnvironmentParameters
    {
        internal readonly float Strength;
        internal readonly float Daylight;
        internal readonly float ClearSky;
        internal readonly float HighQuality;
        internal readonly float Twilight;
        internal readonly float SunVisibility;
        internal readonly float GroundFogDensity;
        internal readonly float InverseGroundFogHeight;

        internal EnvironmentParameters(int quality, int intensity, float sunHeight, float overcast, bool useMstsEnvironment,
            float precipitation = 0, float visibility = 20000, int groundFog = 35, int fogHeight = 40)
        {
            // Preserve route-supplied MSTS environments and offer an explicit legacy mode.
            Strength = quality <= 0 || useMstsEnvironment ? 0 : Math.Clamp(intensity, 0, 100) / 100f;
            sunHeight = Finite(sunHeight, -1);
            var day = Math.Clamp((sunHeight + 0.08f) / 0.26f, 0, 1);
            Daylight = day * day * (3 - 2 * day);
            ClearSky = 1 - Math.Clamp(Finite(overcast, 1), 0, 1);
            // A separate twilight envelope retains horizon warmth after direct light fades.
            Twilight = SmoothStep(-0.2f, -0.02f, sunHeight) * (1 - SmoothStep(0.04f, 0.25f, sunHeight));
            SunVisibility = SmoothStep(300, 8000, Finite(visibility, 0))
                * (1 - 0.9f * Math.Clamp(Finite(precipitation, 1), 0, 1));
            GroundFogDensity = Strength > 0 ? Math.Clamp(groundFog, 0, 100) * 0.0000045f : 0;
            InverseGroundFogHeight = 1f / Math.Clamp(fogHeight, 5, 500);
            HighQuality = quality >= 2 && Strength > 0 && (Daylight > 0 || Twilight > 0)
                && ClearSky > 0 && SunVisibility > 0 ? 1 : 0;
        }

        static float Finite(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }

        static float SmoothStep(float start, float end, float value)
        {
            var amount = Math.Clamp((value - start) / (end - start), 0, 1);
            return amount * amount * (3 - 2 * amount);
        }

        internal static string SelectTechniqueSuffix(int quality, bool useMstsEnvironment, bool feature10, bool feature93)
        {
            return quality > 0 && !useMstsEnvironment && feature10
                ? "Advanced" : feature93 ? "Level9_3" : "Level9_1";
        }
    }
}
