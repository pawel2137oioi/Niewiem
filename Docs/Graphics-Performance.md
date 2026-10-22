# Graphics and performance work

Target: 1920x1080 at 60 FPS with adjustable quality. Actual hardware and a
representative route are not specified. This is a target, not a measured result.

## Implemented CPU optimization

Transparency sorting previously computed two vector lengths and moved full
RenderItem structures (including matrices and material references) during
O(n log n) sorting. RenderItemCollection now computes compact, reference-free
sort keys once per item, sorts those keys, and applies their permutation to the
items in place. The key buffer grows with the collection and is reused.
It costs an additional approximately 20 bytes per capacity slot in collections
that use camera sorting; opaque collections do not allocate it.

The existing ordering is retained: far-to-near distance, the 1mm SortIndex tie,
the special rule for nearby water planes, and world-to-XNA camera conversion.
Other IComparer implementations keep the original Array.Sort path. Keys are
rebuilt for each sort so moving cameras and reused frame collections cannot
use stale distances. RenderItem and RenderItemCollection were moved out of
RenderFrame.cs so their actual production code can run in CPU-only regression
checks without instantiating the Windows renderer.

See ../Tests/Renderer.Sorting/README.md for checks and the benchmark command.
A Linux Release microbenchmark with tiered compilation disabled measured
15.857ms before and 3.802ms after for sorting 20,000 objects, averaged over 30
iterations. This measures CPU sorting, not whole-frame performance or FPS.

## Implemented environment rendering

The supplied reference screenshots show pronounced directional sunlight, cool
shadows, distant mountain haze, bright sun halos, cloud highlights and dark
night scenes. The implementation adds these atmosphere/lighting controls to the
existing forward renderer. It does not replace the route geometry or textures.

In Options > Environment:

- Original uses the existing scenery/sky techniques (apart from a bounded
  CPU sunset-glow denominator that avoids a division by zero).
- Balanced, the default, adds world-distance aerial haze, a configurable local
  ground fog layer, stronger sun/shadow separation, cool ambient tint and a
  controllable solar halo. The sky and haze gain a warm horizon during twilight.
- High adds a bounded sun-facing scattering lobe in the fog and highlights
  thin cloud edges. It is an analytic approximation, not volumetric clouds,
  screen-space god rays, physically based materials, or global illumination.
- Effect strength ranges from 0 to 100; the Sun halo checkbox controls the
  additional halo. Direct daylight fades smoothly at dusk, with a separate
  twilight envelope that fades before full night. Direct-sun contrast fades
  under overcast; rain/snow intensity and poor visibility also suppress glare
  and scattering. Existing headlights,
  emissive signals, UI and night texture lighting remain in their own paths.

Ground fog has separate strength (default 35%) and layer height (default 40m)
controls. Its density decreases exponentially above a local plane sampled from
already-loaded terrain below the camera; missing terrain disables the layer.
The shader estimates average density along the camera-to-vertex segment, with
a small-height-difference expansion for near-horizontal rays. This runs per
vertex and adds no scene pass or texture sample. It is a local height-layer
approximation, not a global terrain-following volumetric simulation. The
existing weather fog remains a lower bound on extinction.

The 1080p / 60 FPS button selects Balanced at 70% strength with a 35%/40m ground
layer, enables instancing
and dynamic shadows, uses two 1024px shadow cascades with blur, disables
ShadowAllShapes, selects 2x MSAA and a 2000m view distance, and enables the
existing adaptive PerformanceTuner with a target of 60. VSync retains the
user's choice. Under VSync the existing tuner aims slightly below the refresh
cap. This is a target preset, not a guarantee of 60 FPS. Cancel leaves saved
settings unchanged; changes are written on OK.

New effects use Shader Model 4 and require DirectX feature level 10.0 or newer.
The original level 9.1 and 9.3 techniques remain available and are selected on
older hardware or in Original mode. Route-defined MSTS environments also select
these original techniques. ACE/DDS textures, route files, train formats and
simulation/save formats have not been changed. Compatibility with all route
assets still requires in-game testing.

