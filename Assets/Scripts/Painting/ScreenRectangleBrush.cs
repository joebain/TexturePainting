using System;
using UnityEngine;

namespace Yucatan.Painting
{
    public class ScreenRectangleBrush : IBrush
    {
        private UnityEngine.Camera camera;
        private Vector2 MousePos;

        public Color Colour
        {
            get; set;
        }

        public float Size
        {
            get { return Mathf.Max(size.x, size.y); }
        }

        private Vector2 size;

        public ScreenRectangleBrush(Color colour, Vector2 size, UnityEngine.Camera camera, Vector2 mousePos)
        {
            this.camera = camera;
            Colour = colour;
            MousePos = mousePos;
            this.size = size;
        }

        public int[] GetAffectedTriangles(MeshCollider meshCollider)
        {
            return BrushHelpers.GetTrianglesInSquare(meshCollider, MousePos, this);
        }

        public Ray GetRayFromPoint(Vector2 point)
        {
            return camera.ScreenPointToRay(point);
        }

        public bool IsInsideBrushStroke(Vector3 worldPos)
        {
            Vector3 screenUVPos = camera.WorldToScreenPoint(worldPos);
            screenUVPos.z = 0;
            return (screenUVPos.x > MousePos.x - size.x * 0.5f && screenUVPos.x < MousePos.x + size.x * 0.5f &&
                screenUVPos.y > MousePos.y - size.y * 0.5f && screenUVPos.y < MousePos.y + size.y * 0.5f);
        }
    }
}
