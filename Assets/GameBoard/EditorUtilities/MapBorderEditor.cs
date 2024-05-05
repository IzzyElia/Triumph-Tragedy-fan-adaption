using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(MapBorder))]
    public class MapBorderEditor : Editor
    {
        private MapBorder _border;
        private GUIStyle style = new GUIStyle();
        
        public override void OnInspectorGUI()
        {
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