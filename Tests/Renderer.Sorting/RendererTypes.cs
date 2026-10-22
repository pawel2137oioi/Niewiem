// Minimal CPU-only stand-ins for GPU-facing types referenced by RenderItem.
// Tests link the production collection/comparers, and never create a graphics device.
namespace Orts.Viewer3D
{
    public class Material { }
    public class WaterMaterial : Material { }
    public class RenderPrimitive
    {
        public float SortIndex;
    }
    public enum ShapeFlags { None }
}
