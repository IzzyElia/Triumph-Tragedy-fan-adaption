using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameBoard
{
    public static class EarClippingTriangulationUtility
    {
        public static bool TryTriangulate(IList<Vector3> vertices, out int[] triangles)
        {
            List<int> indices = new List<int>();

            // The list of vertex indices
            List<int> remainingVertices = new List<int>();
            for (int i = 0; i < vertices.Count; i++)
            {
                remainingVertices.Add(i);
            }

            while (remainingVertices.Count > 3)
            {
                bool earFound = false;

                for (int i = 0; i < remainingVertices.Count; i++)
                {
                    int prevIndex = remainingVertices[(i + remainingVertices.Count - 1) % remainingVertices.Count];
                    int currentIndex = remainingVertices[i];
                    int nextIndex = remainingVertices[(i + 1) % remainingVertices.Count];

                    Vector2 prevVertex = vertices[prevIndex];
                    Vector2 currentVertex = vertices[currentIndex];
                    Vector2 nextVertex = vertices[nextIndex];

                    if (IsEar(prevVertex, currentVertex, nextVertex, vertices, remainingVertices))
                    {
                        // Add the triangle indices
                        indices.Add(prevIndex);
                        indices.Add(currentIndex);
                        indices.Add(nextIndex);

                        // Remove the vertex from the polygon
                        remainingVertices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                {
                    triangles = Array.Empty<int>();
                    return false;
                }
            }

            // Add the remaining triangle
            indices.AddRange(remainingVertices);

            triangles = indices.ToArray();
            return true;
        }

        private static bool IsEar(Vector3 prevVertex, Vector3 currentVertex, Vector3 nextVertex, IList<Vector3> vertices, List<int> remainingVertices)
        {
            // Check if the triangle is convex
            if (Vector3.Cross(nextVertex - currentVertex, prevVertex - currentVertex).z >= 0)
                return false;

            // Check if any other vertex is inside the triangle
            for (int i = 0; i < vertices.Count; i++)
            {
                if (!remainingVertices.Contains(i)) continue;

                Vector3 point = vertices[i];
                if (point == prevVertex || point == currentVertex || point == nextVertex)
                    continue;

                if (PointInTriangle(point, prevVertex, currentVertex, nextVertex))
                    return false;
            }

            return true;
        }

        private static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            bool b1 = Sign(pt, v1, v2) < 0.0f;
            bool b2 = Sign(pt, v2, v3) < 0.0f;
            bool b3 = Sign(pt, v3, v1) < 0.0f;

            return ((b1 == b2) && (b2 == b3));
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
