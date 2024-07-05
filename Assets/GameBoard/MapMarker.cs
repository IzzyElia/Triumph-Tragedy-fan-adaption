using System;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard
{
    public class MapMarker : MapObject
    {
        public static MapMarker Create(Map map, GameObject prefab) => Create<MapMarker>(prefab:prefab, map:map);
        public static T Create<T>(Map map, GameObject prefab) where T : MapMarker
        {
            if (prefab is null) throw new ArgumentException("Must provide a prefab");

            T mapMarker = Instantiate(prefab).GetComponent<T>();
            if (mapMarker is null) Debug.LogError($"No {typeof(T)} component attached to the provided prefab");
            mapMarker.RegisterTo(map);
            mapMarker.transform.SetParent(map.transform);
            mapMarker.RecalculateAppearance();
            return mapMarker;
        }
        
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] public bool Highlightable;
        private bool _highlight;

        public bool Highlight
        {
            get => _highlight;
            set
            {
                bool prev = _highlight;
                _highlight = value;
                if (prev != value) RecalculateAppearance();
            }
        }
        private bool _darken;
        public bool Darken
        {
            get => _darken;
            set
            {
                bool prev = _darken;
                _darken = value;
                if (prev != value) RecalculateAppearance();
            }
        }

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        protected virtual void RecalculateAppearance()
        {
            meshRenderer.material.SetInt("_Highlight", Highlight ? 1 : 0);
            meshRenderer.material.SetInt("_Darken", Darken ? 1 : 0);
        }

        public override void OnHoveredStatusChanged(bool isHoveredOver)
        {
            base.OnHoveredStatusChanged(isHoveredOver);
            RecalculateHighlightState();
        }

        protected virtual void RecalculateHighlightState()
        {
            Highlight = Highlightable && Map.HoveredMapObject == this;
            Darken = false;
        }
    }
}