Shader techniques are resolved once when the material manager starts, avoiding
repeated capability checks and name lookups for every material draw. All eight
scenery categories (including terrain transfers, water and forest billboards)
select compatible vertex and pixel shader profiles together.

Environment.fxh contains the shared lighting/fog math. The effects use the
existing textures and render passes; no extra full-screen pass, ray marching,
render target or scene texture sample was introduced. Pixel arithmetic does
increase, especially in High mode; GPU cost has not been measured here.

## Validation and remaining limits

- The CPU regression runner passes 367 ordering checks, 1158 atmosphere/
  capability checks and 1685 precipitation/visibility/ground-layer parameter
  checks against the linked production sources. These are CPU checks, not
  visual or GPU rendering tests.
- C# compilation of RunActivity and Menu succeeds on Linux. The repository's
  existing analyzer/platform warnings remain; this is not a warning-free build.
- All 12 .fx files compiled using dotnet-mgfxc 3.8.1.303, the Windows .NET 6.0.36
  runtime and Microsoft's native d3dcompiler_47.dll from Windows SDK
  10.0.22621.2428, hosted through Wine. Original and new shader profiles were
  included. Native compilation caught legacy instruction/constant limits;
  enhanced techniques are separate SM4 entries so legacy limits are respected.
- Test tooling was installed outside the checkout. APT package hashes were
  verified through signed Debian metadata; Windows runtime/SDK archives were
  checked against their official SHA-512 metadata. TLS verification stayed on.
- There is no representative route/train content or Windows graphics device
  in this workspace. No actual game frames, visual match, GPU cost, in-game FPS
  improvement or complete playable Windows distribution has been verified.
- The newer reference screenshots also show trees/terrain reflected in water
  and low clouds crossing mountain silhouettes. Scene reflections and volumetric
  clouds have not been implemented. Water still uses route-provided textures
  and the existing surface model. More reference images are not required to
  identify these remaining gaps; implementing and tuning these passes needs
  separate renderer work and a Windows GPU validation workflow.
- The user selected https://pkor.trainsim.pl/ as the content source. The
  environment returned HTTP 403 before its page/downloads could be inspected.
  pkor.trainsim.pl was added to the saved network configuration draft, preserving
  the existing package-manager preset. The user must review/save the settings
  and publish the environment; saving the draft alone does not apply access.
  Retry the page after access changes, then inspect the published installation
  requirements and actual download destinations before selecting a package.

C# compilation-only checks, from the repository root:

```sh
dotnet build Source/RunActivity/RunActivity.csproj -t:Compile -m:2
dotnet build Source/Menu/Menu.csproj -t:Compile -m:2
```

For a normal Windows build, use the desktop workload described in Building.md
and build Source/ORTS.sln. RunActivity's PostBuildActions.bat restores the pinned
effect tool and compiles all .fx files, including their shared .fxh dependency,
into Program/Content/*.mgfx. It now stops on a failed tool restore or shader
compile, and resolves its working directory from the batch file path. The
complete Windows packaging/batch workflow has not been run here. Rebuild the
effects together with the executable; old .mgfx files lack the new parameters
and advanced techniques.

For Windows acceptance, compare original and changed Release builds on the
same route, train, weather, time of day, camera and 1080p settings. Warm the
route before recording; keep VSync and adaptive PerformanceTuner off during
comparison so caps/automatic detail changes do not confound the measurement.
Record average FPS, frame-time percentiles, CPU render/updater time and GPU
hardware. Include dense stations, forests, long-distance mountains, overcast,
night, dawn/dusk, headlights and overlapping water/glass/smoke. Check Original,
Balanced and High, then repeat on a second route and a moved camera. A measured
visual/performance comparison is required before claiming the screenshot target
or 60 FPS has been achieved.
