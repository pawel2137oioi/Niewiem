using System.Diagnostics;
using Microsoft.Xna.Framework;
using Orts.Viewer3D;

static RenderItem Item(float x, float y, float z, float sortIndex = 0, bool water = false)
{
    var matrix = Matrix.CreateTranslation(x, y, z);
    return new RenderItem(water ? new WaterMaterial() : new Material(),
        new RenderPrimitive { SortIndex = sortIndex }, ref matrix, ShapeFlags.None, new object());
}

// Preserve the previous algorithm as an independent regression oracle, including its
// world-to-XNA Z conversion, millimetre ties, and special case for water below the camera.
static IComparer<RenderItem> Legacy(Vector3 camera) => new LegacyRenderItemComparer(camera);

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static RenderItem[] Sorted(RenderItemCollection collection)
{
    var result = new List<RenderItem>();
    foreach (var item in collection) result.Add(item);
    return result.ToArray();
}

static void MatchesLegacy(RenderItem[] source, Vector3 camera)
{
    var expected = source.ToArray();
    Array.Sort(expected, Legacy(camera));
    var actual = new RenderItemCollection();
    foreach (var item in source) actual.Add(item);
    actual.Sort(new RenderItem.Comparer(camera));
    Check(Sorted(actual).Select(x => x.ItemData).SequenceEqual(expected.Select(x => x.ItemData)),
        "Cached sort differs from the previous draw order.");
}

var checks = 0;
MatchesLegacy(Array.Empty<RenderItem>(), Vector3.Zero); checks++;
MatchesLegacy(new[] { Item(1, 2, 3) }, Vector3.Zero); checks++;
MatchesLegacy(new[] { Item(0, 0, 10), Item(0, 0, 1), Item(0, 0, 100) }, Vector3.Zero); checks++;
MatchesLegacy(new[] { Item(10, 0, 0, 2), Item(10.0005f, 0, 0, 1), Item(10.002f, 0, 0) }, Vector3.Zero); checks++;
MatchesLegacy(new[] { Item(100, 5, 0, water: true), Item(100, 5.1f, 0, water: true) }, new Vector3(0, 10, 0)); checks++;
MatchesLegacy(new[] { Item(100, 5, 0, water: true), Item(100, 5.1f, 0, water: true) }, new Vector3(0, 0, 0)); checks++;
MatchesLegacy(new[] { Item(10, 0, 0), Item(10, 0, 0), Item(10, 0, 0) }, Vector3.Zero); checks++;
MatchesLegacy(new[] { Item(10, 0, 0, water: true), Item(10.999f, 0, 0, water: true) }, new Vector3(0, 1, 0)); checks++;
MatchesLegacy(new[] { Item(10, 0, 0, water: true), Item(11.001f, 0, 0, water: true) }, new Vector3(0, 1, 0)); checks++;

var random = new Random(73421);
for (var scene = 0; scene < 100; scene++)
{
    var items = Enumerable.Range(0, 512).Select(i => Item(
        random.Next(-10000, 10000), random.Next(-100, 100), random.Next(-10000, 10000), i)).ToArray();
    var camera = new Vector3(random.Next(-1000, 1000), random.Next(-100, 100), random.Next(-1000, 1000));
    MatchesLegacy(items, camera); checks++;
}

// Dense equal-distance groups exercise unstable-sort ties and in-place permutation cycles.
for (var count = 2; count <= 256; count++)
{
    var items = Enumerable.Range(0, count).Select(i => Item(
        random.Next(1, 5), 0, 0, random.Next(0, 3), water: i % 3 == 0)).ToArray();
    MatchesLegacy(items, Vector3.Zero); checks++;
}

// Reusing the collection after a camera move must invalidate the cached keys.
var near = Item(0, 0, 5);
var far = Item(0, 0, 100);
var reused = new RenderItemCollection();
reused.Add(near); reused.Add(far);
reused.Sort(new RenderItem.Comparer(Vector3.Zero));
Check(ReferenceEquals(Sorted(reused)[0].ItemData, far.ItemData), "Initial order incorrect."); checks++;
reused.Sort(new RenderItem.Comparer(new Vector3(0, 0, -110)));
Check(ReferenceEquals(Sorted(reused)[0].ItemData, near.ItemData), "Camera move used stale keys."); checks++;
reused.Clear();
reused.Add(Item(50, 0, 0, 1)); reused.Add(Item(1, 0, 0, 2));
reused.Sort(Comparer<RenderItem>.Create((x, y) => x.RenderPrimitive.SortIndex.CompareTo(y.RenderPrimitive.SortIndex)));
Check(Sorted(reused)[0].RenderPrimitive.SortIndex == 1, "Custom comparer was overridden."); checks++;
Console.WriteLine($"PASS: {checks} sorting regression checks (production RenderItem.cs).");

