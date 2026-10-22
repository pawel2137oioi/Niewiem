using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using var form = new Form { ClientSize = new System.Drawing.Size(256, 128) };
        using var device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef,
            new PresentationParameters { DeviceWindowHandle = form.Handle, BackBufferWidth = 256, BackBufferHeight = 128,
                BackBufferFormat = SurfaceFormat.Color, DepthStencilFormat = DepthFormat.Depth24, IsFullScreen = false });
        Console.WriteLine("Graphics adapter: " + GraphicsAdapter.DefaultAdapter.Description);
        using var process = new EnvironmentPostProcess(device);
        using var scene = new RenderTarget2D(device, 256, 128, false, SurfaceFormat.Color, DepthFormat.None);
        using var batch = new SpriteBatch(device);
        using var white = new Texture2D(device, 1, 1);
        white.SetData(new[] { Color.White });
        void Pattern(Color background, bool square)
        {
            device.SetRenderTarget(scene);
            device.Clear(background);
            if (square) { batch.Begin(); batch.Draw(white, new Rectangle(96, 32, 64, 64), Color.White); batch.End(); }
            device.SetRenderTarget(null);
        }
        Color[] Read()
        {
            var pixels = new Color[256 * 128]; device.GetBackBufferData(pixels); return pixels;
        }
        Pattern(Color.Black, false);
        process.Apply(scene, 1, 1, 1, true);
        if (Read().Any(c => c.R > 1 || c.G > 1 || c.B > 1)) throw new Exception("Black frame gained light.");
        Pattern(new Color(90, 130, 70), false);
        process.Apply(scene, 0, 1, 1, false);
        var unchanged = Read()[1000];
        if (Math.Abs(unchanged.G - 130) > 1 || Math.Abs(unchanged.R - 90) > 1) throw new Exception("Zero strength changed scene.");
        process.Apply(scene, 1, 1, 1, false);
        var graded = Read()[1000];
        if (graded == unchanged || graded.G == 0) throw new Exception("Grading is missing or black.");
        Pattern(Color.Black, true);
        process.Apply(scene, 1, 1, 1, false);
        var noBloom = Read();
        process.Apply(scene, 1, 1, 1, true);
        var bloom = Read();
        if (bloom[64 * 256 + 91].R <= noBloom[64 * 256 + 91].R) throw new Exception("Bloom did not spread outside highlights.");
        if (bloom[64 * 256 + 128].R < 150 || bloom[0].R > 2) throw new Exception("Bad quad orientation or bloom extent.");
        process.Apply(scene, 1, 1, 2, true); // Exercise resize from quarter to half-resolution resources.
        var output = args.Length > 0 ? args[0] : "graphics-smoke.png";
        using var screenshot = new Texture2D(device, 256, 128);
        screenshot.SetData(Read());
        using (var file = File.Create(output)) screenshot.SaveAsPng(file, 256, 128);
        Console.WriteLine("PASS: actual compiled grading/bloom, zero strength, black level, image layout and resized targets.");
        using var volume = new VolumetricClouds(device);
        using var depth = new Texture2D(device,256,128,false,SurfaceFormat.Single);
        var distances = new float[256 * 128];
        for (var y = 0; y < 128; y++) for (var x = 0; x < 256; x++) distances[y * 256 + x] = x < 128 ? 1 : 40000;
        distances[64*256+200]=1; // Thin foreground geometry missed by the low-resolution ray samples.
        depth.SetData(distances);
        Pattern(Color.Black,false);
        volume.Draw(scene,depth,Matrix.CreateLookAt(new Vector3(0,-100,0),new Vector3(0,500,-1000),Vector3.Up),
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,2,0.1f,40000),Vector3.Zero,
            Vector3.Normalize(new Vector3(1,1,-1)),new Vector3(0.5f),new EnvironmentParameters(2,100,0.8f,1,false),1,-750,0);
        device.SetRenderTarget(null);
        var cloudPixels = new Color[256*128];scene.GetData(cloudPixels);
        if (cloudPixels[64*256+64].R > 2) throw new Exception("Clouds leaked in front of a near opaque surface.");
        if (cloudPixels[64*256+200].R > 2) throw new Exception("Cloud upsampling leaked over a one-pixel foreground object.");
        if (!cloudPixels.Where((c,i)=>i%256>160).Any(c=>c.R>15)) throw new Exception("No volumetric cloud radiance rendered.");
        using (var file = File.Create(Path.ChangeExtension(output,"clouds.png"))) scene.SaveAsPng(file,256,128);
        Console.WriteLine("PASS: compiled 3D cloud integration and depth occlusion.");
        SceneCheck.Run(device);
    }
}
