using UnityEngine;

namespace Yucatan.Painting
{
    public class Painter
    {
        private static Mesh prevMesh;
        private static int[] meshTriangles;
        private static Vector3[] meshVertices;
        private static Vector2[] meshUVs;

        public static bool Paint(IBrush brush, Texture2D texture, MeshCollider meshCollider)
        {
            int[] triangles = brush.GetAffectedTriangles(meshCollider);

            Mesh mesh = meshCollider.sharedMesh;
            Transform meshTransform = meshCollider.transform;

            if (mesh != prevMesh)
            {
                prevMesh = mesh;
                meshTriangles = mesh.triangles;
                meshVertices = mesh.vertices;
                meshUVs = mesh.uv;
            }

            Rect bigUVRect = new Rect(-1,-1,0,0);
            for (int t = 0; t < triangles.Length; t++)
            {
                int triangleIndex = triangles[t];

                int[] triangle = new int[3];
                triangle[0] = meshTriangles[triangleIndex * 3];
                triangle[1] = meshTriangles[triangleIndex * 3 + 1];
                triangle[2] = meshTriangles[triangleIndex * 3 + 2];

                Vector2[] triangleUVs = new Vector2[3];
                triangleUVs[0] = meshUVs[triangle[0]];
                triangleUVs[1] = meshUVs[triangle[1]];
                triangleUVs[2] = meshUVs[triangle[2]];

                Rect uvRect = GetUVRect(triangleUVs);
                bigUVRect = MergeRects(bigUVRect, uvRect);
            }

            int x = Mathf.FloorToInt(bigUVRect.x * texture.width);
            int y = Mathf.FloorToInt(bigUVRect.y * texture.height);
            int width = Mathf.FloorToInt(bigUVRect.width * texture.width);
            int height = Mathf.FloorToInt(bigUVRect.height * texture.height);

            Color[] cols = texture.GetPixels(x, y, width, height);
            //Debug.Log("colouring a " + width + "x" + height + " patch");

            for (int t = 0; t < triangles.Length; t++)
            {
                int triangleIndex = triangles[t];

                int[] triangle = new int[3];
                triangle[0] = meshTriangles[triangleIndex * 3];
                triangle[1] = meshTriangles[triangleIndex * 3 + 1];
                triangle[2] = meshTriangles[triangleIndex * 3 + 2];

                Vector3[] triangleVerts = new Vector3[3];
                triangleVerts[0] = meshVertices[triangle[0]];
                triangleVerts[1] = meshVertices[triangle[1]];
                triangleVerts[2] = meshVertices[triangle[2]];

                Vector2[] triangleUVs = new Vector2[3];
                triangleUVs[0] = meshUVs[triangle[0]];
                triangleUVs[1] = meshUVs[triangle[1]];
                triangleUVs[2] = meshUVs[triangle[2]];

                Rect uvRect = GetUVRect(triangleUVs);
                ColourPixels(ref cols, brush, triangleUVs, triangleVerts, meshTransform, uvRect, bigUVRect, texture.width, texture.height);
            }

            texture.SetPixels(x, y, width, height, cols);
            texture.Apply();

            return triangles.Length > 0;
        }

        private static Rect MergeRects(Rect a, Rect b)
        {
            if (a.width == 0 || a.height == 0) return b;
            if (b.width == 0 || b.height == 0) return a;
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static Rect GetUVRect(Vector2[] uvs, int t = 0)
        {
            Vector2 uv = uvs[t];
            Rect uvRect = new Rect(uv, Vector2.zero);

            for (int i = 1; i < 3; i++)
            {
                uv = uvs[t + i];
                if (uv.x > uvRect.xMax)
                {
                    uvRect.xMax = uv.x;
                }
                if (uv.y > uvRect.yMax)
                {
                    uvRect.yMax = uv.y;
                }
                if (uv.x < uvRect.xMin)
                {
                    uvRect.xMin = uv.x;
                }
                if (uv.y < uvRect.yMin)
                {
                    uvRect.yMin = uv.y;
                }
            }

            return uvRect;
        }


        private static int ColourPixels(ref Color[] cols, IBrush brush, Vector2[] triangleUVs, Vector3[] triangleVerts, Transform modelTransform, Rect uvRect, Rect bigUVRect, int texWidth, int texHeight)
        {
            int pixels = 0;

            int xOffset = Mathf.FloorToInt((uvRect.xMin - bigUVRect.xMin) * texWidth);
            int yOffset = Mathf.FloorToInt((uvRect.yMin - bigUVRect.yMin) * texHeight);
            int width = Mathf.FloorToInt(uvRect.width * texWidth);
            int height = Mathf.FloorToInt(uvRect.height * texHeight);

            int colsStride = Mathf.FloorToInt(bigUVRect.width * texWidth);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int i = (y+yOffset) * colsStride + x+xOffset;

                    Vector2 texPos = new Vector2(uvRect.xMin + (float)x / texWidth, uvRect.yMin + (float)y / texHeight);

                    Vector3 bary = GetBarycentricCoordFromUV(texPos, triangleUVs, triangleVerts);
                    float baryMag = bary.magnitude;
                    float min = -0.1f, max = 1.1f, maxMag = 1.1f;
                    if (!(bary.x < min || bary.y < min || bary.z < min || bary.x > max || bary.y > max || bary.z > max || baryMag > maxMag))
                    {
                        Vector3 modelPosOfUV = bary.x * triangleVerts[0] + bary.y * triangleVerts[1] + bary.z * triangleVerts[2];
                        Vector3 worldPosOfUV = modelTransform.TransformPoint(modelPosOfUV);
                        if (brush.IsInsideBrushStroke(worldPosOfUV))
                        {
                            cols[i] = brush.Colour;
                            pixels++;
                        }
                    }
                }
            }
            return pixels;
        }

        private static Vector3 GetBarycentricCoordFromUV(Vector2 uv, Vector2[] triangleUVs, Vector3[] triangleVerts)
        {
            Vector2 a = triangleUVs[0], b = triangleUVs[1], c = triangleUVs[2];
            Vector2 p = uv;

            // get barycentric coords in uv space:
            Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }
    }
}