var environmentChecks = 0;
foreach (var quality in new[] { -1, 0, 1, 2, 100 })
foreach (var intensity in new[] { -100, 0, 50, 100, 200 })
foreach (var sun in new[] { -1f, -0.08f, 0f, 0.18f, 1f, float.NaN, float.PositiveInfinity })
foreach (var clouds in new[] { -1f, 0f, 0.5f, 1f, 2f, float.NaN })
{
    var p = new EnvironmentParameters(quality, intensity, sun, clouds, false);
    foreach (var value in new[] { p.Strength, p.Daylight, p.ClearSky, p.HighQuality, p.Twilight, p.SunVisibility })
        Check(float.IsFinite(value) && value >= 0 && value <= 1, "Unbounded environment uniform.");
    Check(float.IsFinite(p.GroundFogDensity) && p.GroundFogDensity >= 0 && p.GroundFogDensity <= 0.000451f, "Unbounded ground fog.");
    Check(float.IsFinite(p.InverseGroundFogHeight) && p.InverseGroundFogHeight > 0, "Invalid fog layer height.");
    if (quality <= 0) Check(p.Strength == 0 && p.HighQuality == 0, "Legacy effects active.");
    if (sun <= -0.08f) Check(p.Daylight == 0, "Direct daytime lighting active after sunset.");
    if (sun <= -0.2f) Check(p.Twilight == 0 && p.HighQuality == 0, "Twilight scattering active at night.");
    if (clouds >= 1) Check(p.ClearSky == 0 && p.HighQuality == 0, "Direct sun scattering active under full overcast.");
    var msts = new EnvironmentParameters(quality, intensity, sun, clouds, true);
    Check(msts.Strength == 0 && msts.HighQuality == 0 && msts.GroundFogDensity == 0, "Route environment overwritten.");
    environmentChecks++;
}
var previousDay = 0f;
for (var i = 0; i <= 100; i++)
{
    var p = new EnvironmentParameters(1, 70, -0.08f + i * 0.0026f, 0, false);
    Check(p.Daylight >= previousDay, "Discontinuous day transition.");
    previousDay = p.Daylight;
    environmentChecks++;
}
Check(new EnvironmentParameters(1, 70, 1, 0, false).HighQuality == 0, "Balanced enables high-quality path.");
Check(new EnvironmentParameters(2, 70, 1, 0, false).HighQuality == 1, "High-quality path missing.");
Check(EnvironmentParameters.SelectTechniqueSuffix(1, false, true, true) == "Advanced", "Modern GPU does not select effects.");
Check(EnvironmentParameters.SelectTechniqueSuffix(2, false, false, true) == "Level9_3", "High effects selected on unsupported GPU.");
Check(EnvironmentParameters.SelectTechniqueSuffix(1, false, false, false) == "Level9_1", "Low-feature fallback missing.");
Check(EnvironmentParameters.SelectTechniqueSuffix(0, false, true, true) == "Level9_3", "Original mode selects enhanced effects.");
Check(EnvironmentParameters.SelectTechniqueSuffix(2, true, true, true) == "Level9_3", "MSTS environment selects enhanced effects.");
Console.WriteLine($"PASS: {environmentChecks + 7} atmosphere/capability checks (production EnvironmentParameters.cs).");

