using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace GameBoard
{
    [ExecuteAlways]
    public class MapTile : MonoBehaviour
    {
        private Map _map;
        public Map Map
        {
            get
            {
                SetMap();
                return _map;
            }
            set
            {
                _map = value;
            }
        }

        void SetMap()
        {
            if (_map == null)
            {
                _map = GetComponentInParent<Map>();
                if (_map == null)
                    _map = transform.parent.GetComponentInParent<Map>();
                _map.RecalculateMapObjectLists();
            }
        }
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private TextMeshPro cityTextTMP;

        public Mesh Mesh => meshFilter.sharedMesh;
        public List<MapTile> connectedSpaces;
        public List<BorderReference> connectedBorders;
        public Country country;
        [SerializeField] private GameObject cityIcon;
        [SerializeField] private GameObject cityText;
        [SerializeField] private GameObject resourceMarker;
        [SerializeField] private GameObject colonialResourceMarker;
        public int resources;
        public int colonialResources;
        public int citySize;
        public TerrainType terrainType;
        [SerializeField] public bool markFunctional = false;
        [SerializeField] public bool markComplete = false;
        [SerializeField] private bool mapTextOverride;
        [SerializeField] [HideInInspector] private Vector3[] lastCalculatedVertices = new Vector3[0];
        [SerializeField] [HideInInspector] private Vector3 lastCalculatedObjectPosition = Vector3.negativeInfinity;
        [SerializeField] public Vector3 holeMarker;
        public bool containsHole { get; private set; }
        
        private void OnEnable()
        {
            meshFilter.sharedMesh = new Mesh();
        }
        
        private void OnDisable()
        {
            if (_map != null)
                Map.RecalculateMapObjectLists();
        }

        public void Recalculate()
        {
            RecalculateMesh();
        }
        
        private void OnValidate()
        {
            Recalculate();
        }

        public (Vector3[], Vector3[]) GetVertices() //main vertices, hole/island
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> hole = new List<Vector3>();
            for (int iBorder = 0; iBorder < connectedBorders.Count; iBorder++)
            {
                BorderReference borderRef = connectedBorders[iBorder];
                IList<Vector3> targetList;
                if (borderRef.borderIsHole)
                    targetList = hole;
                else
                    targetList = vertices;
                if (borderRef.reverseVertexOrder)
                {
                    for (int i = borderRef.border.points.Length - 1; i >= 0; i--)
                    {
                        Vector3 point = borderRef.border.points[i];
                        if (!targetList.Contains(point))
                            targetList.Add(point);
                    }
                }
                else
                {
                    for (int i = 0; i < borderRef.border.points.Length; i++)
                    {
                        Vector3 point = borderRef.border.points[i];
                        if (!targetList.Contains(point))
                            targetList.Add(point);
                    }
                }
            }

            return (vertices.ToArray(), hole.ToArray());
        }

        // Just generate a simple circle of size 1 for now
        public void RecalculateMesh()
        {
            
            
            (Vector3[] vertices, Vector3[] holeVertices) = GetVertices();
            containsHole = holeVertices.Length >= 3;
        
            if (vertices.Length >= 3)
            {
                string triangulationMethod = "ear clipping";
                Stopwatch stopwatch = Stopwatch.StartNew();
                
                // First save the vertices as they are serialized, before modifying the array
                lastCalculatedVertices = vertices;
                
                if (!IsClockwise(vertices))
                {
                    Array.Reverse(vertices);
                }
                
                int[] triangles;
                // Quickly try ear clipping before using Delaney triangulation
                if (containsHole || !EarClippingTriangulationUtility.TryTriangulate(vertices, out triangles))
                {
                    // If ear clipping didn't work, use Delaney
                    triangulationMethod = "Triangle.Net polygon triangulation";
                    stopwatch.Restart();
                    Polygon polygon = new Polygon(vertices.Length);
                    Vertex[] triangleNetVertices = new Vertex[vertices.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        triangleNetVertices[i] = new Vertex(vertices[i].x, vertices[i].y);
                    }
                    polygon.Add(new Contour(triangleNetVertices));
                    if (containsHole)
                    {
                        triangleNetVertices = new Vertex[holeVertices.Length];
                        for (int i = 0; i < holeVertices.Length; i++)
                        {
                            triangleNetVertices[i] = new Vertex(holeVertices[i].x, holeVertices[i].y);
                        }
                        polygon.Add(new Contour(triangleNetVertices), new Point((float)holeMarker.x, (float)holeMarker.y));
                    }
                    TriangleNet.Meshing.IMesh triangulatedMesh = polygon.Triangulate();
                    triangles = new int[triangulatedMesh.Triangles.Count * 3];
                    int t = 0;
                    foreach (TriangleNet.Topology.Triangle triangle in triangulatedMesh.Triangles)
                    {
                        triangles[t * 3] = triangle.GetVertexID(0);
                        triangles[t * 3 + 1] = triangle.GetVertexID(1);
                        triangles[t * 3 + 2] = triangle.GetVertexID(2);
                        t++;
                    }
                    vertices = new Vector3[triangulatedMesh.Vertices.Count];
                    t = 0;
                    foreach (var vertex in triangulatedMesh.Vertices)
                    {
                        vertices[t++] = new Vector3((float)vertex.X, (float)vertex.Y);
                    }
                }
                
                // Finally, adjust the mesh position to account for the position of the objects transform
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] -= transform.position;
                }

                // For some reason polygons with holes only get generated the wrong way.
                // As a workaround, flip it if it contains a hole
                if (containsHole)
                {
                    int[] flippedTriangles = triangles;
                    var triangleCount = flippedTriangles.Length / 3;
                    for(var i = 0; i < triangleCount; i++)
                    {
                        (flippedTriangles[i*3], flippedTriangles[i*3 + 1]) = (flippedTriangles[i*3 + 1], flippedTriangles[i*3]);
                    }
                    triangles = flippedTriangles;
                }

                try
                {
                    Mesh.vertices = vertices;
                    Mesh.triangles = triangles;
                    RecalculateMeshValues();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Unable to calculate mesh for {gameObject.name}");
                    throw;
                }

                
                Debug.Log($"Calculated mesh for {gameObject.name} in {stopwatch.ElapsedTicks} ticks using {triangulationMethod}");
            }
        }


        private void RecalculateMeshValues()
        {
            Mesh.RecalculateNormals();
            Mesh.RecalculateBounds();
            Mesh.RecalculateTangents();
        }

        public void RecalculateAppearance()
        {
            if (!mapTextOverride)
                cityTextTMP.text = gameObject.name;
            
            if (Application.isEditor)
                return;

            if (terrainType == TerrainType.Land)
            {
                meshRenderer.material.SetColor("_BaseColor", country.color);
            }
            else if (terrainType == TerrainType.NotInPlay)
            {
                
            }
            else if (terrainType == TerrainType.Sea)
            {
                
            }
            else if (terrainType == TerrainType.Ocean)
            {
                
            }
            else if (terrainType == TerrainType.Strait)
            {
                
            }
        }
        
        private static bool IsClockwise(IList<Vector3> vertices)
        {
            float sum = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 v1 = vertices[i];
                Vector3 v2 = vertices[(i + 1) % vertices.Count];
                sum += (v2.x - v1.x) * (v2.y + v1.y);
            }
            return sum > 0;
        }

        [Serializable]
        public class BorderReference
        {
            public MapBorder border;
            public bool reverseVertexOrder;
            public bool borderIsHole;

            public BorderReference(MapBorder border)
            {
                this.border = border;
                this.reverseVertexOrder = false;
                this.borderIsHole = false;
            }
        }
        
        public enum TerrainType
        {
            Land,
            Sea,
            Ocean,
            Strait,
            NotInPlay
        }
    }
}
