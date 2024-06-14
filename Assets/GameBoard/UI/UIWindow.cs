using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameBoard.UI
{
    
    public abstract class UIWindow : UIComponent
    {
        [SerializeField] protected RectTransform rectTransform;
        [FormerlySerializedAs("AppearanceDirection")] [SerializeField] protected Direction appearanceDirection;
        
        public bool Active { get; private set; }
        public void SetActive(bool active)
        {
            if (active && !Active)
            {
                Active = true;
                gameObject.SetActive(true);
                OnActive();
            }
            else if (Active)
            {
                Active = false;
                OnHidden();
            }
        }

        public abstract bool WantsToBeActive { get; }
        protected abstract void OnActive();
        protected abstract void OnHidden();


        private void Awake()
        {
            if (rectTransform is null) Debug.LogError($"No rect transform on {this.globalName}");
        }

        public enum Direction
        {
            up,
            down,
            left,
            right
        }
    }
}