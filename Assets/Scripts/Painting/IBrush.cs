using UnityEngine;

namespace Yucatan.Painting
{
    public interface IBrush
    {
        bool IsInsideBrushStroke(Vector3 worldPos);
        Color Colour { get; }
        float Size { get; }
        int[] GetAffectedTriangles(MeshCollider meshCollider);
        Ray GetRayFromPoint(Vector2 point);
    }
}
