using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GameBoard;

public class MapTileEditorList : EditorWindow
{
    private static Map _map;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        _map = GameObject.Find("Map")?.GetComponent<Map>();

    }

    [MenuItem("Window/Custom Object List")]
    public static void ShowWindow()
    {
        _map = GameObject.Find("Map")?.GetComponent<Map>();
        if (_map != null)
            GetWindow<MapTileEditorList>("Map Tiles");
    }

    private void OnGUI()
    {
        if (_map == null)
        {
            Close();
            return;
        }
        GUILayout.Label("Map Tiles", EditorStyles.boldLabel);
        if (GUILayout.Button("Toggle Background"))
        {
            SpriteRenderer spriteRenderer = _map.GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = !spriteRenderer.enabled;
        }
        GUILayout.Space(15);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        MapTile[] tiles = _map.countriesWrapper.GetComponentsInChildren<MapTile>();
        Array.Sort(tiles, (a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

        foreach (MapTile mapTile in tiles)
        {
            GUILayout.BeginHorizontal();

            Color color = mapTile.markComplete ? Color.green : Color.red;
            // Draw the color box
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(5, 20), color);

            // Display the object name with clickable label
            if (GUILayout.Button(mapTile.name, EditorStyles.label))
            {
                SelectObject(mapTile.gameObject);
                Selection.activeObject = mapTile.gameObject;
                SceneView.FrameLastActiveSceneView();
            }

            // Button 1
            if (GUILayout.Button("Sort", GUILayout.Width(75)))
            {
                mapTile.AutoReorderBorders();
            }
            
            if (GUILayout.Button("Flash", GUILayout.Width(75)))
            {
                mapTile.FlashMesh();
            }

            // Button 2
            if (GUILayout.Button("Unflash", GUILayout.Width(75)))
            {
                mapTile.UnflashMesh();
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void SelectObject(GameObject obj)
    {
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
    }
}