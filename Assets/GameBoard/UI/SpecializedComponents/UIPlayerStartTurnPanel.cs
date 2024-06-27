using System;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIPlayerStartTurnPanel : UIComponent
    {
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI factionLabel;
        [SerializeField] private float dissolveSpeed = 1;



        private bool _dissolving = true;
        private float _dissolveStartTime = -1;
        private float _dissolveProgress = 0;
        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
        }

        public override void UIUpdate()
        {
            if (!image.enabled) return;

            if (_dissolving)
            {
                image.material.SetFloat("_DissolveProgress", _dissolveProgress);
                _dissolveProgress += Time.deltaTime / dissolveSpeed;
            }

            if (UIController.PointerInputStatus == InputStatus.Pressed)
            {
                image.material.SetInt("_Dissolving", 1);
                _dissolving = true;
                factionLabel.enabled = false;
                _dissolveProgress = 0;
            }

            if (_dissolving && _dissolveProgress >= 1)
            {
                image.enabled = false;
                factionLabel.enabled = false;
            }
        }

        public void Setup(MapFaction faction)
        {
            image.enabled = true;
            _dissolving = false;
            _dissolveProgress = 0;
            factionLabel.enabled = true;
            if (faction.leader is not null)
                image.sprite = faction.leader.FlagSprite;
            else image.sprite = Resources.Load<Sprite>("Graphics/CommandsAvailablePanel");
            image.material.SetInt("_Dissolving", 0);
            image.material.SetFloat("_DissolveProgress", 0);
            factionLabel.text = faction.name;
        }
    }
}