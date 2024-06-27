using System;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIInsurgentEffect : UICardEffect
    {
        [NonSerialized] public bool IsNationalInsurgentEffect;
        [NonSerialized] public int iTileOriCountry;
        public void SetCountry(int country)
        {
            MapCountry mapCountry = UIController.MapRenderer.MapCountriesByID[country];
            this.iTileOriCountry = country;
            IsNationalInsurgentEffect = true;
            textMesh[0].text = mapCountry.name;
        }
        public void SetTile(int tile)
        {
            MapTile mapTile = UIController.MapRenderer.MapTilesByID[tile];
            this.iTileOriCountry = tile;
            IsNationalInsurgentEffect = false;
            textMesh[0].text = mapTile.name;
        }

        public override void OnActivated()
        {
            if (HighlightState == CardHighlightState.Darken) return;
            if (HighlightState == CardHighlightState.Highlight)
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.None, CardPlayType.None, 0);
            }
            else
            {
                if (IsNationalInsurgentEffect) Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Country, CardPlayType.Insurgents, iTileOriCountry);
                else Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Tile, CardPlayType.Insurgents, iTileOriCountry);
            }
        }

        protected override CardHighlightState ShouldHighlight(CardplayInfo cardplayInfo)
        {
            throw new NotImplementedException();
        }
    }
}