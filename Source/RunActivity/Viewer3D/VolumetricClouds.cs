using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;

namespace Orts.Viewer3D
{
    internal sealed class VolumetricClouds : IDisposable
    {
        readonly GraphicsDevice device;
        readonly Effect effect;
        readonly Texture3D noise;
        internal Texture3D NoiseTexture => noise;
        RenderTarget2D clouds;
        static readonly VertexPositionTexture[] Quad = {
            new VertexPositionTexture(new Vector3(-1,1,0),new Vector2(0,0)),
            new VertexPositionTexture(new Vector3(1,1,0),new Vector2(1,0)),
            new VertexPositionTexture(new Vector3(-1,-1,0),new Vector2(0,1)),
            new VertexPositionTexture(new Vector3(1,-1,0),new Vector2(1,1)),
        };
        internal VolumetricClouds(GraphicsDevice device)
        {
            this.device = device;
            effect = new Effect(device, File.ReadAllBytes(Path.Combine(ApplicationInfo.ProcessDirectory,"Content","VolumetricClouds.mgfx")));
            const int size = 64;
            var data = new Color[size * size * size];
            uint state = 0x917345;
            for (var i = 0; i < data.Length; i++)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                var value = (byte)(state >> 24);
                data[i] = new Color(value, value, value, (byte)255);
            }
            noise = new Texture3D(device,size,size,size,false,SurfaceFormat.Color);
            noise.SetData(data);
            effect.Parameters["NoiseTexture"].SetValue(noise);
        }
        internal void Draw(RenderTarget2D scene, Texture2D depth, Matrix view, Matrix projection,
            Vector3 offset, Vector3 sun, Vector3 fog, EnvironmentParameters environment, float coverage, float ground, float time)
        {
            // Keep the same cloud target for the main view and water reflections.
            var width = Math.Clamp(device.PresentationParameters.BackBufferWidth / 4,1,640);
            var height = Math.Max(1,width * device.PresentationParameters.BackBufferHeight / device.PresentationParameters.BackBufferWidth);
            if (clouds == null || clouds.Width != width || clouds.Height != height)
            {
                clouds?.Dispose();
                clouds = new RenderTarget2D(device,width,height,false,SurfaceFormat.HalfVector4,DepthFormat.None);
            }
            var inverseView = Matrix.Invert(view);
            effect.Parameters["InverseViewProjection"].SetValue(Matrix.Invert(view * projection));
            effect.Parameters["CameraPosition"].SetValue(inverseView.Translation);
            effect.Parameters["WorldOffset"].SetValue(offset);
            effect.Parameters["SunDirection"].SetValue(sun);
            effect.Parameters["FogColor"].SetValue(fog);
            effect.Parameters["CloudLayer"].SetValue(new Vector4(ground + 750, 1000, MathHelper.Clamp(coverage,0,1),time));
            effect.Parameters["CloudLight"].SetValue(new Vector4(environment.Daylight,environment.ClearSky,environment.Strength,environment.SunVisibility));
            effect.Parameters["LowPixelSize"].SetValue(new Vector2(1f/width,1f/height));
            effect.Parameters["DepthTexture"].SetValue(depth);
            effect.Parameters["CloudTexture"].SetValue((Texture2D)null);
            device.SetRenderTarget(clouds);
            device.BlendState = BlendState.Opaque;
            DrawQuad("MarchClouds");
            device.Textures[0] = null; device.Textures[1] = null;
            device.SetRenderTarget(scene);
            effect.Parameters["CloudTexture"].SetValue(clouds);
            device.BlendState = BlendState.AlphaBlend;
            DrawQuad("CompositeClouds");
            effect.Parameters["DepthTexture"].SetValue((Texture2D)null);
            effect.Parameters["CloudTexture"].SetValue((Texture2D)null);
            for (var i = 0; i < 3; i++) device.Textures[i] = null;
            device.BlendState = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.Default;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
        }
        void DrawQuad(string technique)
        {
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.CurrentTechnique = effect.Techniques[technique];
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip,Quad,0,2);
        }
        public void Dispose() { clouds?.Dispose(); noise.Dispose(); effect.Dispose(); }
    }
}
