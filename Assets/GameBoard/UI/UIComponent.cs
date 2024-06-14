using System;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameBoard.UI
{
    public abstract class UIComponent : MonoBehaviour
    {
        public abstract void OnGamestateChanged();
        public abstract void OnResyncEnded();
        public abstract void UIUpdate();
        protected Map MapRenderer => UIController.MapRenderer;
        protected ITTGameState GameState => UIController.GameState;
        protected MapFaction PlayerFaction => UIController.PlayerMapFaction;
        protected MapCountry PlayerCountry => UIController.PlayerMapFaction.leader;


        
        
        
        [FormerlySerializedAs("name")] public string globalName;
        [NonSerialized] public UIController UIController;
        
        



        
        // Special destruction logic
        private void OnDestroy()
        {
            #if !UNITY_EDITOR
            UIController.DerigsterUIObject(this);
            #endif
        }
    }
}