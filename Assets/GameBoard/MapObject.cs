using System;
using System.Collections.Generic;
using System.IO;
using GameSharedInterfaces;
using UnityEditor;
using UnityEngine;

namespace GameBoard
{
    [ExecuteAlways]
    public abstract class MapObject : MonoBehaviour
    {
        public int ID = -1;
        public bool IsDestroyed { get; private set; } = false;
        [SerializeField] private Map _map;
        public Map Map
        {
            get
            {
                return _map;
            }
            set
            {
                _map = value;
            }
        }

        public void RegisterTo(Map map)
        {
            if (map is null) throw new ArgumentException("Map cannot be null");
            this.Map = map;
            Map.RegisterObject(this);
            registeredForAnimation = false;
        }

        public void Deregister()
        {
            if (_map is null)
            {
                Debug.LogError($"{this.GetType().Name} not registered to a map");
                return;
            }
            _map.DeregisterObject(this);
            _map = null;
            registeredForAnimation = false;
        }

        private bool _supressDestroyWarning = false;
        public void DestroyMapObject()
        {
            foreach (var mapObject in GetComponentsInChildren<MapObject>(includeInactive:true))
            {
                if (mapObject != this) mapObject.DestroyMapObject();
            }
            _supressDestroyWarning = true;
            Map.UIController.ScrubQueue.Add(this);
            Deregister();
            IsDestroyed = true;
            Destroy(this.gameObject);
            Destroy(this);
        }
        private void OnDestroy()
        {
            if (!_supressDestroyWarning && !SharedData.SupressDestroyWarningGlobally && !Application.isEditor) Debug.LogError($"{GetType().Name} destroyed incorrectly!. Map objects should be destroyed by calling MapObject.Destroy, not MonoBehavior.Destroy()");
        }

        private bool registeredForAnimation = false;
        /// <summary>
        /// Sets whether the object is in the map renderer's animation list.
        /// While enabled, Animate() will be called every update
        /// </summary>
        /// <param name="needsAnimation"></param>
        public void SetAnimated(bool needsAnimation)
        {
            if (needsAnimation && !registeredForAnimation)
            {
                _map.AddObjectToAnimationList(this);
                registeredForAnimation = true;
            }
            else if (!needsAnimation && registeredForAnimation)
            {
                _map.RemoveObjectFromAnimationList(this);
                registeredForAnimation = false;
            }
        }

        /// <summary>
        /// Run every update while SetAnimated() is true
        /// </summary>
        public virtual void Animate() {}
        /// <summary>
        /// Run when animation ends or the map renderer changes.
        /// The purpose of this function is to clean up any values that
        /// </summary>
        public virtual void ConcludeAnimation() {}

        /*
        private void OnDisable()
        {
            if (_map != null)
                Map.FullyRecalculate();
        }
        */
        protected virtual void OnTileChanged(MapTile tile)
        {
            if (tile is not null) transform.SetParent(tile.transform);
        }

        public virtual void OnHoveredStatusChanged(bool isHoveredOver)
        {
            
        }

        public virtual void OnSelectionStatusChanged(SelectionStatus selectionStatus)
        {
            
        }

        public virtual bool IsSelectable => false;
    }
}