using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(MapTile))]
    public class MapMeshEditor : Editor
    {
        private MapTile _mapTile;
        private Vector3 prevPosition;
        private GUIStyle style = new GUIStyle();
        private Map _map;
        private bool _mapSelected;

        public override void OnInspectorGUI()
        {
            // Draw the default inspector options
            DrawDefaultInspector();

            if (GUILayout.Button("Add Border"))
            {
                var popup = new MapTileBorderCreationPopup(_mapTile);
                var rect = new Rect(Event.current.mousePosition, Vector2.zero);
                PopupWindow.Show(rect, popup);
            }
            if (GUILayout.Button("Flash"))
            {
                _mapTile.FlashMesh();
            }
            if (GUILayout.Button("Unflash"))
            {
                _mapTile.UnflashMesh();
            }
            if (GUILayout.Button("Sort Borders"))
            {
                _mapTile.AutoReorderBorders();
            }
            // Add a custom button to the inspector
            if (GUILayout.Button("Recalculate Board Values"))
            {
                _mapTile.Map.FullyRecalculate();
            }
        }
        private void OnSceneGUI()
        {
            MapObjEditor.DrawMapCompletion(_map);

            Handles.color = Color.white;
            
            if (_mapTile.transform.position != prevPosition)
            {
                prevPosition = _mapTile.transform.position;
                //_mapTile.RecalculateMesh();
            }

            (Vector3[] vertices, Vector3[] holeVertices) = _mapTile.GetVertices();
            for (int i = 0; i < vertices.Length; i++)
            {
                float p = ((float)i) / vertices.Length;
                Handles.color = new Color(p, 1-p, 1);
                Vector3 vertex = vertices[i]; ;
                float handleSize = HandleUtility.GetHandleSize(vertex) * 0.1f;
                Handles.DrawSolidDisc(vertex, Vector3.back, handleSize);
                Handles.Label(vertex, i.ToString(), style);
            }
            for (int i = 0; i < holeVertices.Length; i++)
            {
                Vector3 vertex = holeVertices[i]; ;
                float handleSize = HandleUtility.GetHandleSize(vertex) * 0.1f;
                Handles.DrawSolidDisc(vertex, Vector3.back, handleSize);
                Handles.Label(vertex, $"h{i.ToString()}", style);
            }

            if (_mapTile.containsHole)
            {
                Handles.color = new Color(1, 0.5f, 0.2f);
                Vector3 oldPosition = _mapTile.holeMarker;
                Vector3 newPosition = Handles.FreeMoveHandle(oldPosition, HandleUtility.GetHandleSize(oldPosition) * 0.2f, Vector3.one * 0.02f, Handles.CylinderHandleCap);
                if (newPosition != oldPosition)
                {
                    _mapTile.holeMarker = new Vector3(newPosition.x, newPosition.y, 0);
                    _mapTile.Recalculate();
                }
            }
            Handles.DrawPolyLine(vertices.ToArray());
        }

        private void OnEnable()
        {
            _mapTile = (MapTile)target;
            style.normal.textColor = Color.black;
            style.alignment = TextAnchor.MiddleCenter;
            Map[] maps = _mapTile.GetComponentsInParent<Map>();
            if (maps.Length > 0)
                _map = maps[0];
            _mapSelected = this._map != null;
        }
    }
}