var weatherChecks = 0;
foreach (var rain in new[] { -1f, 0f, 0.5f, 1f, 2f, float.NaN, float.PositiveInfinity })
foreach (var visibility in new[] { -10f, 0f, 300f, 1000f, 8000f, 20000f, float.NaN, float.PositiveInfinity })
foreach (var ground in new[] { -1, 0, 35, 100, int.MaxValue })
foreach (var layer in new[] { -10, 0, 5, 40, 500, int.MaxValue })
{
    var p = new EnvironmentParameters(2, 70, 0.05f, 0.1f, false, rain, visibility, ground, layer);
    Check(float.IsFinite(p.SunVisibility) && p.SunVisibility >= 0 && p.SunVisibility <= 1, "Invalid solar visibility.");
    Check(float.IsFinite(p.GroundFogDensity) && p.GroundFogDensity >= 0 && p.GroundFogDensity <= 0.000451f, "Invalid layer density.");
    Check(float.IsFinite(p.InverseGroundFogHeight) && p.InverseGroundFogHeight >= 0.002f && p.InverseGroundFogHeight <= 0.2f, "Invalid height clamp.");
    if (ground <= 0) Check(p.GroundFogDensity == 0, "Disabled ground fog is active.");
    if (visibility <= 300) Check(p.SunVisibility == 0 && p.HighQuality == 0, "Sun scatter active in dense fog.");
    weatherChecks++;
}
var clearWeather = new EnvironmentParameters(2, 70, 0.05f, 0.1f, false, 0, 20000);
var rainWeather = new EnvironmentParameters(2, 70, 0.05f, 0.1f, false, 1, 20000);
var fogWeather = new EnvironmentParameters(2, 70, 0.05f, 0.1f, false, 0, 1000);
Check(rainWeather.SunVisibility < clearWeather.SunVisibility, "Rain does not dim sun glare.");
Check(fogWeather.SunVisibility < clearWeather.SunVisibility, "Poor visibility does not dim sun glare.");
Check(new EnvironmentParameters(2, 70, -0.1f, 0, false).Twilight > 0, "Twilight ends before direct sunlight.");
Check(new EnvironmentParameters(2, 70, 1, 0, false).Twilight == 0, "Twilight present at high noon.");
Check(new EnvironmentParameters(2, 70, -1, 0, false).Twilight == 0, "Twilight present at midnight.");
Console.WriteLine($"PASS: {weatherChecks + 5} precipitation, visibility and ground-layer checks.");
QualityChecks.Run();

if (args.Contains("--benchmark"))
{
    var source = Enumerable.Range(0, 20000).Select(i => Item(
        random.Next(-10000, 10000), random.Next(-100, 100), random.Next(-10000, 10000), i)).ToArray();
    var camera = new Vector3(123, 50, -456);
    var legacy = Legacy(camera);
    var cached = new RenderItem.Comparer(camera);
    double Run(IComparer<RenderItem> comparer)
    {
        var collection = new RenderItemCollection();
        var elapsed = 0L;
        for (var iteration = 0; iteration < 35; iteration++)
        {
            collection.Clear();
            foreach (var item in source) collection.Add(item);
            var start = Stopwatch.GetTimestamp();
            collection.Sort(comparer);
            var ticks = Stopwatch.GetTimestamp() - start;
            if (iteration >= 5) elapsed += ticks; // Warm up JIT and array capacity.
        }
        return elapsed * 1000.0 / Stopwatch.Frequency / 30;
    }
    var before = Run(legacy);
    var after = Run(cached);
    Console.WriteLine($"CPU sort only, 20000 items, 30 iterations: previous {before:F3} ms, cached {after:F3} ms, ratio {before / after:F2}x.");
    Console.WriteLine("This is a CPU microbenchmark, not an in-game FPS measurement.");
}

// Exact pre-optimization comparison logic, without a delegate wrapper in the benchmark.
sealed class LegacyRenderItemComparer : IComparer<RenderItem>
{
    readonly Vector3 Camera;
    internal LegacyRenderItemComparer(Vector3 camera)
    {
        Camera = camera;
        Camera.Z *= -1;
    }
    public int Compare(RenderItem x, RenderItem y)
    {
        var xd = (x.XNAMatrix.Translation - Camera).Length();
        var yd = (y.XNAMatrix.Translation - Camera).Length();
        if (x.Material is WaterMaterial && y.Material is WaterMaterial
            && Math.Abs(yd - xd) < 1.0 && x.XNAMatrix.Translation.Y < Camera.Y)
            return Math.Sign(x.XNAMatrix.Translation.Y - y.XNAMatrix.Translation.Y);
        if (Math.Abs(yd - xd) >= 0.001)
            return Math.Sign(yd - xd);
        return Math.Sign(x.RenderPrimitive.SortIndex - y.RenderPrimitive.SortIndex);
    }
}
