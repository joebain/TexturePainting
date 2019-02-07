using System;
using UnityEngine;

namespace Yucatan.Painting
{
    public class ProjectorRectangleBrush : IBrush
    {
        private Transform projector;

        public Color Colour
        {
            get; set;
        }

        public float Size
        {
            get { return Mathf.Max(size.x, size.y); }
        }

        private Vector2 size;

        public ProjectorRectangleBrush(Color colour, Vector2 size, Transform projector)
        {
            Colour = colour;
            this.size = size;
            this.projector = projector;
        }

        public int[] GetAffectedTriangles(MeshCollider meshCollider)
        {
            return BrushHelpers.GetTrianglesInSquare(meshCollider, Vector3.zero, this);
        }

        public Ray GetRayFromPoint(Vector2 point)
        {
            Vector3 point3d = projector.TransformPoint(new Vector3(point.x, point.y, 0));
            Vector3 direction = projector.TransformDirection(Vector3.forward);
            Ray ray = new Ray(point3d, direction);
            
            return ray;
        }

        public bool IsInsideBrushStroke(Vector3 worldPos)
        {
            Vector3 projectedPos = projector.InverseTransformPoint(worldPos);
            projectedPos.z = 0;
            return (projectedPos.x > -size.x * 0.5f && projectedPos.x < size.x * 0.5f &&
                projectedPos.y > -size.y * 0.5f && projectedPos.y < size.y * 0.5f);
        }
    }
}
