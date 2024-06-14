using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using UnityEditor;

namespace GameBoard
{
    [ExecuteAlways]
    public class MapBorder : MapObject
    {
        [Serializable]
        struct VertexShare
        {
            [SerializeField] public bool enabled;
            [SerializeField] public MapBorder target;
            [SerializeField] public bool targetFirstVertex;
        }
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] public Vector3[] points = new Vector3[0];
        [SerializeField] [HideInInspector] private int prevNumPoints;
        public List<MapTile> connectedMapTiles = new List<MapTile>();
        public BorderType borderType;
        [SerializeField] private VertexShare shareFirstVertex;
        [SerializeField] private VertexShare shareLastVertex;
        [SerializeField] private bool markComplete;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        string FilePath => $"{Application.dataPath}/map/{name}.txt";
        string MeshSavePath => $"Assets/Resources/Meshes/Map/Borders/{name}.mesh";
        string MeshLoadPath => $"Meshes/Map/Borders/{name}";

        public void Recalculate()
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (shareFirstVertex.enabled && points.Length >= 2 && shareFirstVertex.target.points.Length >= 2)
            {
                if (shareFirstVertex.targetFirstVertex)
                    points[0] = shareFirstVertex.target.points[0];
                else
                    points[0] = shareFirstVertex.target.points[^1];
            }
            if (shareLastVertex.enabled && points.Length >= 2 && shareLastVertex.target.points.Length >= 2)
            {
                if (shareLastVertex.targetFirstVertex)
                    points[^1] = shareLastVertex.target.points[0];
                else
                    points[^1] = shareLastVertex.target.points[^1];
            }

            if (points.Length > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in points)
                {
                    center += point;
                }
                center /= points.Length;
                transform.localPosition = center;
            }

            if (markComplete)
                SetMeshToFlashed();
            else
                FlashMesh();
            SetupMaterial();
            EditorUtility.SetDirty(this);
            //Save();
        }

        private void Start()
        {
            SetupMaterial();
        }

        private void SetupMaterial()
        {
            Random.InitState(name.GetHashCode());
            Material material;
            Color baseColor;

            if (borderType == BorderType.Sea || 
                borderType == BorderType.HornOfAfrica || 
                borderType == BorderType.Strait)
            {
                baseColor = new Color(0.05f, 0.2f, 0.5f);
                material = Resources.Load<Material>("Shaders/Border");
            }
            else if (borderType == BorderType.Coast || borderType == BorderType.Plains)
            {
                baseColor = new Color(0, 0, 0);
                material = Resources.Load<Material>("Shaders/Border");

            }
            else if (borderType == BorderType.Forest)
            {
                baseColor = new Color(0f, 0.8f, 0.3f);
                material = Resources.Load<Material>("Shaders/Border");

            }
            else if (borderType == BorderType.Mountain)
            {
                baseColor = new Color(0.9f, 0.9f, 0.9f);
                material = Resources.Load<Material>("Shaders/Border");
            }
            else if (borderType == BorderType.River)
            {
                baseColor = new Color(0f, 0.4f, 0.8f);
                material = Resources.Load<Material>("Shaders/River");
            }
            else if (borderType == BorderType.Impassable)
            {
                baseColor = new Color(1f, 0.6f, 0.1f);
                material = Resources.Load<Material>("Shaders/Border");
            }
            else if (borderType == BorderType.Unspecified)
            {
                baseColor = new Color(1, 0, 1);
                material = Resources.Load<Material>("Shaders/Border");
            }
            else
            {
                material = null;
                Debug.LogError($"Unsupported border type {borderType.ToString()}");
                return;
            }

            meshRenderer.sharedMaterial = new Material(material);
            meshRenderer.sharedMaterial.SetColor(BaseColor, baseColor);
        }
        
        

        public Mesh GenerateBorderMesh()
        {
            // Ensure there are at least two points to form a mesh
            if (points.Length < 2)
                return null;

            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[points.Length * 2];
            int[] triangles = new int[(points.Length - 1) * 6];
            Vector2[] uvs = new Vector2[vertices.Length];

            float totalLength = 0;
            float[] segmentLengths = new float[points.Length - 1];

            // Calculate segment lengths and total length
            for (int i = 0; i < points.Length - 1; i++)
            {
                float segmentLength = Vector3.Distance(points[i], points[i + 1]);
                segmentLengths[i] = segmentLength;
                totalLength += segmentLength;
            }

            float accumulatedLength = 0;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 current = points[i];
                Vector3 direction;

                // Determine direction vector
                if (i == 0)
                {
                    direction = (points[i + 1] - current).normalized;
                }
                else if (i == points.Length - 1)
                {
                    direction = (current - points[i - 1]).normalized;
                }
                else
                {
                    direction = ((points[i + 1] - current) + (current - points[i - 1])).normalized;
                }

                Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * Map.borderMeshWidth / 2;

                Vector3 localPosition = transform.localPosition;
                vertices[i * 2] = current + perpendicular - localPosition;
                vertices[i * 2 + 1] = current - perpendicular - localPosition;

                // Calculate v coordinate based on accumulated length
                float v = accumulatedLength
                //    / totalLength
                    ;

                uvs[i * 2] = new Vector2(0, v);
                uvs[i * 2 + 1] = new Vector2(1, v);

                // Accumulate length for next segment
                if (i < points.Length - 1)
                {
                    accumulatedLength += segmentLengths[i];
                }
            }

            // Create triangles
            for (int i = 0; i < points.Length - 1; i++)
            {
                int baseIndex = i * 2;
                int nextBaseIndex = (i + 1) * 2;

                triangles[i * 6] = baseIndex;
                triangles[i * 6 + 1] = nextBaseIndex;
                triangles[i * 6 + 2] = baseIndex + 1;

                triangles[i * 6 + 3] = baseIndex + 1;
                triangles[i * 6 + 4] = nextBaseIndex;
                triangles[i * 6 + 5] = nextBaseIndex + 1;
            }

            // Assign vertices, triangles, and UVs to the mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;

            // Recalculate mesh properties
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }
        
        private void OnValidate()
        {
            /*
            if (points.Length != prevNumPoints)
            {
                for (int i = prevNumPoints; i < points.Length; i++)
                {
                    if (i == 0)
                        points[i] = transform.position;
                    else
                        points[i] = points[i - 1] + new Vector3(
                            0.1f,
                            0);
                }

                prevNumPoints = points.Length;
            }
            */
            //Recalculate();
        }
        
        public void FlashMesh()
        {
            if (markComplete) UnflashMesh();
            Mesh mesh = GenerateBorderMesh();
            if (mesh == null)
            {
                mesh = new Mesh(); // empty mesh
            }
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
                SerializedObject serializedMeshFilter = new SerializedObject(meshFilter);
                SerializedProperty meshProperty = serializedMeshFilter.FindProperty("m_Mesh");
                meshProperty.objectReferenceValue = loadedMesh;
                serializedMeshFilter.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(meshFilter);
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
        

        public void MoveVertex(int vertex, Vector3 position)
        {
            points[vertex] = position;
            foreach (var mapTile in connectedMapTiles)
            {
                //mapTile.RecalculateMesh();
            }
            EditorUtility.SetDirty(this);
        }

        public void RecalculateAppearance()
        {
            // TODO Implement this
        }
    }

    public enum BorderType
    {
        Plains,
        Forest,
        River,
        Mountain,
        Coast,
        Sea,
        Strait,
        HornOfAfrica,
        Impassable,
        Unspecified,
    }
}
