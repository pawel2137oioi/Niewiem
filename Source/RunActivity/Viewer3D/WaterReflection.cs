using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        internal bool Begin(GraphicsDevice device, float height, int cameraTileX, int cameraTileZ, Vector3 cameraEye)
        {
            if (tileX != cameraTileX || tileZ != cameraTileZ || Vector3.DistanceSquared(eye,cameraEye)>100) Valid=false;
            var scale = Math.Min(0.5f, 640f / device.PresentationParameters.BackBufferWidth);
            scale = Math.Min(scale, (float)Math.Sqrt(640f * 360 / ((double)device.PresentationParameters.BackBufferWidth * device.PresentationParameters.BackBufferHeight)));
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
            // Refresh every other frame; reuse the exact projection of the cached image.
            var refresh = !Valid || Math.Abs(Height - height) > 0.25f || ++frames % 2 == 0;
            if (refresh) { Height = height; tileX=cameraTileX; tileZ=cameraTileZ; eye=cameraEye; }
            return refresh;
        }
        public void Dispose() { Target?.Dispose(); Depth?.Dispose(); }
    }
}
