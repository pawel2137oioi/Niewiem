using System;

namespace ORTS.Settings
{
    /// <summary>Shared budgets for independent environment options; 0 disables a feature.</summary>
    public static class GraphicsQuality
    {
        public static string Name(int quality) => Math.Clamp(quality, 0, 4) switch
        { 0 => "Off", 1 => "Low", 2 => "Medium", 3 => "High", _ => "Ultra" };
        // -1 is the migration value: retain the older umbrella/checkbox behavior
        // until the user explicitly chooses an independent quality level.
        public static int Resolve(int configured, bool enabled, int atmosphere)
            => !enabled ? 0 : configured < 0 ? (atmosphere >= 2 ? 3 : 0) : Math.Clamp(configured, 0, 4);

        public static int ResolveBloom(int configured, bool enabled, int atmosphere)
            => !enabled ? 0 : configured < 0 ? (atmosphere >= 2 ? 3 : 2) : Math.Clamp(configured, 0, 4);

        public static CloudBudget Clouds(int quality) => Math.Clamp(quality, 0, 4) switch
        {
            0 => new CloudBudget(25, 0, 240, 1280, false),
            1 => new CloudBudget(25, 16, 240, 1280, false),
            2 => new CloudBudget(33, 32, 200, 1280, true),
            3 => new CloudBudget(50, 48, 160, 1280, true),
            _ => new CloudBudget(75, 80, 100, 1536, true),
        };

        public static ReflectionBudget Reflections(int quality) => Math.Clamp(quality, 0, 4) switch
        {
            0 => new ReflectionBudget(0, 1),
            1 => new ReflectionBudget(320, 4),
            2 => new ReflectionBudget(480, 3),
            3 => new ReflectionBudget(640, 2),
            _ => new ReflectionBudget(960, 1),
        };

        public static int BloomDivisor(int quality) => Math.Clamp(quality, 1, 4) switch
        {
            1 => 8,
            2 => 4,
            3 => 2,
            _ => 1,
        };
    }

    public readonly struct CloudBudget
    {
        public readonly int ResolutionPercent, Intervals, MaxWidth;
        public readonly float StepLength;
        public readonly bool Shadows;
        public CloudBudget(int resolutionPercent, int intervals, float stepLength, int maxWidth, bool shadows)
        { ResolutionPercent = resolutionPercent; Intervals = intervals; StepLength = stepLength; MaxWidth = maxWidth; Shadows = shadows; }
    }

    public readonly struct ReflectionBudget
    {
        public readonly int MaxWidth, UpdateFrames;
        public ReflectionBudget(int maxWidth, int updateFrames) { MaxWidth = maxWidth; UpdateFrames = updateFrames; }
    }
}
