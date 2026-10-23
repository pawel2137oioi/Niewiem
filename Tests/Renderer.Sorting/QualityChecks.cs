using ORTS.Settings;

internal static class QualityChecks
{
    internal static void Run()
    {
        var checks = 0;
        void Check(bool valid, string message) { checks++; if (!valid) throw new Exception(message); }
        Check(GraphicsQuality.Resolve(-1, true, 1) == 0, "Old Balanced config unexpectedly enabled volumes.");
        Check(GraphicsQuality.Resolve(-1, true, 2) == 3, "Old High config lost volume quality.");
        Check(GraphicsQuality.ResolveBloom(-1, true, 1) == 2, "Old Balanced bloom changed budget.");
        Check(GraphicsQuality.ResolveBloom(-1, true, 2) == 3, "Old High bloom changed budget.");
        for (var configured = -2; configured <= 6; configured++)
            for (var atmosphere = 0; atmosphere <= 2; atmosphere++)
            {
                Check(GraphicsQuality.Resolve(configured, false, atmosphere) == 0, "Disabled checkbox was ignored.");
                Check(GraphicsQuality.ResolveBloom(configured, false, atmosphere) == 0, "Disabled bloom checkbox was ignored.");
                Check(GraphicsQuality.Resolve(configured, true, atmosphere) is >= 0 and <= 4, "Unbounded feature quality.");
            }
        for (var level = 1; level <= 4; level++)
        {
            Check(GraphicsQuality.Resolve(level, true, 1) == level, "Independent feature still depends on High atmosphere.");
            var current = GraphicsQuality.Clouds(level); var previous = GraphicsQuality.Clouds(level - 1);
            Check(current.Intervals > previous.Intervals, "Cloud accuracy did not increase.");
            Check(current.ResolutionPercent >= previous.ResolutionPercent, "Cloud scale decreased with quality.");
            Check(current.StepLength <= previous.StepLength, "Cloud sampling got coarser at higher quality.");
            Check(current.Shadows == (level >= 2), "Cloud shadow quality gate changed.");
            if (level > 1)
            {
                Check(GraphicsQuality.Reflections(level).MaxWidth > GraphicsQuality.Reflections(level - 1).MaxWidth, "Reflection resolution did not increase.");
                Check(GraphicsQuality.Reflections(level).UpdateFrames < GraphicsQuality.Reflections(level - 1).UpdateFrames, "Reflection cadence did not increase.");
                Check(GraphicsQuality.BloomDivisor(level) < GraphicsQuality.BloomDivisor(level - 1), "Bloom resolution did not increase.");
            }
        }
        Check(GraphicsQuality.Clouds(0).Intervals == 0 && GraphicsQuality.Reflections(0).MaxWidth == 0, "Off still has a rendering budget.");
        Check(GraphicsQuality.Clouds(999).Intervals == GraphicsQuality.Clouds(4).Intervals, "Cloud quality not clamped.");
        Check(GraphicsQuality.Reflections(-99).MaxWidth == 0, "Negative reflection quality not clamped.");
        Console.WriteLine($"PASS: {checks} quality compatibility, independence and budget checks.");
    }
}
