using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(MapCadre))]
    public class MapCadreEditor : Editor
    {
        private MapCadre _cadre;
        private Map _map;
        private bool _mapSelected;

        public override void OnInspectorGUI()
        {
            // Draw the default inspector options
            DrawDefaultInspector();

            if (GUILayout.Button("MoveUnit"))
            {
                var popup = new MapCadreMovementPopup(_cadre);
                var rect = new Rect(Event.current.mousePosition, Vector2.zero);
                PopupWindow.Show(rect, popup);
            }

            if (GUILayout.Button("Rebuild Generated Assets"))
            {
                RebuildGeneratedAssets();
            }
        }

        /// <summary>
        /// Creates assets including the following and adds them to the resources folder, destroying and replacing existing assets if applicable
        ///     1. A mesh of a rectangular with dimensions 1, 1, 0.3. The cube has a uv map such that only the front 1x1 face is mapped 0-1. Ie I want my texture to be mapped to the front face
        ///     2. TBD
        /// </summary>
        private void RebuildGeneratedAssets()
        {
            Mesh blockMesh = CreateCadreBlockMesh();
            Mesh cylinderMesh = CreateCylinderMesh();
            AssetDatabase.CreateAsset(blockMesh, $"Assets/Resources/Meshes/CadreBlock.mesh");
            AssetDatabase.CreateAsset(cylinderMesh, $"Assets/Resources/Meshes/CadreCylinder.mesh");
            AssetDatabase.SaveAssets();
        }

        private Mesh CreateCadreBlockMesh()
        {
            Mesh mesh = new Mesh();

            // Define vertices
            Vector3[] vertices = new Vector3[]
            {
                // Front Face
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),

                // Back Face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),

                // Top Face
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),

                // Bottom Face
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),

                // Left Face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),

                // Right Face
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
            };

            // Triangles array
            int[] triangles = new int[]
            {
                // Front face
                0, 1, 2, 0, 2, 3,
                // Back face
                5, 4, 7, 5, 7, 6,
                // Top face
                8, 9, 10, 8, 10, 11,
                // Bottom face
                13, 12, 15, 13, 15, 14,
                // Left face
                16, 17, 18, 16, 18, 19,
                // Right face
                21, 20, 23, 21, 23, 22
            };


            // Define UVs for UV0 - standard mapping on all faces
            Vector2[] uv0 = new Vector2[24];
            for (int i = 0; i < 24; i++)
            {
                int mod = i % 4;
                uv0[i] = new Vector2(mod == 1 || mod == 2 ? 1 : 0, mod == 2 || mod == 3 ? 1 : 0);
            }

            // Define UVs for UV1 - front face only
            Vector2[] uv1 = new Vector2[24];
            for (int i = 0; i < 24; i++)
            {
                uv1[i] = Vector2.zero; // Default to zero for all other faces
            }

            uv1[4] = new Vector2(0, 0);
            uv1[5] = new Vector2(1, 0);
            uv1[6] = new Vector2(1, 1);
            uv1[7] = new Vector2(0, 1);

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv0;
            mesh.uv2 = uv1;

            mesh.RecalculateNormals(); // For proper lighting and shading

            return mesh;
        }

Mesh CreateCylinderMesh()
{
    const int segments = 24;
    const float height = 2f;
    const float radius = 1f;
    const float innerRadius = 0.95f * radius;  // Slightly smaller than the main radius
    Mesh mesh = new Mesh();

    // Adjust vertex count to include an additional inner ring
    int verticesCount = segments * 3 + 2; // top + bottom center points + inner top ring
    Vector3[] vertices = new Vector3[verticesCount];
    Vector2[] uv0 = new Vector2[verticesCount];
    Vector2[] uv1 = new Vector2[verticesCount];

    // Top and bottom center
    vertices[verticesCount - 2] = new Vector3(0, 0, -height / 2);  // Top center now at negative z
    vertices[verticesCount - 1] = new Vector3(0, 0, height / 2);   // Bottom center now at positive z
    uv0[verticesCount - 2] = new Vector2(0.5f, 0.5f);  // Center of UV map for top
    uv0[verticesCount - 1] = new Vector2(0.5f, 0.5f);  // Center of UV map for bottom
    uv1[verticesCount - 2] = new Vector2(0.5f, 0.5f);  // Only top has meaningful uv1
    uv1[verticesCount - 1] = Vector2.zero;

    float angleStep = 360.0f / segments;
    for (int i = 0; i < segments; i++)
    {
        float angle = Mathf.Deg2Rad * angleStep * i;
        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;
        float innerX = Mathf.Cos(angle) * innerRadius;
        float innerY = Mathf.Sin(angle) * innerRadius;

        // Outer ring vertices
        vertices[i] = new Vector3(x, y, -height / 2); // Lower cap at negative z
        vertices[segments + i] = new Vector3(x, y, height / 2); // Upper cap at positive z
        // Inner ring vertices (only for the top)
        vertices[2 * segments + i] = new Vector3(innerX, innerY, height / 2); // Inner top ring

        // uv0 mapping remains the same for the main segments
        uv0[i] = new Vector2((i * 4.0f / (float)segments) % 1.0f, 1);
        uv0[segments + i] = new Vector2((i * 4.0f / (float)segments) % 1.0f, 0);
        // Additional uv0 mapping for the inner ring, same as outer top
        uv0[2 * segments + i] = new Vector2((i * 4.0f / (float)segments) % 1.0f, 0);

        // uv1 mapping
        uv1[i] = new Vector2(x / radius / 2 + 0.5f, y / radius / 2 + 0.5f); // Map to corners for outer ring
        uv1[segments + i] = Vector2.zero; // Bottom ring does not need uv1 mapping
        uv1[2 * segments + i] = new Vector2(innerX / innerRadius / 2 + 0.5f, innerY / innerRadius / 2 + 0.5f); // Map like the original top mapping
    }

    // Update triangle count to include additional top triangles
    int[] triangles = new int[segments * 12 + segments * 3]; // Additional triangles for the inner ring
    for (int i = 0, t = 0; i < segments; i++)
    {
        int next = (i + 1) % segments;

        // Side triangles
        triangles[t++] = i;
        triangles[t++] = segments + next;
        triangles[t++] = segments + i;

        triangles[t++] = i;
        triangles[t++] = next;
        triangles[t++] = segments + next;

        // Top triangles (outer ring to inner ring)
        triangles[t++] = 2 * segments + i;
        triangles[t++] = 2 * segments + next;
        triangles[t++] = verticesCount - 2;

        // Bottom triangles (no change)
        triangles[t++] = verticesCount - 1;
        triangles[t++] = segments + next;
        triangles[t++] = segments + i;
    }

    mesh.vertices = vertices;
    mesh.uv = uv0;
    mesh.uv2 = uv1;
    mesh.triangles = triangles;
    mesh.RecalculateNormals();

    return mesh;
}



        private void OnEnable()
        {
            _cadre = (MapCadre)target;
        }
    }
}