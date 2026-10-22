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
            for (var z = 0; z < size; z++)
                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                        data[(z * size + y) * size + x] = new Color(
                            ValueNoise(x, y, z, 8, 0x917345u),
                            ValueNoise(x, y, z, 16, 0x529acdu),
                            ValueNoise(x, y, z, 32, 0x376b19u));
            noise = new Texture3D(device,size,size,size,false,SurfaceFormat.Color);
            noise.SetData(data);
            effect.Parameters["NoiseTexture"].SetValue(noise);
        }
        internal void Draw(RenderTarget2D scene, Texture2D depth, Matrix view, Matrix projection,
            Vector3 offset, Vector3 sun, Vector3 fog, EnvironmentParameters environment, float coverage, float ground, float time,
            int resolutionPercent = 50)
        {
            // Keep the same cloud target for the main view and water reflections.
            // Bound total pixels as well as width for portrait/ultrawide screens.
            var screenWidth = device.PresentationParameters.BackBufferWidth;
            var screenHeight = device.PresentationParameters.BackBufferHeight;
            var scale = Math.Clamp(resolutionPercent, 25, 100) / 100f;
            scale = Math.Min(scale, 1280f / screenWidth);
            scale = Math.Min(scale, (float)Math.Sqrt(1280f * 720 / ((double)screenWidth * screenHeight)));
            var width = Math.Max(1, (int)(screenWidth * scale));
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

        // Quintic interpolation gives a periodic, smooth field rather than
        // independent voxel noise. Generated once; no CPU noise work per frame.
        static float ValueNoise(int x, int y, int z, int period, uint seed)
        {
            var p = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * (period / 64f);
            var ix = (int)p.X; var iy = (int)p.Y; var iz = (int)p.Z;
            var fx = Fade(p.X - ix); var fy = Fade(p.Y - iy); var fz = Fade(p.Z - iz);
            var mask = period - 1;
            var a = MathHelper.Lerp(Hash(ix & mask, iy & mask, iz & mask, seed), Hash((ix + 1) & mask, iy & mask, iz & mask, seed), fx);
            var b = MathHelper.Lerp(Hash(ix & mask, (iy + 1) & mask, iz & mask, seed), Hash((ix + 1) & mask, (iy + 1) & mask, iz & mask, seed), fx);
            var c = MathHelper.Lerp(Hash(ix & mask, iy & mask, (iz + 1) & mask, seed), Hash((ix + 1) & mask, iy & mask, (iz + 1) & mask, seed), fx);
            var d = MathHelper.Lerp(Hash(ix & mask, (iy + 1) & mask, (iz + 1) & mask, seed), Hash((ix + 1) & mask, (iy + 1) & mask, (iz + 1) & mask, seed), fx);
            return MathHelper.Lerp(MathHelper.Lerp(a, b, fy), MathHelper.Lerp(c, d, fy), fz);
        }

        static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        static float Hash(int x, int y, int z, uint seed)
        {
            unchecked
            {
                uint h = seed ^ ((uint)x * 0x8da6b343u) ^ ((uint)y * 0xd8163841u) ^ ((uint)z * 0xcb1ab31fu);
                h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; h ^= h >> 16;
                return (h >> 8) / 16777215f;
            }
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
