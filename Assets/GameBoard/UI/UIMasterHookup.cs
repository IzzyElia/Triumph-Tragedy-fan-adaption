using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace GameBoard.UI
{
    /*
    public class UIMasterHookup : MonoBehaviour
    {
        public UIWindow UnitPlacementWindow;
        public TextMeshProUGUI DebugTextbox;

        private void Awake()
        {
            foreach (var field in typeof(UIMasterHookup).GetFields())
            {
                if (field.IsPublic && !((field.Attributes & FieldAttributes.NotSerialized) != 0))
                {
                    if (field.GetValue(this) == null) Debug.LogWarning($"{field.Name} not set");
                }
            }
        }
    }
    */
}