using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelDiceOption : UIComponent, ICombatPanelAnimationParticipant
    {
        [SerializeField] public RectTransform RectTransform;
        [SerializeField] public UnitCategory UnitCategory;
        [SerializeField] private Image overlay;
        [SerializeField] private Image background;
        [SerializeField] private Image unitCategoryIcon;
        [SerializeField] private Image increaseButton;
        [SerializeField] private Image decreaseButton;
        [SerializeField] private TextMeshProUGUI selectedDiceText; 
        [NonSerialized] public CombatPanelDecisionManager DecisionManager;
        private Image[] allImages;
        private Color _overlayBaseColor;
        private Color _backgroundBaseColor;

        private void Awake()
        {
            _overlayBaseColor = overlay.color;
            _backgroundBaseColor = background.color;
            allImages = GetComponentsInChildren<Image>();
        }


        // Triggered by the parent CombatDecisionManager
        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            switch (timeData.AnimationState)
            {
                case AnimationState.FirstFrame:
                    overlay.gameObject.SetActive(true);
                    break;
                case AnimationState.LastFrame:
                    overlay.gameObject.SetActive(false);
                    background.color = _backgroundBaseColor;
                    break;
            }
            if (animationData.firingTargetType == this.UnitCategory)
            {
                const float darkenMod = 0.1f;
                background.color = new Color(_backgroundBaseColor.r * darkenMod * timeData.DarkenProgress, _backgroundBaseColor.g * darkenMod * timeData.DarkenProgress, _backgroundBaseColor.b * darkenMod * timeData.DarkenProgress, _backgroundBaseColor.a - timeData.DarkenProgress / 2);
            }
            else
            {
                background.color = new Color(_backgroundBaseColor.r, _backgroundBaseColor.g, _backgroundBaseColor.b, _backgroundBaseColor.a - timeData.DarkenProgress);
            }
        }

        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
            
        }
        

        static Color _hoverColor = new Color(0.4f, 0.4f, 0.4f, 1);
        public override void UIUpdate()
        {
            // Animation stuff
            
            if (UIController.CombatPanel.AnimationOngoing) return;
            
            // Non-animation stuff
            if (UIController.UIObjectAtPointer == increaseButton.gameObject)
            {
                increaseButton.color = _hoverColor;
                decreaseButton.color = Color.white;
                if (UIController.PointerInputStatus == InputStatus.Pressed)
                {
                    DecisionManager.AdjustDice(UnitCategory, 1);
                }
            }
            else if (UIController.UIObjectAtPointer == decreaseButton.gameObject)
            {
                increaseButton.color = Color.white;
                decreaseButton.color = _hoverColor;
                if (UIController.PointerInputStatus == InputStatus.Pressed)
                {
                    DecisionManager.AdjustDice(UnitCategory, -1);
                }
            }
            else
            {
                increaseButton.color = Color.white;
                decreaseButton.color = Color.white;
            }
        }

        public void OnDiceDistributionUpdated(CombatDiceDistribution diceDistribution)
        {
            switch (UnitCategory)
            {
                case UnitCategory.Air:
                    selectedDiceText.text = diceDistribution.AirDice.ToString();
                    break;
                case UnitCategory.Ground:
                    selectedDiceText.text = diceDistribution.GroundDice.ToString();
                    break;
                case UnitCategory.Sea:
                    selectedDiceText.text = diceDistribution.SeaDice.ToString();
                    break;
                case UnitCategory.Sub:
                    selectedDiceText.text = diceDistribution.SubDice.ToString();
                    break;
                default: throw new NotImplementedException();
            }
        }
        
        public enum Buttons
        {
            Increase,
            Decrease,
            None
        }
    }
}