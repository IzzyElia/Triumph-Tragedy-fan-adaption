using System;
using System.Collections.Generic;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy.ForcedInitialization;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class ProductionAction : PlayerAction
    {
        static ProductionAction()
        {
            PlayerAction.RegisterPlayerActionType<ProductionAction>();
        }
        private List<ProductionActionData> _productionActions = new List<ProductionActionData>();
        
        public override void Execute()
        {
            GameCadre cadre;
            foreach (var productionAction in _productionActions)
            {
                List<ActionCard> actionDeck = GameCard.GetCardsInDeck<ActionCard>(GameState);
                List<InvestmentCard> investmentDeck = GameCard.GetCardsInDeck<InvestmentCard>(GameState);
               
                switch (productionAction.ActionType)
                {
                    case ProductionActionType.BuildUnit:
                        GameTile tile = GameState.GetEntity<GameTile>(productionAction.iTile);
                        cadre = GameCadre.CreateCadre(GameState, productionAction.iUnitType, tile.Country.ID,
                            productionAction.iTile);
                        cadre.RecalculateDerivedValuesAndPushFullState();
                        break;
                    case ProductionActionType.ReinforceUnit:
                        cadre = GameState.GetEntity<GameCadre>(productionAction.iCadre);
                        cadre.Pips += 1;
                        cadre.RecalculateDerivedValues();
                        cadre.RecalculateDerivedValuesAndPushFullState();
                        break;
                    case ProductionActionType.DrawCard:
                        if (productionAction.CardType == CardType.Action)
                        {
                            int cardToDraw = Random.Range(minInclusive:0, maxExclusive:actionDeck.Count);
                            actionDeck[cardToDraw].HoldingPlayer = iPlayerFaction;
                            actionDeck[cardToDraw].RecalculateDerivedValuesAndPushFullState();
                            actionDeck.RemoveAt(cardToDraw);
                        }
                        else if (productionAction.CardType == CardType.Investment)
                        {
                            int cardToDraw = Random.Range(minInclusive:0, maxExclusive:investmentDeck.Count);
                            investmentDeck[cardToDraw].HoldingPlayer = iPlayerFaction;
                            investmentDeck[cardToDraw].RecalculateDerivedValuesAndPushFullState();
                            investmentDeck.RemoveAt(cardToDraw);
                        }
                        break;
                    default: throw new NotImplementedException();
                }
            }

            if (GameState.AdvanceTurnMarker())
            {
                for (int i = 0; i < GameState.PlayerPassed.Length; i++)
                {
                    GameState.PlayerPassed[i] = false;
                }
                GameState.GamePhase = GamePhase.Diplomacy;
            }
            GameState.PushGlobalFields();
        }

        private List<ProductionActionData> _testingProductionActions = new List<ProductionActionData>();
        public override (bool, string) TestParameter(params object[] parameters)
        {
            _testingProductionActions = new List<ProductionActionData>(_productionActions);
            for (int i = 0; i < parameters.Length; i++)
            {
                try
                {
                    _testingProductionActions.Add((ProductionActionData)parameters[i]);
                }
                catch (InvalidCastException e)
                {
                    Debug.LogError("Invalid data type passed to action");
                }
            }

            return Validate(_testingProductionActions);
        }

        public override void AddParameter(params object[] parameter)
        {
            _productionActions.Add((ProductionActionData)parameter[0]);
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            return _productionActions.Remove((ProductionActionData)parameter[0]);
        }

        public override void SetAllParameters(params object[] parameters)
        {
            _productionActions.Clear();
            for (int i = 0; i < parameters.Length; i++)
            {
                try
                {
                    _productionActions.Add((ProductionActionData)parameters[i]);

                }
                catch (InvalidCastException e)
                {
                    Debug.LogError("Invalid data type passed to action");
                }
            }
        }

        public override object[] GetParameters()
        {
            object[] output = new object[_productionActions.Count];
            for (int i = 0; i < _productionActions.Count; i++)
            {
                output[i] = _productionActions[i];
            }

            return output;
        }

        public override object[] GetData()
        {
            return new object[] { GetProductionUsed(_productionActions) };
        }

        int GetProductionUsed(List<ProductionActionData> productionActions)
        {
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            int productionUsed = 0;
            for (int i = 0; i < productionActions.Count; i++)
            {
                ProductionActionData productionAction = productionActions[i];
                if (productionAction.ActionType == ProductionActionType.BuildUnit)
                {

                    productionUsed += 1;
                }
                else if (productionAction.ActionType == ProductionActionType.ReinforceUnit)
                {
                    productionUsed += 1;
                }
                else if (productionAction.ActionType == ProductionActionType.DrawCard)
                {
                    productionUsed += 1;
                }
            }

            return productionUsed;
        }
        
        (bool, string) Validate(List<ProductionActionData> productionActions)
        { 
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            int productionUsed = GetProductionUsed(productionActions);
            int actionCardsDrawn = 0;
            int investmentCardsDrawn = 0;
            for (int i = 0; i < productionActions.Count; i++)
            {
                ProductionActionData productionAction = productionActions[i];
                if (productionAction.ActionType == ProductionActionType.BuildUnit)
                {
                    GameTile tile = GameState.GetEntity<GameTile>(productionAction.iTile);
                    UnitType unitType = GameState.Ruleset.unitTypes[productionAction.iUnitType];
                    if (tile == null) return (false, "Invalid tile id");
                    if (unitType == null) return (false, "Invalid unit type ");
                    if (!(tile.TerrainType == TerrainType.Land || tile.TerrainType == TerrainType.Strait))
                        return (false, "Unit must be placed on land");
                    if (tile.Occupier != playerFaction) return (false, "tile not controlled by your faction");
                    if ((unitType.Category == UnitCategory.Sea || unitType.Category == UnitCategory.Sub) && !tile.IsCoastal) 
                        return (false, "Navel units can only be built on coastal tiles");
                    if (!unitType.IsBuildableThroughNormalPlacementRules)
                        return (false, "Unit type cannot be built through normal placement");
                    switch (GameState.Ruleset.unitPlacementRule)
                    {
                        case UnitPlacementRule.HomeTerritoryOnly:
                            if (!(tile.Country.Faction == playerFaction && tile.Country.MembershipStatus == FactionMembershipStatus.InitialMember))
                                return (false, "Country must be a starting country of your faction");
                            break;
                        case UnitPlacementRule.DiploAnnexedCountriesAllowed:
                            if ((tile.Country.Faction != playerFaction || !(
                                    tile.Country.MembershipStatus == FactionMembershipStatus.InitialMember ||
                                    tile.Country.MembershipStatus == FactionMembershipStatus.Ally
                                ))) return (false, "Country must be a diplomatic member of your faction");
                            break;
                        case UnitPlacementRule.AllControlledTerritory:
                            break;
                    }

                }
                else if (productionAction.ActionType == ProductionActionType.ReinforceUnit)
                {
                    GameCadre cadre = GameState.GetEntity<GameCadre>(productionAction.iCadre);
                    if (cadre == null) return (false, "Invalid cadre id");
                    if (cadre.Faction != playerFaction) return (false, "Cadre not a member of your faction");
                    if (cadre.Pips + 1 > GameState.Ruleset.maxCadrePips)
                        return (false, "Cannot add more pips to the cadre");
                }
                else if (productionAction.ActionType == ProductionActionType.DrawCard)
                {
                    switch (productionAction.CardType)
                    {
                        case CardType.Action:
                            actionCardsDrawn++;
                            break;
                        case CardType.Investment:
                            investmentCardsDrawn++;
                            break;
                        default: throw new NotImplementedException("Don't forget to add the check below as well");
                    }
                }
                else
                {
                    return (false, "Attempted to pass invalid or improperly serialized production actions");
                }
            }
            if (productionUsed > playerFaction.ProductionAvailable)
                return (false, "Would use more production than you have");
            if (actionCardsDrawn > GameCard.GetCardsInDeck<ActionCard>(GameState).Count)
                return (false, "Not enough action cards in the deck");
            if (investmentCardsDrawn > GameCard.GetCardsInDeck<InvestmentCard>(GameState).Count)
                return (false, "Not enough investment cards in the deck");
            return (true, null);
        }
        public override (bool, string) Validate()
        {
            return Validate(_productionActions);
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            int numProductionActions = incomingMessage.ReadInt();
            _productionActions.Clear();
            for (int i = 0; i < numProductionActions; i++)
            {
                ProductionActionData deserializedData = ProductionActionData.Recreate(ref incomingMessage);
                _productionActions.Add(deserializedData);
            }
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(_productionActions.Count);
            for (int i = 0; i < _productionActions.Count; i++)
            {
                _productionActions[i].Write(ref outgoingMessage);
            }
        }

        public override void Reset()
        {
            _productionActions.Clear();
        }
    }
}