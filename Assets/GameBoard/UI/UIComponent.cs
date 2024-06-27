using System;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameBoard.UI
{
    public abstract class UIComponent : MonoBehaviour
    {
        public abstract void OnGamestateChanged();
        public abstract void OnResyncEnded();
        public abstract void UIUpdate();
        public virtual void OnRegistered(){}
        protected Map MapRenderer => UIController.MapRenderer;
        protected ITTGameState GameState => UIController.GameState;
        protected MapFaction PlayerFaction => UIController.PlayerMapFaction;

        protected MapCountry PlayerCountry
        {
            get
            {
                try
                {
                    return UIController.PlayerMapFaction.leader;
                }
                catch (NullReferenceException e)
                {
                    throw e;
                }
            }
        } 
        protected int iPlayer => UIController.iPlayer;


        
        
        
        [FormerlySerializedAs("name")] public string globalName;
        [NonSerialized] public UIController UIController;





        // Special destruction logic
        bool _supressDestroyWarning = false;
        public void DestroyUIComponent()
        {
            Debug.Log($"Destroying {GetType().Name}");
            if (UIController is null) Debug.LogError($"{GetType().Name} was not ever registered to a UI Controller");
            foreach (var uiComponent in GetComponentsInChildren<UIComponent>())
            {
                if (uiComponent != this) uiComponent.DestroyUIComponent();
            }
            UIController.DeregisterUIObject(this);
            _supressDestroyWarning = true;
            Destroy(this.gameObject);
        }
        private void OnDestroy()
        {
            if (!_supressDestroyWarning && !SharedData.SupressDestroyWarningGlobally) Debug.LogError($"{GetType().Name} destroyed illegally. UI Component's should only ever be destroyed by calling UIComponent.DestroyUIComponent() ");
        }
    }
}