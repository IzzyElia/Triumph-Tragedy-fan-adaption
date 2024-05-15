using System;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using TriangleNet.Geometry;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

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

        public Mesh Mesh
        {
            get => meshFilter.sharedMesh;
            set => meshFilter.sharedMesh = value;
        } 
        public List<MapTile> connectedSpaces;
        public List<BorderReference> connectedBorders;
        [FormerlySerializedAs("country")] public MapCountry mapCountry;
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
        string MeshSavePath => $"Assets/Resources/{name}.mesh";
        string MeshLoadPath => $"{name}";

        private void OnEnable()
        {
            meshFilter.sharedMesh = null;
        }
        
        private void OnDisable()
        {
            if (_map != null)
                Map.RecalculateMapObjectLists();
        }

        public void Recalculate()
        {
            if (_map != null)
                meshFilter.sharedMesh = _map.fallbackMapTileMesh;
            if (markComplete)
            {
                SetMeshToFlashed();
            }
            else
            {
                meshFilter.sharedMesh = RecalculateMesh();

            }
            
            // TODO TEMP Set a random test color
            Random.InitState(name.GetHashCode());
            Color color = new Color(Random.value, Random.value, Random.value);

            if (terrainType == TerrainType.Sea)
            {
                color = (color + Color.cyan*2) / 3f;
            }
            if (terrainType == TerrainType.Ocean)
            {
                color = (color + Color.blue*2) / 3f;
            }
            if (terrainType == TerrainType.NotInPlay)
            {
                color = (color + Color.magenta*2) / 3f;
            }
            if (terrainType == TerrainType.Land)
            {
                color = (color + new Color(0.5f, 0.2f, 0.2f)*2) / 3f;
            }

            meshRenderer.sharedMaterial.color = color;
            //if (!markComplete)
            //    AutoReorderBorders();
        }
        
        private void OnValidate()
        {
            //Recalculate();
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
        
        public Mesh RecalculateMesh()
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
                    Mesh mesh = new Mesh();
                    mesh.vertices = vertices;
                    mesh.triangles = triangles;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    Debug.Log($"Calculated mesh for {gameObject.name} in {stopwatch.ElapsedTicks} ticks using {triangulationMethod}");
                    return mesh;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Unable to calculate mesh for {gameObject.name}");
                    throw;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Save the mesh and set it in the inspector
        /// </summary>
        public void FlashMesh()
        {
            if (markComplete) UnflashMesh();
            Mesh mesh = RecalculateMesh();
            AssetDatabase.CreateAsset(mesh, MeshSavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();  
            markComplete = SetMeshToFlashed();
        }

        public bool SetMeshToFlashed()
        {
            UnityEngine.Mesh loadedMesh = Resources.Load<Mesh>(MeshLoadPath);
            if (loadedMesh != null)
            {
                SerializedObject serializedObject = new SerializedObject(meshFilter);
                SerializedProperty meshProperty = serializedObject.FindProperty("m_Mesh");
                meshProperty.objectReferenceValue = loadedMesh;
                serializedObject.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(this);
                return true;
            }
            else
            {
                Debug.LogWarning($"Unable to load mesh for {name}. Unflashing...");
                UnflashMesh();
                return false;
            }
        }

        public void UnflashMesh()
        {
            markComplete = false;
            meshFilter.sharedMesh = null;
            AssetDatabase.DeleteAsset(MeshSavePath);
            AssetDatabase.Refresh();
        }

        public void AutoReorderBorders()
        {
            // Calculate the center of all points
            Vector3 center = Vector3.zero;
            int totalPoints = 0;
        
            for (int i = 0; i < connectedBorders.Count; i++)
            {
                BorderReference borderRef = connectedBorders[i];
                totalPoints += borderRef.border.points.Length;
                for (int j = 0; j < borderRef.border.points.Length; j++)
                {
                    center += borderRef.border.points[j];
                }
            }
            center /= totalPoints;
            connectedBorders.Sort((a, b) =>
            {
                Vector3 aOther;
                Vector3 bOther;
                if (a.border.points.Length == 2) {
                    aOther = a.border.connectedMapTiles[0] == this
                    ? a.border.connectedMapTiles[1].transform.position
                    : a.border.connectedMapTiles[0].transform.position;
                }
                else
                {
                    aOther = a.border.transform.position;
                }

                if (b.border.points.Length == 2)
                {
                    bOther = b.border.connectedMapTiles[0] == this
                        ? b.border.connectedMapTiles[1].transform.position
                        : b.border.connectedMapTiles[0].transform.position;
                }
                else
                {
                    bOther = b.border.transform.position;
                }

                Vector3 aDir = aOther - center;
                Vector3 bDir = bOther - center;
                float aAngle = Mathf.Atan2(aDir.y, aDir.x);
                float bAngle = Mathf.Atan2(bDir.y, bDir.x);
                return aAngle.CompareTo(bAngle);
            });
            //Then, reverse the vertex order of any border where the last point is counterclockwise compared to the first, again calculated using the mesh center
            for (int i = 0; i < connectedBorders.Count; i++)
            {
                BorderReference borderRef = connectedBorders[i];
                if (borderRef.border.points.Length == 0)
                {
                    Debug.LogWarning($"{borderRef.border.name} has a length of zero");
                    continue;
                }
                Vector3 firstPoint = borderRef.border.points[0];
                Vector3 lastPoint = borderRef.border.points[^1];
                Vector3 firstDir = firstPoint - center;
                Vector3 lastDir = lastPoint - center;
                float firstAngle = Mathf.Atan2(firstDir.y, firstDir.x);
                float lastAngle = Mathf.Atan2(lastDir.y, lastDir.x);
                // Normalize angles to range [0, 2 * Mathf.PI]
                firstAngle = (firstAngle < 0) ? firstAngle + 2 * Mathf.PI : firstAngle;
                lastAngle = (lastAngle < 0) ? lastAngle + 2 * Mathf.PI : lastAngle;

                float angleDifference = lastAngle - firstAngle;
                if (angleDifference < 0)
                {
                    angleDifference += 2 * Mathf.PI;
                }

                if (angleDifference > Mathf.PI)
                {
                    borderRef.reverseVertexOrder = true;
                }
                else
                {
                    borderRef.reverseVertexOrder = false;
                }
            }
            
            
            Recalculate();
        }

        public void RecalculateAppearance()
        {
            if (!mapTextOverride)
                cityTextTMP.text = gameObject.name;
            
            if (Application.isEditor)
                return;

            if (terrainType == TerrainType.Land)
            {
                meshRenderer.material.SetColor("_BaseColor", mapCountry.color);
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
