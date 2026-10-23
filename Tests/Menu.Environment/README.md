# Environment options checks

Build and run on Windows with the .NET 6 SDK/Desktop Runtime, or under Wine.
The host links the production Environment page and settings library, without
the full menu/updater. It checks preset controls, edits before OK/Cancel,
mixed feature levels, legacy/disabled controls, preservation of display/FPS
preferences and a real temporary INI save/reload. An optional output PNG path
captures the page for layout inspection. Launch the apphost EXE, not dotnet DLL.
Some Wine versions omit native controls from DrawToBitmap; use a native window
capture for visual inspection in that case. The control bounds/visibility check
and functional assertions still run independently of the exported PNG.
