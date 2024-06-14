using System;
using GameBoard.UI;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(UIWindow))]
    public class UIWindowEditor : Editor
    {
        private UIWindow _uiWindow;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            if (GUILayout.Button("Toggle Active"))
            {
                _uiWindow.SetActive(!_uiWindow.Active);
            }
        }

        private void OnEnable()
        {
            _uiWindow = (UIWindow)target;
        }
    }
}