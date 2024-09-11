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

        }

        public override void OnResyncEnded(){}

        public override void UIUpdate()
        {
            if (UIController.SelectionChanged)
            {
                string cadreInfo = "";
                text = $"<color=#c6ffbf>Year: {GameState.Year.ToString()}\n" +
                       $"<color=#fbffbf>Game Phase: {GameState.GamePhase.ToString()}\n" +
                       $"<color=#ffd9bf>Season: {GameState.Season.ToString()}\n" +
                       $"<color=#ffbfce>Active Player: {GameState.ActivePlayer.ToString()}\n"
                    ;


                MapObject selectedMapObject = UIController.SelectedMapObject;
                foreach (var field in selectedMapObject.GetType().GetFields())
                {
                    if (field.GetCustomAttributes(typeof(ShowInDebugWindowAttribute), inherit:false).Length == 0) continue;
                    const int cap = 5;
                    string valueString = field.GetValue(selectedMapObject)?.ToString();
                    if (valueString?.Length > cap + 3)
                    {
                        valueString = valueString.Substring(0, cap);
                        valueString += "...";
                    }
                    text += $"{field.Name} = {valueString}\n";
                }
                foreach (var property in selectedMapObject.GetType().GetProperties())
                {
                    if (property.GetCustomAttributes(typeof(ShowInDebugWindowAttribute), inherit:false).Length == 0) continue;
                    const int cap = 5;
                    string valueString = property.GetValue(selectedMapObject)?.ToString();
                    if (valueString?.Length > cap + 3)
                    {
                        valueString = valueString.Substring(0, cap);
                        valueString += "...";
                    }
                    text += $"{property.Name} = {valueString}\n";
                }
            }
        }
        
        

        public string text
        {
            get => tmp.text;
            set => tmp.text = value;
        }
    }
}