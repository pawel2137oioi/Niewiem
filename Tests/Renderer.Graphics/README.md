# DirectX renderer smoke checks

Build on Windows with .NET 6 SDK, or cross-build with Windows targeting enabled.
Compile the production SceneryShader.fx, SkyShader.fx, PrecipitationShader.fx,
EnvironmentPostProcess.fx and VolumetricClouds.fx using the pinned dotnet-mgfxc tool and place their .mgfx
outputs in Content beside Renderer.Graphics.exe. Run the executable with the
Windows Desktop Runtime 6.0.36. An optional first argument selects the output
PNG path. The program also writes a cloud-occlusion test image beside it.
SceneryShader.fx and VolumetricClouds.fx share CloudDensity.fxh; recompile both
when that include changes. Launch the EXE apphost so assets resolve beside it.
WeatherLighting.fxh is shared by scenery, sky, volume and precipitation;
recompile all its consumers after changes.

The cloud quality check compares world-aligned pixels after a one-pixel
off-axis camera pan, checks repeatability, daylight cloud brightness, quality
changes and a clear-weather frame. It exports cloud-quality and cloud-pan
images as synthetic fixtures. Render targets preserve scene contents as the
production compositor does. Failures exit with a nonzero code without opening
an unhandled-exception dialog.

The horizon fixture checks row-band residuals against a dense 128-interval
reference and alternates reflection/main buffers. The weather fixtures check
twilight, night, overcast, mirrored sunrise/sunset colors, explicit sky depth
in an MRT, rain/snow proportions and near-camera/night precipitation fades.
Exported PNGs are synthetic shader fixtures, not route screenshots.

These checks create a real DirectX GraphicsDevice, render the production
effects, and read back pixels. They also work under Wine with a working Xorg/
OpenGL driver. The reported adapter name under Wine is not evidence of the
physical GPU. Run on native Windows hardware for acceptance and FPS testing.

The synthetic images are test fixtures, not screenshots of a route or a claim
to match the user's visual references. See Docs/Modern-Renderer.md for scope.
