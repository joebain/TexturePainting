using UnityEngine;

namespace Yucatan.Painting
{
    public class ScreenCircleBrush : IBrush
    {
        private UnityEngine.Camera camera;

        public Color Colour { get; set; }
        public float Size { get; set; }
        public Vector2 MousePos;

        public ScreenCircleBrush(Color colour, float size, UnityEngine.Camera camera, Vector2 mousePos)
        {
            this.camera = camera;
            Colour = colour;
            MousePos = mousePos;
            Size = size*0.5f;
        }

        public bool IsInsideBrushStroke(Vector3 worldPos)
        {
            Vector3 screenUVPos = camera.WorldToScreenPoint(worldPos);
            screenUVPos.z = 0;
            float dist = Vector3.Distance(MousePos, screenUVPos);
            return dist < Size;
        }

        public Ray GetRayFromPoint(Vector2 point)
        {
            return camera.ScreenPointToRay(point);
        }

        public int[] GetAffectedTriangles(MeshCollider meshCollider)
        {
            return BrushHelpers.GetTrianglesInSquare(meshCollider, MousePos, this);
        }
    }
}
