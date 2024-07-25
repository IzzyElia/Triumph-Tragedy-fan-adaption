using System;
using TMPro;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class DebugTextbox : UIWindow
    {
        [SerializeField] private TextMeshProUGUI tmp;
        public override bool WantsToBeActive => true;
        protected override void OnActive() {}

        protected override void OnHidden() {}

        public override void OnGamestateChanged()
        {
            string cadreInfo = "";
            if (UIController.SelectedMapObjects.Count > 0 && UIController.SelectedMapObjects[0] is MapCadre cadre)
            {
                cadreInfo = "Cadre ID: " + cadre.ID.ToString();
            }
            text = $"<color=#c6ffbf>Year: {GameState.Year.ToString()}\n" +
                   $"<color=#fbffbf>Game Phase: {GameState.GamePhase.ToString()}\n" +
                   $"<color=#ffd9bf>Season: {GameState.Season.ToString()}\n" +
                   $"<color=#ffbfce>Active Player: {GameState.ActivePlayer.ToString()}\n" +
                   cadreInfo
                ;
        }

        public override void OnResyncEnded(){}

        public override void UIUpdate()
        {
            
        }

        public string text
        {
            get => tmp.text;
            set => tmp.text = value;
        }
    }
}