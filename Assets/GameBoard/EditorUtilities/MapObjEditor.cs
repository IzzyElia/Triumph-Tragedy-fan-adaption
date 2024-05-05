using System;
using UnityEditor;
using UnityEngine;

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

            // Add a custom button to the inspector
            if (GUILayout.Button("Recalculate Board Values"))
            {
                _map.RecalculateMapObjectLists();
            }
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
                    Handles.color = Color.red;
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