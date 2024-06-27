using System;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(PipsManager))]
    public class PipManagerEditor : Editor
    {
        private PipsManager _pipsManager;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Rebuild"))
            {
                _pipsManager.Rebuild();
            }
        }

        private void OnEnable()
        {
            _pipsManager = (PipsManager)target;
        }
    }
}