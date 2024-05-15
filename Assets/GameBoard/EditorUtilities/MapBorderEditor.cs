using System;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(MapBorder))]
    public class MapBorderEditor : Editor
    {
        private MapBorder _border;
        private GUIStyle style = new GUIStyle();

        void CheckAddPoint()
        {
            Event editorEvent = Event.current;

            // Check if the event is a key press and if it matches the desired key
            if (editorEvent.type == EventType.KeyDown && editorEvent.keyCode == KeyCode.E && editorEvent.shift)
            {
                Debug.Log("Creating new point");
                Ray worldRay = HandleUtility.GUIPointToWorldRay(editorEvent.mousePosition);
                float distance = (0 - worldRay.origin.z) / worldRay.direction.z;
                Vector3 mousePositionInScene = worldRay.GetPoint(distance);
                Vector3[] prevBorderPoints = new Vector3[_border.points.Length];
                _border.points.CopyTo(prevBorderPoints, 0);
                _border.points = new Vector3[_border.points.Length + 1];
                prevBorderPoints.CopyTo(_border.points, 0);
                _border.points[^1] = new Vector3(mousePositionInScene.x, mousePositionInScene.y, 0);
                editorEvent.Use();
            }
        }
        
        public override void OnInspectorGUI()
        {
            CheckAddPoint();
            // Draw the default inspector options
            DrawDefaultInspector();

            // Add a custom button to the inspector
            if (GUILayout.Button("Recalculate Board Values"))
            {
                _border.Map.RecalculateMapObjectLists();
            }
        }

        private void OnSceneGUI()
        {
            CheckAddPoint();
            Vector3[] vertices = _border.points;
            Handles.DrawPolyLine(vertices);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 oldPosition = vertices[i];
                Vector3 newPosition = Handles.FreeMoveHandle(oldPosition, HandleUtility.GetHandleSize(oldPosition) * 0.2f, Vector3.one * 0.02f, Handles.CylinderHandleCap);
                Handles.Label(oldPosition, i.ToString(), style);
                if (newPosition != oldPosition)
                {
                    _border.MoveVertex(i, new Vector3(newPosition.x, newPosition.y, 0));
                    Debug.Log("Moving vertex by handle...");
                }
            }
        }

        private void OnEnable()
        {
            _border = (MapBorder)target;
            style.normal.textColor = Color.black;
            style.alignment = TextAnchor.MiddleCenter;
        }
    }
}