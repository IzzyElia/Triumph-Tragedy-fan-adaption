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
        }

        private void OnEnable()
        {
            _cadre = (MapCadre)target;
        }
    }
}
