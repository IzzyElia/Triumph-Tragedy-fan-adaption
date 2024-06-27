using System;
using GameBoard.UI.SpecializeComponents.GameBoard.UI.SpecializeComponents;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIInvestmentCard : UICard
    {
        private int _techsHash = 0;
        public override void Refresh(ICard gameCard)
        {
            base.Refresh(gameCard);
            
            IInvestmentCard investmentCard = gameCard as IInvestmentCard;

            
            int techsHash = 17;
            for (int i = 0; i < investmentCard.Techs.Count; i++)
            {
                unchecked
                {
                    techsHash ^= investmentCard.Techs[i].GetHashCode() * 17;
                }
            }

            if (techsHash != _techsHash)
            {
                _techsHash = techsHash;
                ClearCardEffects();
                foreach (var iTech in investmentCard.Techs)
                {
                    Tech tech = GameState.Ruleset.GetTech(iTech);
                    InstantiateTechEffect(tech);
                }
            }
        }

        UITechEffect InstantiateTechEffect(Tech tech)
        {
            try
            {
                UITechEffect techCardEffect = 
                    (UITechEffect)InstantiateCardEffect(CardHand.InvestmentCardTechEffectPrefab);
                techCardEffect.SetTech(tech);

                return techCardEffect;
            }
            catch (InvalidCastException e)
            {
                Debug.LogError("Insurgent card prefab does not contain the insurgent ui effect component");
                return null;
            }
        }
    }
}