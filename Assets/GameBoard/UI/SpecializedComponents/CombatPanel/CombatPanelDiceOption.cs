using System;
using System.Collections.Generic;
using GameSharedInterfaces;
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
        private Color _overlayBaseColor;
        private Color _backgroundBaseColor;
        private void Awake()
        {
            _overlayBaseColor = overlay.color;
            _backgroundBaseColor = background.color;
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
                    break;
            }
            if (animationData.firingTargetType == this.UnitCategory)
            {
                overlay.color = new Color(_overlayBaseColor.r, _overlayBaseColor.g, _overlayBaseColor.b, _overlayBaseColor.a * timeData.DarkenProgress);
            }
            else
            {
                overlay.color = Color.clear;
            }
        }

        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
        }

        
        public override void UIUpdate()
        {
        }

        public void OnHoveredStatusChanged(bool isHoveredOver)
        {
            if (isHoveredOver)
            {
                const float mod = 0.7f;
                background.color = new Color(_backgroundBaseColor.r * mod, _backgroundBaseColor.g * mod, _backgroundBaseColor.b * mod, _backgroundBaseColor.a);
            }
            else
            {
                background.color = _backgroundBaseColor;
            }
        }
    }
}