using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Triangulation
{
    public static class DelaunayTriangulator
    {
        public static int[] GetTriangulation(Vector3[] originalPoints)
        {
            Vector2[] points = new Vector2[originalPoints.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(originalPoints[i].x, originalPoints[i].y);
            }
            
             // pointList is a set of coordinates defining the points to be triangulated


            //triangulation := empty triangle mesh data structure
            List<Triangle2d> triangles = new List<Triangle2d>();
            Dictionary<Edge2d, int> badTriangleEdgeCounter = new Dictionary<Edge2d, int>();

            // Calculate the enclosing-triangle
            // must be large enough to completely contain all the points in pointList
            // add super-triangle to triangulation
            float min_x = float.PositiveInfinity;
            float min_y = float.PositiveInfinity;
            float max_x = float.NegativeInfinity;
            float max_y = float.NegativeInfinity;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].x < min_x)
                    min_x = points[i].x;
                if (points[i].y < min_y)
                    min_y = points[i].y;
                if (points[i].x > max_x)
                    max_x = points[i].x;
                if (points[i].y > max_y)
                    max_y = points[i].y;
            }
            float dx = (max_x - min_x) * 10;
            float dy = (max_y - min_y) * 10;
            Triangle2d superTriangle = new Triangle2d(
                new Vector2(min_x - dx, min_y - dy * 3),
                new Vector2(min_x - dx, min_y + dy),
                new Vector2(max_x + dx * 3, max_y + dy)
                );
            triangles.Add(superTriangle);


            // add all the points one at a time to the triangulation
            //for each point in pointList do
            List<Triangle2d> badTriangles = new List<Triangle2d>();
            foreach (Vector2 point in points)
            {
                //badTriangles := empty set
                badTriangles.Clear();

                // first find all the triangles that are no longer valid due to the insertion
                //for each triangle in triangulation do
                foreach (Triangle2d triangle in triangles)
                {
                    //if point is inside circumcircle of triangle
                    //add triangle to badTriangles
                    if (PointIsInTriangleCircumcircle(point, triangle))
                    {
                        badTriangles.Add(triangle);
                        AddToEdgeCounter(triangle, badTriangleEdgeCounter);
                    }
                }



                //polygon := empty set
                List<Edge2d> polygon = new List<Edge2d>();

                // find the boundary of the polygonal hole
                //for each triangle in badTriangles do
                //for each edge in triangle do
                //if edge is not shared by any other triangles in badTriangles
                //add edge to polygon
                foreach (KeyValuePair<Edge2d, int> edge_numEdges in badTriangleEdgeCounter)
                {
                    if (edge_numEdges.Value == 1)
                        polygon.Add(edge_numEdges.Key);
                }

                // remove them from the data structure
                //for each triangle in badTriangles do
                //remove triangle from triangulation
                foreach (Triangle2d triangle in badTriangles)
                    triangles.Remove(triangle);
                badTriangleEdgeCounter.Clear();

                // re-triangulate the polygonal hole
                //for each edge in polygon do
                //newTri := form a triangle from edge to point
                //add newTri to triangulation
                foreach (Edge2d edge in polygon)
                {
                    Triangle2d newTriangle = new Triangle2d(point, edge.A, edge.B);
                    triangles.Add(newTriangle);
                }
            }


            // done inserting points, now clean up
            //for each triangle in triangulation
            //if triangle contains a vertex from original super-triangle
            //remove triangle from triangulation
            foreach (Triangle2d triangle in triangles)
            {
                foreach (Vector2 superVertex in superTriangle.Vertices)
                {
                    if (triangle.Vertices.Contains(superVertex))
                    {
                        badTriangles.Add(triangle);
                        break;
                    }
                }
            }
            foreach (Triangle2d badTriangle in badTriangles)
                triangles.Remove(badTriangle);
            
            Dictionary<Vector2, int> pointsIndices = new Dictionary<Vector2, int>();
            for (int i = 0; i < originalPoints.Length; i++)
            {
                if (!pointsIndices.TryAdd(originalPoints[i], i))
                    throw new ArgumentException("Duplicate vertices in vertex array");
            }
            int[] output = new int[triangles.Count * 3];
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle2d triangle = triangles[i];
                int i2 = i * 3;
                output[i2] = pointsIndices[triangle.A];
                output[i2 + 1] = pointsIndices[triangle.B];
                output[i2 + 2] = pointsIndices[triangle.C];
            }
            return output;
        }
        static float Sign(Vector2 a, Vector2 b, Vector2 c)
        {
            return (a.x - c.x) * (b.y - c.y) - (b.x - c.x) * (a.y - c.y);
        }
        static bool PointIsInTriangleCircumcircle(Vector2 point, Triangle2d triangle)
        {
            return Vector2.Distance(point, triangle.circumcenter) <= triangle.circumradius;
            /*
            float ax_ = a.X - point.X;
            float ay_ = a.Y - point.Y;
            float bx_ = b.X - point.X;
            float by_ = b.Y - point.Y;
            float cx_ = c.X - point.X;
            float cy_ = c.Y - point.Y;
            return (
                 //         | ax-dx, ay-dy, (ax-dx)² + (ay-dy)² |
                 //   det = | bx-dx, by-dy, (bx-dx)² + (by-dy)² |
                 //         | cx-dx, cy-dy, (cx-dx)² + (cy-dy)² |
                 
                (ax_ * ax_ + ay_ * ay_) * (bx_ * cy_ - cx_ * by_) -
                (bx_ * bx_ + by_ * by_) * (ax_ * cy_ - cx_ * ay_) +
                (cx_ * cx_ + cy_ * cy_) * (ax_ * by_ - bx_ * ay_)
            ) > 0;
            */
        }
        static void AddToEdgeCounter(Triangle2d triangle, Dictionary<Edge2d, int> edgeCounter)
        {
            foreach (Edge2d edge in triangle.Edges)
            {
                if (edgeCounter.ContainsKey(edge))
                    edgeCounter[edge] += 1;
                else
                    edgeCounter.Add(edge, 1);
            }
        }
        static void SubtractFromEdgeCounter(Triangle2d triangle, Dictionary<Edge2d, int> edgeCounter)
        {
            foreach (Edge2d edge in triangle.Edges)
            {
                if (edgeCounter.ContainsKey(edge))
                    edgeCounter[edge] -= 1;
                else
                    throw new InvalidOperationException("Would result in a negative edge count (meaning something may have gone wrong!)");
            }
        }
    }
}