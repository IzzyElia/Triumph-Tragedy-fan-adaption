using System;
using Codice.Utils;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.WSA;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(GameBoard.Map))]
    public class MapObjEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Map _map = (Map)target;
            // Draw the default inspector options
            DrawDefaultInspector();
            
            if (GUILayout.Button("Recalculate Board Values"))
            {
                _map.FullyRecalculate();
            }
            
            if (GUILayout.Button("Auto select national capitals"))
            {
                foreach (var mapCountry in _map.MapCountriesByID)
                {
                    (MapTile tile, int largestCitySize) largestCity = (null, -1);
                    foreach (var mapTile in _map.MapTilesByID)
                    {
                        if (mapTile.mapCountry != mapCountry) continue;
                        if (mapTile.citySize > largestCity.largestCitySize) largestCity = (mapTile, mapTile.citySize);
                    }
                    if (largestCity.tile is null) Debug.LogWarning($"Unable to find capital for {mapCountry.name} - it doesn't control any tiles");
                    else
                    {
                        Debug.Log($"Setting the capital of {mapCountry.name} to {largestCity.tile.name}");
                        mapCountry.Capital = largestCity.tile;
                    }
                    EditorUtility.SetDirty(mapCountry);
                }
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

            if (GUILayout.Button("Regenerate Background Mesh"))
            {
                MeshFilter meshFilter = _map.mapBackground.GetComponent<MeshFilter>();
                meshFilter.mesh = RegenerateMapBackgroundMesh(_map.mapBackgroundSubdivisions);
            }
            
            if (GUILayout.Button("Save Map"))
            {
                _map.SaveToFile();
            }
        }

        public static Mesh RegenerateMapBackgroundMesh(int subdivisions)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Subdivided Quad";

            int vertCount = (subdivisions + 1) * (subdivisions + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uv = new Vector2[vertCount];
            int[] triangles = new int[subdivisions * subdivisions * 6];

            float stepSize = 1.0f / subdivisions;
            int vertIndex = 0;
            int triIndex = 0;

            for (int i = 0; i <= subdivisions; i++)
            {
                for (int j = 0; j <= subdivisions; j++)
                {
                    vertices[vertIndex] = new Vector3(j * stepSize - 0.5f, i * stepSize - 0.5f, 0);
                    uv[vertIndex] = new Vector2(j * stepSize, i * stepSize);

                    if (i < subdivisions && j < subdivisions)
                    {
                        int topLeft = vertIndex;
                        int bottomLeft = vertIndex + subdivisions + 1;

                        triangles[triIndex++] = topLeft;
                        triangles[triIndex++] = bottomLeft;
                        triangles[triIndex++] = bottomLeft + 1;

                        triangles[triIndex++] = topLeft;
                        triangles[triIndex++] = bottomLeft + 1;
                        triangles[triIndex++] = topLeft + 1;
                    }

                    vertIndex++;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }



        public static void DrawMapCompletion(Map map)
        {
            if (map is null)
            {
                Debug.LogWarning("Should probably figure out why this's happening");
                return;
            }
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
            Map _map = (Map)target;
            DrawMapCompletion(_map);
        }
    }
}