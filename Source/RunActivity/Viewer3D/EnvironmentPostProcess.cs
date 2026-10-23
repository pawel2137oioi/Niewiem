using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using ORTS.Settings;

namespace Orts.Viewer3D
{
    // Owned by the render process. No resource creation or allocation per frame.
    internal sealed class EnvironmentPostProcess : IDisposable
    {
        readonly GraphicsDevice device;
        readonly Effect effect;
        readonly Texture2D black;
        RenderTarget2D bloomA, bloomB;
        static readonly VertexPositionTexture[] Quad = {
            new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
            new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
        };

        internal EnvironmentPostProcess(GraphicsDevice device)
        {
            this.device = device;
            effect = new Effect(device, File.ReadAllBytes(Path.Combine(ApplicationInfo.ProcessDirectory, "Content", "EnvironmentPostProcess.mgfx")));
            black = new Texture2D(device, 1, 1);
            black.SetData(new[] { Color.Black });
        }

        internal void Apply(RenderTarget2D scene, float strength, float daylight, int quality, bool bloom, bool antiAliasing = true, bool colorGrading = true, int bloomQuality = -1)
        {
            var level = GraphicsQuality.ResolveBloom(bloomQuality, bloom, quality);
            bloom = level > 0;
            var viewport = device.Viewport;
            effect.Parameters["SampleStep"].SetValue(new Vector2(1f / scene.Width, 1f / scene.Height));
            if (bloom)
            {
                var divisor = GraphicsQuality.BloomDivisor(level);
                var scale = Math.Min(1f / divisor, Math.Min(1920f / scene.Width, 1080f / scene.Height));
                var width = Math.Max(1, (int)(scene.Width * scale));
                var height = Math.Max(1, (int)(scene.Height * scale));
                if (bloomA == null || bloomA.Width != width || bloomA.Height != height)
                {
                    bloomA?.Dispose(); bloomB?.Dispose();
                    bloomA = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                    bloomB = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                }
                effect.Parameters["SampleStep"].SetValue(new Vector2(1f / scene.Width, 1f / scene.Height));
                Pass(scene, bloomA, "ExtractBloom");
                effect.Parameters["BlurDirection"].SetValue(new Vector2(1f / width, 0));
                Pass(bloomA, bloomB, "BlurBloom");
                effect.Parameters["BlurDirection"].SetValue(new Vector2(0, 1f / height));
                Pass(bloomB, bloomA, "BlurBloom");
            }
            device.SetRenderTarget(null);
            device.Viewport = viewport;
            effect.Parameters["Grade"].SetValue(new Vector4(strength, daylight, bloom ? 0.14f : 0, antiAliasing ? 1 : 0));
            effect.Parameters["ColorGradeStrength"].SetValue(colorGrading ? strength : 0);
            effect.Parameters["BloomTexture"].SetValue(bloom ? (Texture2D)bloomA : black);
            Pass(scene, null, "CompositeScene");
            // Release sampled render targets before the next frame binds one for writing.
            effect.Parameters["SceneTexture"].SetValue((Texture2D)null);
            effect.Parameters["BloomTexture"].SetValue((Texture2D)null);
            device.Textures[0] = null;
            device.Textures[1] = null;
            device.DepthStencilState = DepthStencilState.Default;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }

        void Pass(Texture2D source, RenderTarget2D target, string technique)
        {
            device.Textures[0] = null;
            device.Textures[1] = null;
            device.SetRenderTarget(target);
            device.BlendState = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.Parameters["SceneTexture"].SetValue(source);
            effect.CurrentTechnique = effect.Techniques[technique];
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, Quad, 0, 2);
        }

        public void Dispose()
        {
            bloomA?.Dispose(); bloomB?.Dispose(); black.Dispose(); effect.Dispose();
        }
    }
}
