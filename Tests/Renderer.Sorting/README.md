# Renderer sorting regression runner

This CPU-only runner links the production RenderItem.cs. Small stand-ins supply
GPU-facing type identities; all distance calculations use the real MonoGame
Matrix and Vector3 types. No graphics device or Windows Desktop runtime is used.
This does not test rendering, shaders, route loading, or frame rate.

The runner also links EnvironmentParameters.cs and checks bounded atmospheric
uniforms, continuous day/night transitions, overcast, MSTS environment
preservation and automatic capability fallback for DirectX feature levels.
Weather cases cover solar attenuation in rain and fog, twilight extending past
direct daylight, disabled ground layers and bounded layer height/density.

From the repository root, with .NET SDK 6.0.428:

```sh
dotnet run --project Tests/Renderer.Sorting/Renderer.Sorting.csproj -c Release
DOTNET_TieredCompilation=0 dotnet run --project Tests/Renderer.Sorting/Renderer.Sorting.csproj -c Release -- --benchmark
```

The runner exits nonzero on a regression. It compares draw order against the
previous algorithm for deterministic random scenes, equal distances, millimetre
ties, water below/above the camera, camera moves, empty/single-item collections,
and custom comparers. Benchmark mode times sorting only, after populating the
collection and warming up capacity/JIT. The old comparison is a concrete class
with the original calculations, rather than a delegate.

The MonoGame WindowsDX assembly declares Windows platform support even for
managed matrix math, so the portable runner produces CA1416 analyzer warnings.
The verified checks execute this managed subset on Linux; that is not evidence
that the graphics API works there.
