# DirectX renderer smoke checks

Build on Windows with .NET 6 SDK, or cross-build with Windows targeting enabled.
Compile the production SceneryShader.fx, EnvironmentPostProcess.fx and
VolumetricClouds.fx using the pinned dotnet-mgfxc tool and place their .mgfx
outputs in Content beside Renderer.Graphics.exe. Run the executable with the
Windows Desktop Runtime 6.0.36. An optional first argument selects the output
PNG path. The program also writes a cloud-occlusion test image beside it.

These checks create a real DirectX GraphicsDevice, render the production
effects, and read back pixels. They also work under Wine with a working Xorg/
OpenGL driver. The reported adapter name under Wine is not evidence of the
physical GPU. Run on native Windows hardware for acceptance and FPS testing.

The synthetic images are test fixtures, not screenshots of a route or a claim
to match the user's visual references. See Docs/Modern-Renderer.md for scope.
