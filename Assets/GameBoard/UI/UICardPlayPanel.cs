using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI
{
    public enum CardPlayOption
    {
        None,
        Diplomacy,
        Insurgents,
        Commands,
        Industry,
        Tech
    }
    public class UICardPlayPanel : UIComponent
    {
        public Image Image;
        [NonSerialized] public CardPlayOption CardPlayOption;
        [NonSerialized] public RectTransform RectTransform;

        public override void Start()
        {
            base.Start();
            RectTransform = GetComponent<RectTransform>();
        }

        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
        }

        public override void UIUpdate()
        {
            if (UIController.UICardPlayPanelUnderPointer == this.gameObject || UIController.CardHand.TargetedCardPlayPanel == this)
            {
                Image.color = Color.white * 0.7f;
            }
            else
            {
                Image.color = Color.white;
            }
        }
    }
}