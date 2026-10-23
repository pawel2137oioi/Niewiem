using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Settings;

namespace Orts.Viewer3D
{
    internal sealed class WaterReflection : IDisposable
    {
        internal RenderTarget2D Target;
        internal RenderTarget2D Depth;
        internal RenderTargetBinding[] Bindings;
        internal Matrix ViewProjection;
        internal float Height;
        internal bool Valid;
        int frames;
        int tileX, tileZ;
        Vector3 eye;
        int previousQuality = -1;
        internal bool Begin(GraphicsDevice device, float height, int cameraTileX, int cameraTileZ, Vector3 cameraEye, int quality = 3)
        {
            var budget = GraphicsQuality.Reflections(quality);
            if (budget.MaxWidth == 0) { Valid = false; return false; }
            if (previousQuality != quality) { Valid = false; frames = 0; previousQuality = quality; }
            if (tileX != cameraTileX || tileZ != cameraTileZ || Vector3.DistanceSquared(eye,cameraEye)>100) Valid=false;
            var scale = Math.Min(0.5f, budget.MaxWidth / (float)device.PresentationParameters.BackBufferWidth);
            scale = Math.Min(scale, (float)Math.Sqrt(budget.MaxWidth * (budget.MaxWidth * 9f / 16) / ((double)device.PresentationParameters.BackBufferWidth * device.PresentationParameters.BackBufferHeight)));
            var width = Math.Max(1, (int)(device.PresentationParameters.BackBufferWidth * scale));
            var pixelsHigh = Math.Max(1, (int)(device.PresentationParameters.BackBufferHeight * scale));
            if (Target == null || Target.Width != width || Target.Height != pixelsHigh)
            {
                Target?.Dispose();
                Depth?.Dispose();
                Target = new RenderTarget2D(device, width, pixelsHigh, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
                Depth = new RenderTarget2D(device,width,pixelsHigh,false,SurfaceFormat.Single,DepthFormat.None,0,RenderTargetUsage.PreserveContents);
                Bindings = new[] { new RenderTargetBinding(Target),new RenderTargetBinding(Depth) };
                Valid = false;
            }
            // Reuse the exact projection of the cached image between quality-dependent refreshes.
            var refresh = !Valid || Math.Abs(Height - height) > 0.25f || ++frames % budget.UpdateFrames == 0;
            if (refresh) { Height = height; tileX=cameraTileX; tileZ=cameraTileZ; eye=cameraEye; }
            return refresh;
        }
        public void Dispose() { Target?.Dispose(); Depth?.Dispose(); }
    }
}
