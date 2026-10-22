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
64-cubed noise texture drives moving density within a height layer, 32 bounded
view samples, two light samples, Beer extinction, ambient light and forward
scattering. Scene depth terminates each ray. Depth-aware upsampling limits
leakage at foreground silhouettes. Noise-driven cloud shadows modulate direct
sunlight per vertex. Clear weather produces no clouds; Ctrl and the =/+ key
increase cloud cover, Ctrl and - decrease it using the existing weather controls.

Cloud resolution defaults to 50% of screen dimensions, with 25–100% available
in Environment options; width and total pixels are capped at 1280 and 1280×720.
The previous preview used 25% with a 640-pixel width cap. 25% is the cheaper
choice; High visual preset chooses 50%. The noise texture is generated once
and reused. There is no
temporal history or full-resolution volume march. A bounded flat cloud layer
is an approximation and needs visual tuning, especially around mountains and
when flying through clouds.

The cloud motion update removes pixel-hashed jitter, which was fixed to the
screen and crawled over the cloud field when panning. Rays use deterministic
midpoints across the entire visible interval; the old 250-metre step cap could
omit the far end of shallow rays. RGB now packs three independently seeded,
periodic value-noise fields with quintic interpolation, generated once on the
CPU. One filtered lookup replaces three independent voxel-noise lookups per
density sample, including in the shared scenery-shadow field. No temporal
accumulation means there is no history ghosting; residual undersampling can
still occur at distant edges. Daylight cloud ambient light is brighter, and
the bloom threshold excludes ordinary sky/foliage to retain scene contrast.
Higher resolution increases GPU work; reduced texture sampling does not
establish a net frame-time improvement on native hardware.

Water uses animated metre-based normals, Fresnel reflection and sunlight.
High renders the prepared opaque scenery, sky and cloud volume into a
half-resolution reflected view every other frame, reusing shadow maps.
Underwater geometry is clipped. Water and UI do not recurse into reflections.
The nearest visible water height selects one reflection plane; other heights
fall back to a sky approximation. Reflections reuse the prepared geometry, so
objects outside the main view's culling volume may be missing. Arbitrary sloped
water, refraction and underwater depth absorption remain unfinished.

Balanced uses cheaper sky reflections and reduced-resolution bloom. HDR and
single-channel metric depth use matching non-MSAA targets. FXAA in the final
scene pass provides AA when antialiasing is enabled; the legacy path keeps its
MSAA behavior. This avoids relying on optional multisampled HDR format support.

GPU resources are reused between frames, resized on screen changes, and
disposed on the render thread. Reflection images keep their own projection
and are invalidated when camera tiles, position or water height change.

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
versus 0.191 for this update; the 0.4 threshold rejects the previous version.
This isolates screen-dependent sampling changes rather than treating genuine
cloud motion as an error. Repeatability, clear sky, daylight brightness and
25/50/100% resource resizing are checked too. The exported sky images are
synthetic fixtures, not proof of a matching route screenshot or measured FPS.

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
