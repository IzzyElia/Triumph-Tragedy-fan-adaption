using System;
using Coffee.UIExtensions;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelDie : UIComponent
    {
        public static CombatPanelDie Create(GameObject prefab, CombatPanelDecisionManager decisionWrapper, CombatPanelDiceOption startingPanel, int startingPanelPositionIndex, UIController uiController)
        {
            Random.InitState(DateTime.Now.GetHashCode());
            
            CombatPanelDie die = Instantiate(prefab, startingPanel.transform).GetComponent<CombatPanelDie>();
            die.DroppedOnPanel = startingPanel;
            
            // starting position calculation
            RectTransform dieRect = die.GetComponent<RectTransform>();
            RectTransform panelRect = startingPanel.RectTransform;
            dieRect.sizeDelta = new Vector2(120, 120);
            dieRect.anchorMin = Vector2.zero;
            dieRect.anchorMax = Vector2.zero;
            dieRect.anchoredPosition = new Vector3(
                x: Random.Range(0, panelRect.sizeDelta.x),
                y: Random.Range(0, panelRect.sizeDelta.y),
                z: 0);
            /*
            float xPos = dieRect.rect.width / startingPanelPositionIndex;
            int row = (int)Math.Floor(tw / panelRect.rect.width);
            dieRect.position = new Vector3(
                x:panelRect.rect.xMin + ((tw + dieRect.rect.width / 2) % panelRect.rect.width),
                y:panelRect.rect.yMax - (row * dieRect.rect.height),
                z: 0);*/
            
            uiController.RegisterUIComponent(die);
            return die;
        }
        
        [NonSerialized] public CombatPanelDiceOption DroppedOnPanel;
        public UIShiny shinyEffect;
        private Image _image;
        private Color _baseColor;
        private bool _hoveredOver;

        public void Awake()
        {
            _image = GetComponent<Image>();
            _baseColor = _image.color;
        }

        public override void OnGamestateChanged()
        {
            
        }

        public override void OnResyncEnded()
        {
        }

        public override void UIUpdate()
        {
            if (!_hoveredOver && UIController.UICombatDieUnderPointer == this.gameObject)
            {
                _hoveredOver = true;
                shinyEffect.Play();
                RecalculateAppearance();
            }
            else if (_hoveredOver && UIController.UICombatDieUnderPointer != this.gameObject)
            {
                _hoveredOver = false;
                shinyEffect.Stop(reset:true);
                RecalculateAppearance();
            }
        }

        public void RecalculateAppearance()
        {
            if (_hoveredOver)
            {
                _image.color = _baseColor * 0.5f;
            }
            else
            {
                _image.color = _baseColor;
            }
        }
    }
}