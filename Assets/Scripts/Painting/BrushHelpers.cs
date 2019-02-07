using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yucatan.Painting
{
    struct RayResult
    {
        public RayResult(bool hit, RaycastHit hitInfo = default(RaycastHit))
        {
            this.hit = hit;
            this.hitInfo = hitInfo;
        }
        
        public bool hit;
        public RaycastHit hitInfo;
    }

    class BrushHelpers
    {
        private const float MIN_TRIANGLE_WIDTH = 2f;

        internal static void AddTriangleToListIfHit(IBrush brush, MeshCollider meshCollider, Vector2 point, List<int> newTriangles, Dictionary<Vector2, RayResult> rayResults)
        {
            RayResult rayResult;
            if (rayResults.ContainsKey(point))
            {
                rayResult = rayResults[point];
            }
            else
            {
                Ray ray = brush.GetRayFromPoint(point);
                RaycastHit hit;
                if (meshCollider.Raycast(ray, out hit, float.MaxValue))
                {
                    rayResult = new RayResult(true, hit);
                }
                else
                {
                    rayResult = new RayResult(false);
                }
                rayResults[point] = rayResult;
            }

            if (rayResult.hit)
            {
                if (!newTriangles.Contains(rayResult.hitInfo.triangleIndex))
                {
                    newTriangles.Add(rayResult.hitInfo.triangleIndex);
                }
            }
        }

        internal static int[] GetTrianglesInSquare(MeshCollider meshCollider, Vector2 centre, IBrush brush)
        {
            Ray ray = brush.GetRayFromPoint(centre);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.black, 1);
            RaycastHit hit;

            List<int> triangles = new List<int>();

            if (meshCollider.Raycast(ray, out hit, float.MaxValue))
            {
                triangles.Add(hit.triangleIndex);
                
                Stack<Rect> rects = new Stack<Rect>();
                Dictionary<Vector2, RayResult> rayResults = new Dictionary<Vector2, RayResult>(); 

                rects.Push(new Rect(centre + (Vector2.down + Vector2.left) * brush.Size * 0.5f, new Vector2(brush.Size, brush.Size)));
                int rectsChecked = 0;
                while (rects.Count > 0)
                {
                    rectsChecked++;
                    Rect rect = rects.Pop();

                    Vector2 BL = new Vector2(rect.xMin, rect.yMin);
                    Vector2 BR = new Vector2(rect.xMax, rect.yMin);
                    Vector2 TL = new Vector2(rect.xMin, rect.yMax);
                    Vector2 TR = new Vector2(rect.xMax, rect.yMax);

                    List<int> theseTriangles = new List<int>();

                    AddTriangleToListIfHit(brush, meshCollider, TR, theseTriangles, rayResults);
                    AddTriangleToListIfHit(brush, meshCollider, TL, theseTriangles, rayResults);
                    AddTriangleToListIfHit(brush, meshCollider, BR, theseTriangles, rayResults);
                    AddTriangleToListIfHit(brush, meshCollider, BL, theseTriangles, rayResults);


                    if (theseTriangles.Count > 1 && rect.width > MIN_TRIANGLE_WIDTH)
                    {
                        float size = rect.width * 0.5f;
                        rects.Push(new Rect(rect.xMin, rect.yMin, size, size));
                        rects.Push(new Rect(rect.xMin, rect.yMin + size, size, size));
                        rects.Push(new Rect(rect.xMin + size, rect.yMin, size, size));
                        rects.Push(new Rect(rect.xMin + size, rect.yMin + size, size, size));
                    }

                    // add the triangles we don't have already
                    for (int t = 0; t < theseTriangles.Count; t++)
                    {
                        if (!triangles.Contains(theseTriangles[t]))
                        {
                            triangles.Add(theseTriangles[t]);
                        }
                    }
                }
                //Debug.Log("checked " + rectsChecked + " rects");
                //Debug.Log("ray results dict size is " + rayResults.Count);
            }

            return triangles.ToArray();
        }
    }
}
