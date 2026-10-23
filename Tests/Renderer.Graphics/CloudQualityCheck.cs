using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D;
using ORTS.Settings;

internal static class CloudQualityCheck
{
    internal static void Run(GraphicsDevice device, string output)
    {
        const int width = 512, height = 256;
        var pp = device.PresentationParameters;
        pp.BackBufferWidth = width; pp.BackBufferHeight = height;
        device.Reset(pp);
        using var volume = new VolumetricClouds(device);
        using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        using var depth = new Texture2D(device, width, height, false, SurfaceFormat.Single);
        depth.SetData(Enumerable.Repeat(40000f, width * height).ToArray());
        var view = Matrix.CreateLookAt(new Vector3(0, 100, 0), new Vector3(0, 1100, -1500), Vector3.Up);
        var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 3, 2, 0.1f, 40000);
        var environment = new EnvironmentParameters(2, 100, 0.8f, 0.65f, false);
        Color[] Render(Matrix p, int resolution = 50, float coverage = 0.65f, Matrix? camera = null, int budget = 48, bool reflection = false, int quality = 3)
        {
            device.SetRenderTarget(target);
            device.Clear(new Color(110, 155, 200));
            volume.Draw(target, depth, camera ?? view, p, Vector3.Zero, Vector3.Normalize(new Vector3(-1, 2, -1)),
                new Vector3(0.55f, 0.65f, 0.75f), environment, coverage, 0, 0, resolution, reflection, budget, quality);
            device.SetRenderTarget(null);
            var pixels = new Color[width * height]; target.GetData(pixels); return pixels;
        }
        void Save(string suffix, Color[] pixels)
        {
            using var texture = new Texture2D(device, width, height);
            texture.SetData(pixels);
            using var stream = File.Create(Path.ChangeExtension(output, suffix + ".png"));
            texture.SaveAsPng(stream, width, height);
        }

        var still = Render(projection);
        Save("cloud-quality", still);
        var repeat = Render(projection);
        if (!still.SequenceEqual(repeat)) throw new Exception("Static cloud field is not deterministic.");
        // An off-axis projection moves the same world rays by exactly one pixel.
        // Re-aligning images removes physical motion; residual grain comes from
        // the screen-space sampling/filter, which caused the reported crawling.
        var pan = projection;
        pan.M31 -= 2f / width;
        var moved = Render(pan);
        double error = 0;
        var count = 0;
        for (var y = 2; y < height - 2; y++)
            for (var x = 2; x < width - 3; x++)
            {
                var a = still[y * width + x]; var b = moved[y * width + x + 1];
                error += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                count += 3;
            }
        var meanError = error / count;
        Console.WriteLine($"Cloud pan: mean aligned error={meanError:F3} / 255.");
        if (meanError > 0.4) throw new Exception("Cloud sampling crawls during a one-pixel pan.");
        if (!still.Any(c => c.R > 160)) throw new Exception("Daylight clouds lost their bright body.");
        Save("cloud-pan", moved);
        // Exercise quality changes on the same resource owner and preserve clear weather.
        Render(projection, 25);
        Render(projection, 100);
        var clear = Render(projection, 50, 0);
        if (clear.Any(c => c.R != 110 || c.G != 155 || c.B != 200)) throw new Exception("Clear weather retained cloud radiance.");
        Console.WriteLine("PASS: cloud camera-pan stability, deterministic field, bright daylight, quality resize and clear sky.");
        var horizonView = Matrix.CreateLookAt(new Vector3(0,100,0),new Vector3(0,180,-1000),Vector3.Up);
        var horizon = Render(projection,camera:horizonView);
        var reference = Render(projection,camera:horizonView,budget:128);
        Save("cloud-horizon",horizon); Save("cloud-horizon-reference",reference);
        var rowErrors = new double[height];
        for (var y=0;y<height;y++) for(var x=0;x<width;x++)
            rowErrors[y] += (horizon[y*width+x].R-reference[y*width+x].R)/(double)width;
        double stripeEnergy=0;
        for(var y=1;y<height-1;y++) stripeEnergy += Math.Pow(rowErrors[y]-(rowErrors[y-1]+rowErrors[y+1])*0.5,2);
        var stripeRms=Math.Sqrt(stripeEnergy/(height-2));
        Console.WriteLine($"Horizon row-band residual against 128 intervals: {stripeRms:F3} / 255.");
        if(stripeRms>1.5) throw new Exception("Horizon integration forms horizontal bands relative to the dense reference.");
        Render(projection,reflection:true,budget:24);
        Render(projection); // Separate reflection/main targets must survive alternating views.
        Console.WriteLine("PASS: horizon integration against dense reference and separate reflection resources.");
        for (var quality=1; quality<=4; quality++)
        {
            var budget=GraphicsQuality.Clouds(quality);
            var pixels=Render(projection,budget.ResolutionPercent,budget:budget.Intervals,quality:quality);
            if(!pixels.Any(c=>c.R>150)) throw new Exception("Cloud level produced an empty volume: "+quality);
        }
        var disabled=Render(projection,quality:0);
        if(disabled.Any(c=>c.R!=110 || c.G!=155 || c.B!=200)) throw new Exception("Cloud quality Off drew a volume.");
        using var reflections=new WaterReflection();
        for(var level=1;level<=4;level++)
        {
            if(!reflections.Begin(device,0,0,0,new Vector3(0,2,0),level)) throw new Exception("Reflection quality switch did not invalidate its target.");
            reflections.Valid=true;
            for(var i=1;i<=GraphicsQuality.Reflections(level).UpdateFrames;i++)
                if(reflections.Begin(device,0,0,0,new Vector3(0,2,0),level)!=(i==GraphicsQuality.Reflections(level).UpdateFrames))
                    throw new Exception("Incorrect reflection update cadence: "+level);
        }
        if(reflections.Begin(device,0,0,0,new Vector3(0,2,0),0) || reflections.Valid) throw new Exception("Disabled reflections remained valid.");
        using var post=new EnvironmentPostProcess(device);
        for(var level=0;level<=4;level++) post.Apply(target,1,1,2,true,false,true,level);
        Console.WriteLine("PASS: all cloud, reflection and bloom levels, including Off and reflection refresh cadence.");
    }
}
