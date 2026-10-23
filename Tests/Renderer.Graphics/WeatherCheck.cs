using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D;

internal struct WeatherParticleVertex : IVertexType
{
    public Vector4 Start, End, TileCorner;
    public VertexDeclaration VertexDeclaration => Declaration;
    static readonly VertexDeclaration Declaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
        new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Position, 1),
        new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Position, 2));
}

internal static class WeatherCheck
{
    internal static void Run(GraphicsDevice device, string output)
    {
        const int width = 512, height = 256;
        using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
        using var black = new Texture2D(device, 1, 1); black.SetData(new[] { Color.Black });
        using var sky = new Effect(device, File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Content", "SkyShader.mgfx")));
        sky.Parameters["WorldViewProjection"].SetValue(Matrix.Identity);
        sky.Parameters["SkyMapTexture"].SetValue(black); sky.Parameters["StarMapTexture"].SetValue(black);
        sky.Parameters["SkyColor"].SetValue(Vector3.One);
        sky.Parameters["EnvironmentSunGlare"].SetValue(1f);
        sky.CurrentTechnique = sky.Techniques["SkyAdvanced"];
        var skyQuad = new[] {
            new VertexPositionNormalTexture(new Vector3(-1,1,0),new Vector3(-1.8f,1,-1),Vector2.Zero),
            new VertexPositionNormalTexture(new Vector3(1,1,0),new Vector3(1.8f,1,-1),Vector2.UnitX),
            new VertexPositionNormalTexture(new Vector3(-1,-1,0),new Vector3(-1.8f,-0.05f,-1),Vector2.UnitY),
            new VertexPositionNormalTexture(new Vector3(1,-1,0),new Vector3(1.8f,-0.05f,-1),Vector2.One) };
        Color[] Read()
        {
            device.SetRenderTarget(null); var data = new Color[width * height]; target.GetData(data); return data;
        }
        void Begin()
        {
            device.SetRenderTarget(target); device.Clear(Color.Transparent);
            device.BlendState = BlendState.Opaque; device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.None;
        }
        void Save(string name, Color[] pixels)
        {
            using var texture = new Texture2D(device, width, height); texture.SetData(pixels);
            using var file = File.Create(Path.ChangeExtension(output, name + ".png")); texture.SaveAsPng(file, width, height);
        }
        Color[] Sky(float elevation, float east = 0, float overcast = 0, float rain = 0)
        {
            var environment = new EnvironmentParameters(2,100,elevation,overcast,false,rain);
            var sun = new Vector3(east,elevation,-(float)Math.Sqrt(Math.Max(0,1-east*east-elevation*elevation)));
            sky.Parameters["LightVector"].SetValue(new Vector4(sun,1));
            sky.Parameters["Environment"].SetValue(new Vector4(1,environment.Daylight,environment.ClearSky,1));
            sky.Parameters["EnvironmentWeather"].SetValue(new Vector4(environment.Twilight,environment.SunVisibility,0,0));
            Begin(); sky.CurrentTechnique.Passes[0].Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip,skyQuad,0,2);
            return Read();
        }
        var day = Sky(0.75f); Save("day",day);
        var dusk = Sky(0.015f); Save("sunset",dusk);
        var night = Sky(-0.6f); Save("night",night);
        var storm = Sky(0.75f,0,1,1); Save("overcast",storm);
        var horizonPixel = dusk[220 * width + width / 2];
        if (horizonPixel.R < horizonPixel.B * 1.3) throw new Exception("Sunset horizon has no warm light.");
        if (night.Average(c=>(double)c.R+c.G+c.B) >= day.Average(c=>(double)c.R+c.G+c.B)*0.2)
            throw new Exception("Night sky did not darken.");
        var rise = Sky(0.02f,0.4f); var set = Sky(0.02f,-0.4f);
        double mirroredError = 0;
        for(var y=0;y<height;y++) for(var x=0;x<width;x++)
            mirroredError += Math.Abs(rise[y*width+x].R-set[y*width+width-1-x].R);
        if (mirroredError/(width*height)>0.1) throw new Exception("Sunrise and sunset lighting are directionally inconsistent.");
        using (var skyDepth = new RenderTarget2D(device,width,height,false,SurfaceFormat.Single,DepthFormat.None))
        {
            device.SetRenderTargets(new RenderTargetBinding(target),new RenderTargetBinding(skyDepth));
            device.Clear(Color.Transparent); sky.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip,skyQuad,0,2);
            device.SetRenderTarget(null); var distances=new float[width*height]; skyDepth.GetData(distances);
            if(distances.Any(d=>d<39999 || d>40001)) throw new Exception("Sky left undefined cloud-occlusion depth.");
        }
        Console.WriteLine("PASS: compiled sky, warm twilight, dark night, overcast and mirrored sunrise/sunset.");

        using var precipitation = new Effect(device,File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Content","PrecipitationShader.mgfx")));
        precipitation.Parameters["worldViewProjection"].SetValue(Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,2,0.1f,200));
        precipitation.Parameters["invView"].SetValue(Matrix.Identity);
        precipitation.Parameters["particleSize"].SetValue(1f);
        precipitation.Parameters["currentTime"].SetValue(0.5f);
        precipitation.Parameters["PrecipitationVisibility"].SetValue(1000f);
        precipitation.CurrentTechnique = precipitation.Techniques["PrecipitationAdvanced"];
        Color[] Particle(float liquidity, float sunHeight, float distance = 2)
        {
            var e = new EnvironmentParameters(2,100,sunHeight,0.7f,false);
            precipitation.Parameters["LightVector"].SetValue(new Vector3(0,sunHeight,0));
            precipitation.Parameters["PrecipitationEnvironment"].SetValue(new Vector4(1,e.Daylight,e.Twilight,liquidity));
            var corners = new[] {0,1,3,2}.Select(i=>new WeatherParticleVertex {
                Start=new Vector4(0,1,-distance,0),End=new Vector4(0,-1,-distance,1),TileCorner=new Vector4(0,0,i,0) }).ToArray();
            Begin(); device.BlendState = BlendState.NonPremultiplied;
            precipitation.CurrentTechnique.Passes[0].Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleStrip,corners,0,2);
            return Read();
        }
        int ParticleHeight(Color[] pixels) => Enumerable.Range(0,height).Count(y=>pixels.Skip(y*width).Take(width).Any(c=>c.A>2));
        var drops=Particle(1,0.8f); Save("rain-particle",drops);
        var flakes=Particle(0,0.8f); Save("snow-particle",flakes);
        if (ParticleHeight(drops)<ParticleHeight(flakes)*3 || ParticleHeight(flakes)<3)
            throw new Exception("Rain streak / snow flake proportions are incorrect.");
        var dark=Particle(0,-0.6f);
        if (dark.Max(c=>c.R) >= flakes.Max(c=>c.R)*0.4) throw new Exception("Snow is too bright at night.");
        if (Particle(1,0.8f,0.25f).Any(c=>c.A>1)) throw new Exception("Near-camera precipitation did not fade.");
        Console.WriteLine("PASS: compiled rain/snow shape, night lighting and near-camera fade.");
    }
}
