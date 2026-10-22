using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

internal struct SceneVertex : IVertexType
{
    public Vector3 Position, Normal;
    public Vector2 UV;
    public Matrix Instance;
    public VertexDeclaration VertexDeclaration => Declaration;
    static readonly VertexDeclaration Declaration = new VertexDeclaration(
        new VertexElement(0,VertexElementFormat.Vector3,VertexElementUsage.Position,0),
        new VertexElement(12,VertexElementFormat.Vector3,VertexElementUsage.Normal,0),
        new VertexElement(24,VertexElementFormat.Vector2,VertexElementUsage.TextureCoordinate,0),
        new VertexElement(32,VertexElementFormat.Vector4,VertexElementUsage.TextureCoordinate,1),
        new VertexElement(48,VertexElementFormat.Vector4,VertexElementUsage.TextureCoordinate,2),
        new VertexElement(64,VertexElementFormat.Vector4,VertexElementUsage.TextureCoordinate,3),
        new VertexElement(80,VertexElementFormat.Vector4,VertexElementUsage.TextureCoordinate,4));
    internal SceneVertex(float x,float y) { Position=new Vector3(x,y,0); Normal=Vector3.Up; UV=new Vector2((x+2)/4,(1-y)/2); Instance=Matrix.Identity; }
}
internal static class SceneCheck
{
    internal static void Run(GraphicsDevice device)
    {
        using var effect = new Effect(device,File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Content","SceneryShader.mgfx")));
        using var scene = new RenderTarget2D(device,256,128,false,SurfaceFormat.HalfVector4,DepthFormat.Depth24,0,RenderTargetUsage.PreserveContents);
        using var depth = new RenderTarget2D(device,256,128,false,SurfaceFormat.Single,DepthFormat.None,scene.MultiSampleCount,RenderTargetUsage.PreserveContents);
        using var white = new Texture2D(device,16,16); white.SetData(Enumerable.Repeat(Color.White,256).ToArray());
        effect.Parameters["ImageTexture"].SetValue(white);
        effect.Parameters["World"].SetValue(Matrix.Identity);
        effect.Parameters["View"].SetValue(Matrix.CreateLookAt(new Vector3(0,0,3),Vector3.Zero,Vector3.Up));
        effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographic(8,4,0.1f,100));
        effect.Parameters["ViewerPos"].SetValue(new Vector3(0,0,3));
        effect.Parameters["LightVector_ZFar"].SetValue(new Vector4(0,1,0,100));
        effect.Parameters["ZBias_Lighting"].SetValue(new Vector4(0,1,0,0));
        effect.Parameters["Environment"].SetValue(new Vector4(1,1,1,0));
        effect.Parameters["EnvironmentWeather"].SetValue(new Vector4(0,1,0,0.025f));
        effect.Parameters["NightColorModifier"].SetValue(1f);
        effect.Parameters["Fog"].SetValue(new Vector4(0.5f,0.6f,0.7f,0));
        effect.CurrentTechnique = effect.Techniques["ImageAdvanced"];
        var vertices = new[] { new SceneVertex(-2,1),new SceneVertex(2,1),new SceneVertex(-2,-1),
            new SceneVertex(-2,-1),new SceneVertex(2,1),new SceneVertex(2,-1) };
        var distances=new float[256*128];
        var radiance=new HalfVector4[256*128];
        foreach (var technique in effect.Techniques.Where(t=>t.Name.StartsWith("Probe")).Concat(new[] {effect.Techniques["ImageAdvanced"]}))
        {
            device.SetRenderTargets(new RenderTargetBinding(scene),new RenderTargetBinding(depth));
            device.Clear(ClearOptions.Target|ClearOptions.DepthBuffer,Color.Transparent,1,0);
            device.BlendState=BlendState.Opaque; device.RasterizerState=RasterizerState.CullNone;device.DepthStencilState=DepthStencilState.Default;
            effect.CurrentTechnique=technique;
            effect.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,2);
            device.SetRenderTarget(null);
            depth.GetData(distances);scene.GetData(radiance);
            Console.WriteLine(technique.Name+" peak="+radiance.Max(p=>p.ToVector4().X)+", center="+radiance[64*256+128].ToVector4());
        }
        if (distances[64*256+128]<2.9f || distances[64*256+128]>3.2f) throw new Exception("Opaque scene did not write metric depth.");
        if (!radiance.Any(p=>p.ToVector4().X>1)) throw new Exception("HDR scene clipped sunlight to LDR.");
        Console.WriteLine("PASS: actual scenery shader, HDR radiance > 1, metric depth MRT and matching MSAA targets.");
        using (var finish = new Orts.Viewer3D.EnvironmentPostProcess(device))
        {
            finish.Apply(scene,1,1,2,true,false);
            var finished=new Color[256*128];device.GetBackBufferData(finished);
            if (finished[0].R>2 || finished[64*256+128].R<200) throw new Exception("HDR scene composition lost its black background or highlights.");
        }
        device.BlendState=BlendState.Opaque;device.RasterizerState=RasterizerState.CullNone;device.DepthStencilState=DepthStencilState.Default;
        using var pointTexture = new Texture2D(device,1,1);
        pointTexture.SetData(new[] { Color.White });
        effect.Parameters["ImageTexture"].SetValue(pointTexture);
        for (var i=0;i<vertices.Length;i++) vertices[i].UV=Vector2.Zero;
        device.SetRenderTargets(new RenderTargetBinding(scene),new RenderTargetBinding(depth));
        device.Clear(ClearOptions.Target|ClearOptions.DepthBuffer,Color.Transparent,1,0);
        effect.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,2);
        device.SetRenderTarget(null);scene.GetData(radiance);
        if (!radiance.Any(p=>p.ToVector4().X>1)) throw new Exception("Constant UV / one-pixel texture became nonfinite or black.");
        Console.WriteLine("PASS: finite constant-UV material with one-pixel texture.");
        using var density = new Texture3D(device,4,4,4,false,SurfaceFormat.Color);
        density.SetData(Enumerable.Repeat(Color.White,64).ToArray());
        effect.Parameters["CloudNoiseTexture"].SetValue(density);
        effect.Parameters["CloudShadowLayer"].SetValue(new Vector4(0,1000,1,0));
        device.SetRenderTargets(new RenderTargetBinding(scene),new RenderTargetBinding(depth));
        device.Clear(ClearOptions.Target|ClearOptions.DepthBuffer,Color.Transparent,1,0);
        effect.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,2);
        device.SetRenderTarget(null);scene.GetData(radiance);
        var shadow = radiance[64*256+128].ToVector4().X;
        if (shadow<0.1f || shadow>1.15f) throw new Exception("Cloud density did not shade direct sunlight.");
        Console.WriteLine("PASS: 3D cloud density shades scenery through the production vertex shader.");
        effect.Parameters["CloudShadowLayer"].SetValue(Vector4.Zero);
        effect.CurrentTechnique=effect.Techniques["TransferAdvanced"];
        var transfers=vertices.Select(v=>new VertexPositionTexture(v.Position,v.UV)).ToArray();
        device.SetRenderTargets(new RenderTargetBinding(scene),new RenderTargetBinding(depth));
        device.Clear(ClearOptions.Target|ClearOptions.DepthBuffer,Color.Transparent,1,0);
        effect.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleList,transfers,0,2);
        device.SetRenderTarget(null);scene.GetData(radiance);
        if (radiance[64*256+128].ToVector4().X<0.1f) throw new Exception("Normal-less transfer became nonfinite or black with specular disabled.");
        effect.CurrentTechnique=effect.Techniques["ImageAdvanced"];
        for(var i=0;i<vertices.Length;i++) vertices[i].Normal=Vector3.Zero;
        device.SetRenderTargets(new RenderTargetBinding(scene),new RenderTargetBinding(depth));
        device.Clear(ClearOptions.Target|ClearOptions.DepthBuffer,Color.Transparent,1,0);
        effect.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,2);
        device.SetRenderTarget(null);scene.GetData(radiance);
        if (radiance[64*256+128].ToVector4().X<0.1f) throw new Exception("Zero-length mesh normal produced nonfinite or black material.");
        Console.WriteLine("PASS: normal-less transfer and zero-length shape normal remain finite.");
        using var waterMask=new Texture2D(device,16,16);
        effect.CurrentTechnique=effect.Techniques["WaterAdvanced"];
        effect.Parameters["ImageTexture"].SetValue(waterMask);
        effect.Parameters["ReferenceAlpha"].SetValue(10/255f);
        for(var i=0;i<vertices.Length;i++)vertices[i].UV=new Vector2((vertices[i].Position.X+2)/4,(1-vertices[i].Position.Y)/2);
        foreach(var alpha in new byte[]{0,128})
        {
            waterMask.SetData(Enumerable.Repeat(new Color((byte)80,(byte)100,(byte)100,alpha),256).ToArray());
            device.SetRenderTargets(new RenderTargetBinding(scene),new RenderTargetBinding(depth));
            device.Clear(ClearOptions.Target|ClearOptions.DepthBuffer,Color.Transparent,1,0);
            effect.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,2);
            device.SetRenderTarget(null);scene.GetData(radiance);
            var opacity=radiance[64*256+128].ToVector4().W;
            if(Math.Abs(opacity-alpha/255f)>0.01f)throw new Exception("Water changed the route's shoreline/layer alpha mask.");
        }
        Console.WriteLine("PASS: water coverage and partial-opacity route masks are preserved.");
    }
}
