# Modern renderer prototype

The requested TSRE reference appearance has not been matched or certified 1:1.
This work replaces stages of Open Rails' renderer while keeping its simulation,
route/train formats and controls. It is not a completed conversion to another
game engine. The older atmosphere-only download remains a separate baseline.

## Pipeline

The DirectX 10+ path renders into floating-point HDR color targets. Opaque
scenery, terrain and vegetation also write metric camera distance into a second
target, without a separate depth draw. Depth-aware reduced-resolution clouds
are composited after opaque scenery and before glass, particles and cab
interiors. Scene grading and bloom finish before labels, HUD and 2D overlays.
The 2D cab retains its colors. Original/MSTS selects the legacy path.
The preview requires DirectX 10+; older hardware has not been validated with
the expanded effect binaries.

High clouds are actual 3D density integration, not cloud billboards. A fixed
64-cubed noise texture drives moving density within a height layer, 12–48 bounded
integration intervals with two density samples per interval, two light samples,
Beer extinction, ambient light and forward
scattering. Scene depth terminates each ray. Depth-aware upsampling limits
leakage at foreground silhouettes. Noise-driven cloud shadows modulate direct
sunlight per vertex. Clear weather produces no clouds; Ctrl and the =/+ key
increase cloud cover, Ctrl and - decrease it using the existing weather controls.

Cloud resolution defaults to 50% of screen dimensions, with 25–100% available
in Environment options; width and total pixels are capped at 1280 and 1280×720.
The previous preview used 25% with a 640-pixel width cap. 25% is the cheaper
choice; High visual preset chooses 50%. The noise texture is generated once
and reused, including its filtered mip chain. There is no
temporal history or full-resolution volume march. A bounded flat cloud layer
is an approximation and needs visual tuning, especially around mountains and
when flying through clouds.

The cloud motion update removes pixel-hashed jitter, which was fixed to the
screen and crawled over the cloud field when panning. Rays use deterministic
quadrature across the entire visible interval; the old 250-metre step cap could
omit the far end of shallow rays. RGB now packs three independently seeded,
periodic value-noise fields with quintic interpolation, generated once on the
CPU. One filtered lookup replaces three independent voxel-noise lookups per
density sample, including in the shared scenery-shadow field. No temporal
accumulation means there is no history ghosting; residual undersampling can
still occur at distant edges. Daylight cloud ambient light is brighter, and
the bloom threshold excludes ordinary sky/foliage to retain scene contrast.
Higher resolution increases GPU work; reduced texture sampling does not
establish a net frame-time improvement on native hardware.

The horizon update combines two-point Gauss integration with adaptive interval
counts and mip-filtered density. The mip footprint includes the derivative of
the ray/slab intersection: at a grazing angle, a pixel covers far more world
space than its ray angle alone suggests. Density fades from 14–18 km to soften
the bounded volume's far edge. The sky explicitly writes a 40-km distance to
the MRT used for cloud occlusion, avoiding unwritten render-target output.
This reduces sampling artifacts but does not establish that all stripes in
the user's route view are eliminated; native visual verification remains open.

Water uses animated metre-based normals, Fresnel reflection and sunlight.
High renders the prepared opaque scenery, sky and cloud volume into a
reduced-resolution reflected view every other frame, reusing shadow maps.
Reflection color/depth are now capped at 640×360 pixels. Reflection clouds
use a separate reusable target at half the main cloud dimensions and a
24-interval ceiling. This avoids allocating/resizing shared targets when
alternating the two views, and limits work that was duplicated at main-view
cloud quality. The main view's cloud resolution setting is preserved.
Underwater geometry is clipped. Water and UI do not recurse into reflections.
The nearest visible water height selects one reflection plane; other heights
fall back to a sky approximation. Reflections reuse the prepared geometry, so
objects outside the main view's culling volume may be missing. Arbitrary sloped
water, refraction and underwater depth absorption remain unfinished.

Balanced uses cheaper sky reflections and reduced-resolution bloom. HDR and
single-channel metric depth use matching non-MSAA targets. FXAA in the final
scene pass provides AA when antialiasing is enabled; the legacy path keeps its
MSAA behavior. This avoids relying on optional multisampled HDR format support.
The modern backbuffer also avoids MSAA: FXAA already handles scene edges,
so multisampling the final fullscreen copy added a redundant buffer/resolve.

GPU resources are reused between frames, resized on screen changes, and
disposed on the render thread. Reflection images keep their own projection
and are invalidated when camera tiles, position or water height change.

The High / FPS preset keeps volumetric clouds, at 33% resolution, while
disabling scene water reflections, choosing two 1024-pixel shadow maps,
limiting view distance to 1500 m, enabling model instancing and setting the
existing performance tuner's target to 60 FPS. Applying or cancelling the
options follows the existing deferred-save behavior. The target is not a
measured result, and higher FPS on a particular machine is not guaranteed.

## Dawn, dusk and precipitation

