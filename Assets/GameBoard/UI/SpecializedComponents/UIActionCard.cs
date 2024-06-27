using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIActionCard : UICard
    {
        private int _actionsHash = 0;
        public override void Refresh(ICard gameCard)
        {
            base.Refresh(gameCard);
            
            IActionCard actionCard = gameCard as IActionCard;




            
            // Card Actions
            int actionsHash = HashCode.Combine(actionCard.Initiative, actionCard.NumActions);
            for (int i = 0; i < actionCard.Countries.Count; i++)
            {
                unchecked
                {
                    actionsHash ^= actionCard.Countries[i].GetHashCode() * 17;
                }
            }

            for (int i = 0; i < actionCard.SpecialDiplomacyActions.Count; i++)
            {
                unchecked
                {
                    actionsHash *= actionCard.SpecialDiplomacyActions[i].GetStateHashCode() * 17;
                }
            }

            if (actionsHash != _actionsHash)
            {
                _actionsHash = actionsHash;
                ClearCardEffects();
                
                UICardEffect commandCardEffect = InstantiateCommandEffect(actionCard.ID);

                for (int i = 0; i < actionCard.Countries.Count; i++)
                {
                    UICardEffect countryCardEffect = InstantiateCountryEffect(actionCard.Countries[i]);
                }

                for (int i = 0; i < actionCard.SpecialDiplomacyActions.Count; i++)
                {
                    SpecialDiplomacyActionFactionDefinition myFactionDefinition =
                        actionCard.SpecialDiplomacyActions[i].GetFactionDefinition(iPlayer);
                    if (myFactionDefinition != null)
                    {
                        if (myFactionDefinition.IsInsurgentAction)
                        {
                            foreach (var iCountry in myFactionDefinition.iCountries)
                            {
                                UICardEffect countryCardEffect = InstantiateInsurgentEffect(isCountryTargeting:true, iCountry);
                            }
                            foreach (var iTile in myFactionDefinition.iTiles)
                            {
                                UICardEffect countryCardEffect = InstantiateInsurgentEffect(isCountryTargeting:false, iTile);
                            }
                        }
                        else
                        {
                            foreach (var iCountry in myFactionDefinition.iCountries)
                            {
                                UICardEffect countryCardEffect = InstantiateCountryEffect(iCountry);
                            }
                        }
                    }
                }
            }
        }


        // Utility
        UICommandCardEffect InstantiateCommandEffect(int iCard)
        {
            try
            {
                UICommandCardEffect commandCardEffect = 
                    (UICommandCardEffect)InstantiateCardEffect(CardHand.ActionCardCommandEffectPrefab, isMainEffect:true);
                commandCardEffect.SetCardTarget(iCard);
                return commandCardEffect;
            }
            catch (InvalidCastException e)
            {
                Debug.LogError("card prefab does not contain the ui effect component");
                return null;
            }
        }
        UICountryEffect InstantiateCountryEffect(int iCountry)
        {
            try
            {
                UICountryEffect countryCardEffect = 
                    (UICountryEffect)InstantiateCardEffect(CardHand.ActionCardCountryEffectPrefab);
                countryCardEffect.SetCountry(iCountry);
                return countryCardEffect;
            }
            catch (InvalidCastException e)
            {
                Debug.LogError("Insurgent card prefab does not contain the insurgent ui effect component");
                return null;
            }
        }

        UIInsurgentEffect InstantiateInsurgentEffect(bool isCountryTargeting, int iTileOriCountry)
        {
            try
            {
                UIInsurgentEffect insurgentCardEffect = 
                    (UIInsurgentEffect)InstantiateCardEffect(CardHand.ActionCardInsurgentEffectPrefab);
                if (isCountryTargeting)
                {
                    insurgentCardEffect.SetCountry(iTileOriCountry);
                }
                else
                {
                    insurgentCardEffect.SetTile(iTileOriCountry);
                }

                return insurgentCardEffect;
            }
            catch (InvalidCastException e)
            {
                Debug.LogError("Insurgent card prefab does not contain the insurgent ui effect component");
                return null;
            }
        }



    }
}