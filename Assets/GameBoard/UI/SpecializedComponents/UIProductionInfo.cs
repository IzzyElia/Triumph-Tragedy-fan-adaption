using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIProductionInfo : UIComponent
    {
        [SerializeField] private GameObject productionPanel;
        [SerializeField] private GameObject productionDetailsPanel;
        [SerializeField] private TextMeshProUGUI productionText;
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private TextMeshProUGUI resourcesText;
        [SerializeField] private TextMeshProUGUI industryText;


        public void Refresh()
        {
            IGameFaction faction = GameState.GetFaction(iPlayer);
            int productionUsed = (int)UIController.ProductionAction.GetData()[0];
            int productionRemaining = faction.ProductionAvailable - productionUsed;
            if (GameState.GamePhase == GamePhase.Production)
                productionText.text = $"{productionRemaining}/{faction.Production}";
            else productionText.text = $"{faction.Production}";

            populationText.text = faction.Population.ToString();
            resourcesText.text = faction.Resources.ToString();
            industryText.text = faction.Industry.ToString();
        }
        
        public override void OnGamestateChanged()
        {
            Refresh();
        }

        public override void OnResyncEnded()
        {
            Refresh();
        }
        
        public override void UIUpdate()
        {
            /* Deal with this when you feel like dealing with UIObjectAtPointer
            if (productionDetailsPanel.activeSelf == false && (UIController.UIObjectAtPointer == productionDetailsPanel.gameObject))
            {
                productionDetailsPanel.SetActive(true);
            }
            if (productionDetailsPanel.activeSelf && !(UIController.UIObjectAtPointer == productionDetailsPanel.gameObject || UIController.UIObjectAtPointer == productionPanel.gameObject))
            {
                productionDetailsPanel.SetActive(false);
            }
            */
        }
    }
}