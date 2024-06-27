using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UICommandsInfo : UIWindow
    {
        [SerializeField] private GameObject commandsPanel;
        [SerializeField] private TextMeshProUGUI commandsHeaderText;
        [SerializeField] private TextMeshProUGUI commandsAvailableText;


        public void Refresh()
        {
            (int commandsAvailable, int commandsRemaining) = UIController.GetCommands();
            commandsAvailableText.text = $"{commandsRemaining}/{commandsAvailable}";
        }
        
        public override void OnGamestateChanged()
        {
            Refresh();
        }

        public override void OnResyncEnded()
        {
            Refresh();
        }

        public override bool WantsToBeActive => GameState.GamePhase == GamePhase.GiveCommands;
        protected override void OnActive()
        {
            
        }

        protected override void OnHidden()
        {
            
        }
    }
}