Advanced sky uses a continuous analytic day/twilight/night gradient and a
bounded solar disk/halo. The same solar tint drives scenery and cloud light;
it depends on solar height rather than separate east/west legacy glow rules.
Night stars remain supported. This is an approximation, not a full spectral
atmosphere or a physically based material conversion.

Advanced rain uses narrow soft streaks, snow uses small soft flakes, and mixed
liquidity blends their shapes. Existing particle simulation, wind, collision
and route weather inputs remain in use. Size variation, near-camera/far fades,
weather visibility, day/night brightness and twilight tint replace constant
white sprites. Original/MSTS uses the legacy precipitation technique. Wet
material authoring, puddles, splash particles, windshield simulation and
dynamic snow accumulation have not been added by this change.

## Validation

Tests/Renderer.Graphics links the production postprocessor, volume renderer
and parameter code and executes compiled DirectX effects. It checks black
levels, zero strength, bloom spread, resized targets, cloud depth occlusion,
HDR values above 1, opaque metric-depth output, and a constant-UV material
using a one-pixel texture. That last case originally produced nonfinite color
in the test driver; explicit LOD sampling now handles zero UV derivatives.
This does not establish the cause of the user's black route surfaces.

Additional pixel checks cover zero-length mesh normals, normal-less transfers,
cloud shadows through the production vertex shader, and transparent/partially
opaque water masks. Water keeps the route's coverage alpha; wave motion only
changes its reflection normal, not the shoreline mask.

The graphics runner operates through Wine with Windows Desktop Runtime and an
Xorg display. These are synthetic render checks; they are not proof of native
Windows performance or reference-scene appearance. The existing CPU runner
also checks sorting and bounded atmosphere/weather parameters.

The cloud-motion regression renders the same world rays with a one-pixel
off-axis camera shift and compares aligned RGB pixels. In the fixed fixture
under Wine, the previous renderer measured 0.668 mean error in 8-bit units,
versus 0.191 for the earlier motion update and 0.203 for the horizon update;
the 0.4 threshold rejects the first preview.
This isolates screen-dependent sampling changes rather than treating genuine
cloud motion as an error. Repeatability, clear sky, daylight brightness and
25/50/100% resource resizing are checked too. The exported sky images are
synthetic fixtures, not proof of a matching route screenshot or measured FPS.

The horizon fixture compares row-band residuals against a 128-interval dense
reference (0.033 / 255 RMS in this Wine run). It also alternates main/reflection
targets. Weather fixtures execute the production sky and precipitation effects:
warm sunset horizon, dark night, overcast, mirrored east/west lighting,
explicit sky depth, rain/snow proportions, night brightness and camera fades.
These are shader checks, not a fresh acceptance run on the full route or
proof of SimRail/reference appearance. Full route testing described below
belongs to the earlier preview.
The first new rainy-route launch also ended in a render watchdog timeout
under Wine, so the synthetic checks alone do not establish game stability.
The retry reached an actual rainy cab and external EU07 view at 18:30 on
Północna Polska. A further launch after the backbuffer-MSAA removal also rendered
the cab with the antialiasing setting enabled. This confirms loading/rendering,
not stable Windows performance, a measured speedup or visual acceptance.

Północna Polska (December 2023), its Global/Sound package and matching timetable
stock were downloaded from PKOR and extracted using the RAR decoder from 7-Zip.
The Białogard–Stargard path with EU07-465 loaded into an actual cab and external
3D view under Wine. This is a functional route smoke check, not visual acceptance.
It exposed water coverage and material issues that synthetic tests now cover.
The route still visibly differs from the reference; no 1:1 result is claimed.
Large black surfaces remain visible in the route view, and water coverage/
reflection appearance still needs investigation against the legacy renderer.
The shader guards do not establish that those route-level defects are fixed.
One repeat launch terminated with a render-thread watchdog timeout under Wine;
the next launch rendered the cab and exterior. Shader warmup is a possibility,
not a confirmed diagnosis. This is an unresolved preview stability issue.

The route check also required guarding unsupported optional WMI/PDH telemetry,
handling missing cross-thread watchdog stacks on .NET 6, and allowing bounded
progress during the first driver shader warmup. Native Windows behavior still
needs verification. Packaged logging writes to Logs/OpenRailsLog.txt beside
the extracted application, rather than relying on a desktop location.

The target remains 1080p / 60 FPS, without a measured guarantee. High adds
volume and reflection work, so compare both frame time and image quality on
the same route, train, camera, weather and time. The debug HUD reports the
active renderer and fallback reason. Native GPU timings remain necessary.

## Remaining scope

The prototype does not supply new route models, vegetation meshes or material
textures. It does not yet implement a full PBR material workflow, normal-map
authoring/import, screen-space ambient occlusion, temporal AA, general scene
reflections or a completely rewritten simulation engine. Lighting still
preserves parts of the legacy material conventions for compatibility. Matching
the references requires those remaining material/asset tasks and repeated
side-by-side route testing; changing engine names does not provide those assets.

PKOR access now works. The Północna Polska route, global/sound and timetable/
stock downloads are being used locally for validation; third-party content is
not part of the public engine source or Windows ZIP.
