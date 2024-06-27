using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameSharedInterfaces;
using TMPro;
using TriangleNet.Geometry;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace GameBoard
{
    public enum TerrainType
    {
        Land,
        Sea,
        Ocean,
        Strait,
        NotInPlay
    }

    public enum TileHighlightState
    {
        NotHighlighted,
        HoverHighlighted,
        MovementOptionHighlighted,
    }
    [ExecuteAlways]
    public class MapTile : MapObject
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshCollider meshCollider;
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
        public int startingCadres;
        public int resources;
        public int colonialResources;
        public int citySize;
        public TerrainType terrainType;
        public bool IsCoastal;
        [SerializeField] public bool markFunctional = false;
        [SerializeField] public bool markComplete = false;
        [SerializeField] private bool mapTextOverride;
        [SerializeField] [HideInInspector] private Vector3[] lastCalculatedVertices = new Vector3[0];
        [SerializeField] [HideInInspector] private Vector3 lastCalculatedObjectPosition = Vector3.negativeInfinity;
        [SerializeField] public Vector3 holeMarker;
        public bool containsHole { get; private set; }
        string MeshSavePath => $"Assets/Resources/Meshes/Map/Tiles/{name}.mesh";
        string MeshLoadPath => $"Meshes/Map/Tiles/{name}";
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static Material seaMaterial;
        private static Material landMaterial;
        private static Material straitMaterial;
        private static readonly int OccupierColor = Shader.PropertyToID("_OccupierColor");


        [NonSerialized] public MapCountry Occupier;
        [NonSerialized] public TileHighlightState HighlightState;

#if !UNITY_EDITOR
        static MapTile()
        {
            seaMaterial = Resources.Load<Material>("Shaders/Sea");
            landMaterial = Resources.Load<Material>("Shaders/Land");
            straitMaterial = Resources.Load<Material>("Shaders/Strait");
        }
#endif

        
        private void Start()
        {
            SetupMaterialForEditor();
        }

        public bool IsPointInside(Vector2 point)
        {
            point -= (Vector2)transform.position;
            Vector3[] vertices = Mesh.vertices;
            bool result = false;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                if ((vertices[i].y > point.y) != (vertices[j].y > point.y) &&
                    (point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    result = !result;
                }
            }
            return result;
        }
        public void Recalculate(bool forceRecalculateMesh = false)
        {
#if UNITY_EDITOR
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            gameObject.layer = LayerMask.NameToLayer("Tiles");
            
            mapCountry = transform.parent.GetComponent<MapCountry>();
            if (forceRecalculateMesh)
            {
                FlashMesh();
            }
            else if (markComplete)
            {
                SetMeshToFlashed();
            }
            else
            {
                meshFilter.sharedMesh = RecalculateMesh();
            }
            SetupMaterialForEditor();
            //if (!markComplete)
            //    AutoReorderBorders();
#endif
        }

        public void RecalculateMaterialDuringRuntime()
        {
            Color baseColor;
            if (terrainType == TerrainType.Sea)
            {
                meshRenderer.material.SetColor(BaseColor, new Color(0.15f, 0.6f, 1f));
            }
            else if (terrainType == TerrainType.Ocean)
            {
                meshRenderer.material.SetColor(BaseColor, new Color(0.15f, 0.3f, 1f));

            }
            else if (terrainType == TerrainType.NotInPlay)
            {
                baseColor = Color.white * 0.2f ;
            }
            else if (terrainType == TerrainType.Land || terrainType == TerrainType.Strait)
            {
                if (Occupier is not null)
                {
                    meshRenderer.material.SetColor(OccupierColor, Occupier.CalculatedColor);
                }
                else
                {
                    meshRenderer.material.SetColor(OccupierColor, Color.white);
                }

                if (mapCountry.colonialOverlord is not null)
                {
                    meshRenderer.material.SetColor(BaseColor, (mapCountry.colonialOverlord.CalculatedColor*2 + mapCountry.CalculatedColor) / 3f);
                }
                else
                {
                    meshRenderer.material.SetColor(BaseColor, mapCountry.CalculatedColor);
                }
            }
            else
            {
                Debug.LogError("Unsupported terrain type");
                return;
            }
            
            RecalculateHighlighting();
        }

        public void RecalculateHighlighting()
        {
            if (Map.UIController.HoveredOverTile == this)
            {
                HighlightState = TileHighlightState.HoverHighlighted;
            }
            else if (Map.UIController.MovementHighlights.Contains(this.ID))
            {
                HighlightState = TileHighlightState.MovementOptionHighlighted;
            }
            else HighlightState = TileHighlightState.NotHighlighted;

            foreach (var borderReference in connectedBorders)
            {
                borderReference.border.RecalculateMaterialRuntimeValues();
            }
        }
        private void SetupMaterialForEditor()
        {
            Random.InitState(name.GetHashCode());
            //Color randColor = new Color(Random.value, Random.value, Random.value);
            Material material;
            Color baseColor;

            if (terrainType == TerrainType.Sea)
            {
                baseColor = new Color(0.15f, 0.6f, 1f);
                material = Resources.Load<Material>("Shaders/Sea");
            }
            else if (terrainType == TerrainType.Ocean)
            {
                baseColor = new Color(0.15f, 0.3f, 1f);
                material = Resources.Load<Material>("Shaders/Sea");

            }
            else if (terrainType == TerrainType.NotInPlay)
            {
                baseColor = Color.white * 0.2f ;
                material = Resources.Load<Material>("Shaders/Land");

            }
            else if (terrainType == TerrainType.Strait)
            {
                if (!(mapCountry is null))
                    baseColor = mapCountry.color;
                else
                    baseColor = Color.black;
                material = Resources.Load<Material>("Shaders/Strait");
            }
            else if (terrainType == TerrainType.Land)
            {
                if (!(mapCountry is null))
                    baseColor = mapCountry.color;
                else
                    baseColor = Color.black;
                material = Resources.Load<Material>("Shaders/Land");
            }
            else
            {
                material = null;
                Debug.LogError("Unsupported terrain type");
                return;
            }

            meshRenderer.sharedMaterial = new Material(material);
            meshRenderer.sharedMaterial.SetColor(BaseColor, baseColor);
        }
        /// <returns> (Min, Max) in world space </returns>
        public (Vector2, Vector2) GetMeshBoundingBox()
        {
            Vector3[] vertices = Mesh.vertices;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 position = transform.position;
                Vector2 vertex = new Vector2(vertices[i].x, vertices[i].y) + position;
                min = Vector2.Min(min, vertex);
                max = Vector2.Max(max, vertex);
            }
            return (min, max);
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
                
                // UVS mapped directly to vertex coordinates. Should I normalize them?
                Vector2[] uvs = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
                }


                try
                {
                    Mesh mesh = new Mesh();
                    mesh.vertices = vertices;
                    mesh.triangles = triangles;
                    mesh.uv = uvs;
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
            Material loadedShader;
            if (terrainType == TerrainType.Land || terrainType == TerrainType.NotInPlay)
                loadedShader = Resources.Load<Material>("Shaders/Land");
            else if (terrainType == TerrainType.Sea || terrainType == TerrainType.Ocean)
                loadedShader = Resources.Load<Material>("Shaders/Sea");
            // TODO Straits should have their own shader
            else if (terrainType == TerrainType.Strait)
                loadedShader = Resources.Load<Material>("Shaders/Land");
            else
            {
                loadedShader = Resources.Load<Material>("Shaders/Land");
                Debug.LogWarning($"No shader assigned to terrain type {terrainType.ToString()}");
            }
            if (loadedMesh is not null)
            {
                SerializedObject serializedMeshFilter = new SerializedObject(meshFilter);
                SerializedObject serializedMeshRenderer = new SerializedObject(meshRenderer);
                SerializedObject serializedMeshCollider = new SerializedObject(meshCollider);
                SerializedProperty meshProperty = serializedMeshFilter.FindProperty("m_Mesh");
                SerializedProperty shaderProperty = serializedMeshRenderer.FindProperty("m_Materials.Array.data[0]");
                SerializedProperty colliderProperty = serializedMeshCollider.FindProperty("m_Mesh");
                meshProperty.objectReferenceValue = loadedMesh;
                shaderProperty.objectReferenceValue = loadedShader;
                colliderProperty.objectReferenceValue = loadedMesh;
                serializedMeshFilter.ApplyModifiedProperties();
                serializedMeshRenderer.ApplyModifiedProperties();
                serializedMeshCollider.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(meshFilter);
                EditorUtility.SetDirty(meshRenderer);
                EditorUtility.SetDirty(meshCollider);
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
            SerializedObject serializedObject = new SerializedObject(meshFilter);
            SerializedProperty meshProperty = serializedObject.FindProperty("m_Mesh");
            meshProperty.objectReferenceValue = null;
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

        public override void OnHoveredStatusChanged(bool isHoveredOver)
        {
            base.OnHoveredStatusChanged(isHoveredOver);
            RecalculateMaterialDuringRuntime();
            RecalculateHighlighting();
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
    }
}
