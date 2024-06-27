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
            text = $"<color=#c6ffbf>Year: {GameState.Year.ToString()}\n" +
                   $"<color=#fbffbf>Game Phase: {GameState.GamePhase.ToString()}\n" +
                   $"<color=#ffd9bf>Season: {GameState.Season.ToString()}\n" +
                   $"<color=#ffbfce>Active Player: {GameState.ActivePlayer.ToString()}\n"
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