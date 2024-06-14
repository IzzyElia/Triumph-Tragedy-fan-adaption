using System;
using Codice.Utils;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(GameBoard.Map))]
    public class MapObjEditor : Editor
    {
        private Map _map;
        public override void OnInspectorGUI()
        {
            // Draw the default inspector options
            DrawDefaultInspector();
            
            if (GUILayout.Button("Recalculate Board Values"))
            {
                _map.FullyRecalculate();
            }
            
            if (GUILayout.Button("Toggle Text"))
            {
                TextMeshPro[] texts = _map.countriesWrapper.GetComponentsInChildren<TextMeshPro>(includeInactive:true);
                bool toggleOn = texts.Length > 0 && !texts[0].gameObject.activeSelf;
                foreach (TextMeshPro text in texts)
                {
                    text.gameObject.SetActive(toggleOn);
                }
            }
            
            if (GUILayout.Button("Save Map"))
            {
                _map.SaveToFile();
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
            Mesh mesh = CreateCadreMesh();
            AssetDatabase.CreateAsset(mesh, $"Assets/Resources/Meshes/CadreBlock.mesh");
            AssetDatabase.SaveAssets();
        }

        private Mesh CreateCadreMesh()
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

        public static void DrawMapCompletion(Map map)
        {
            MapTile[] mapTiles = map.countriesWrapper.GetComponentsInChildren<MapTile>();
            MapBorder[] mapBorders = map.mapBorderWrapper.GetComponentsInChildren<MapBorder>();
            foreach (MapTile mapTile in mapTiles)
            {
                if (mapTile.markComplete)
                    Handles.color = Color.green;
                else if (mapTile.markFunctional)
                    Handles.color = Color.yellow;
                else
                    Handles.color = new Color(1, 0.6f, 0.1f);
                Handles.DrawSolidDisc(mapTile.transform.position, Vector3.back, 0.05f);
                Handles.Label(mapTile.transform.position, mapTile.gameObject.name);
            }

            foreach (MapBorder border in mapBorders)
            {
                if (border.connectedMapTiles.Count == 2)
                {
                    Handles.color = new Color(0, 0f, 0);
                    Handles.DrawLine(
                        border.connectedMapTiles[0].transform.position, 
                        border.connectedMapTiles[1].transform.position
                    );
                }
                else
                {
                    Handles.color = Color.red;
                    Handles.DrawSolidDisc(border.transform.position, Vector3.back, 0.1f);

                }
            }
        }
        
        private void OnSceneGUI()
        {
            DrawMapCompletion(_map);
        }

        private void OnEnable()
        {
            _map = (Map)target;
        }
    }
}