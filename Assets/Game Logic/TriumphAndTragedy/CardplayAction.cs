using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic;
using GameSharedInterfaces;
using Izzy.ForcedInitialization;
using PlasticGui.Help;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class CardplayAction : PlayerAction
    {
        static CardplayAction()
        {
            RegisterPlayerActionType<CardplayAction>();
        }

        private CardplayInfo _cardplayInfo;
        
        public override void Execute()
        {
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            switch (_cardplayInfo.CardPlayType)
            {
                case CardPlayType.Pass:
                    GameState.PlayerPassed[iPlayerFaction] = true;
                    break;
                case CardPlayType.Diplomacy:
                    foreach (var iCard in _cardplayInfo.iCardsUsed) ((ActionCard)GameState.GetCard(iCard, CardType.Action)).SetHoldingPlayerAndPush(-1);
                    GameState.PlayerPassed[iPlayerFaction] = false;
                    GameCountry diploTargetCountry = GameState.GetEntity<GameCountry>(_cardplayInfo.iTarget);
                    diploTargetCountry.influencePlayedByPlayer[iPlayerFaction]++;
                    Debug.Log(diploTargetCountry.ID + $" has {diploTargetCountry.influencePlayedByPlayer[iPlayerFaction]} influence applied");
                    diploTargetCountry.PushFullState();
                    break;
                case CardPlayType.Insurgents:
                    foreach (var iCard in _cardplayInfo.iCardsUsed) ((ActionCard)GameState.GetCard(iCard, CardType.Action)).SetHoldingPlayerAndPush(-1);
                    GameState.PlayerPassed[iPlayerFaction] = false;
                    throw new NotImplementedException();
                case CardPlayType.Industry:
                    foreach (var iCard in _cardplayInfo.iCardsUsed) ((InvestmentCard)GameState.GetCard(iCard, CardType.Investment)).SetHoldingPlayerAndPush(-1);
                    GameState.PlayerPassed[iPlayerFaction] = false;
                    playerFaction.Industry++;
                    playerFaction.PushFullState();
                    break;
                case CardPlayType.Tech:
                    foreach (var iCard in _cardplayInfo.iCardsUsed) ((InvestmentCard)GameState.GetCard(iCard, CardType.Investment)).SetHoldingPlayerAndPush(-1);
                    GameState.PlayerPassed[iPlayerFaction] = false;
                    playerFaction.iTechs.Add(_cardplayInfo.iTarget);
                    playerFaction.PushFullState();
                    break;
                case CardPlayType.Command:
                    ActionCard commandCard =
                        (ActionCard)GameState.GetCard(_cardplayInfo.iCardsUsed[0], CardType.Action);
                    foreach (var iCard in _cardplayInfo.iCardsUsed) ((ActionCard)GameState.GetCard(iCard, CardType.Action)).SetHoldingPlayerAndPush(-1);
                    playerFaction.CommandsAvailable = commandCard.NumActions;
                    playerFaction.CommandInitiative = commandCard.Initiative;
                    GameState.PlayerPassed[iPlayerFaction] = true;
                    playerFaction.PushFullState();
                    GameState.PushGlobalFields();
                    break;
                default: throw new NotImplementedException();
            }

            if (GameState.HaveAllPlayersPassed)
            {
                if (GameState.GamePhase == GamePhase.Diplomacy) GameState.EndCardplay();
                else if (GameState.GamePhase == GamePhase.SelectCommandCards) GameState.EndCommandCardSelection();
            }
            else
            {
                GameState.AdvanceTurnMarker();
            }
            GameState.PushGlobalFields();
        }

        public override (bool, string) TestParameter(params object[] parameter)
        {
            return CardplayValid((CardplayInfo)parameter[0]);
        }

        public override void AddParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            throw new NotImplementedException();
        }

        public override void SetAllParameters(params object[] parameters)
        {
            _cardplayInfo = (CardplayInfo)parameters[0];
        }

        public override object[] GetParameters()
        {
            return new object[1] { _cardplayInfo };
        }

        public override object[] GetData()
        {
            throw new System.NotImplementedException();
        }

        private const string _doesNotHoldCardError = "Attempting to play a card you do not hold";
        private const string _notInCardplayError = "Not in cardplay phase";
        (bool, string) CardplayValid(CardplayInfo cardplayInfo)
        {
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            if (GameState.GamePhase == GamePhase.Diplomacy && GameState.ActivePlayer != iPlayerFaction) return (false, "Not your turn");
            else if (GameState.GamePhase == GamePhase.SelectCommandCards && GameState.PlayerCommitted[iPlayerFaction]) return (false, "Already committed");
            switch (cardplayInfo.CardPlayType)
            {
                case CardPlayType.None: return (false, "Must either play a card or pass");
                case CardPlayType.Pass:
                    if (GameState.GamePhase != GamePhase.Diplomacy &&
                        GameState.GamePhase != GamePhase.SelectCommandCards)
                        return (false, _notInCardplayError);
                    else
                        return (true, null);
                case CardPlayType.Diplomacy:
                    if (GameState.GamePhase != GamePhase.Diplomacy)
                        return (false, _notInCardplayError);
                    foreach (var iCard in cardplayInfo.iCardsUsed)
                    {
                        ActionCard actionCard = GameState.GetCard(iCard, CardType.Action) as ActionCard;
                        if (actionCard.HoldingPlayer != iPlayerFaction)
                            return (false, _doesNotHoldCardError);
                        if (actionCard.Countries.Contains(cardplayInfo.iTarget)) return (true, null);
                        foreach (var specialDiplomacyAction in actionCard.SpecialDiplomacyActions)
                        {
                            if (specialDiplomacyAction.GetFactionDefinition(iPlayerFaction).iCountries
                                .Contains(cardplayInfo.iTarget)) return (true, null);
                        }
                    }
                    break;
                case CardPlayType.Command:
                    if (GameState.GamePhase != GamePhase.SelectCommandCards)
                        return (false, "Not in command card phase");
                    foreach (var iCard in cardplayInfo.iCardsUsed)
                    {
                        if (GameState.GetCard(iCard, CardType.Action).HoldingPlayer != iPlayerFaction)
                            return (false, _doesNotHoldCardError);
                    }

                    return (true, null);
                case CardPlayType.Industry:
                    if (GameState.GamePhase != GamePhase.Diplomacy)
                        return (false, _notInCardplayError);
                    int totalFactoriesOnCards = 0;
                    foreach (var iCard in cardplayInfo.iCardsUsed)
                    {
                        InvestmentCard investmentCard = GameState.GetCard(iCard, CardType.Investment) as InvestmentCard;
                        if (investmentCard.HoldingPlayer != iPlayerFaction)
                            return (false, _doesNotHoldCardError);
                        totalFactoriesOnCards += investmentCard.FactoryValue;
                    }

                    if (totalFactoriesOnCards >= playerFaction.FactoriesNeededForIndustryUpgrade)
                        return (true, null);
                    else
                        return (false, "Would not be enough factories on played cards for industry upgrade");
                case CardPlayType.Tech:
                    if (GameState.GamePhase != GamePhase.Diplomacy)
                        return (false, _notInCardplayError);
                    Tech attemptingToPlayTech = GameState.Ruleset.GetTech(cardplayInfo.iTarget);
                    if (playerFaction.HasTech(attemptingToPlayTech.ID))
                        return (false, $"Already have the tech {attemptingToPlayTech.ToString()}");
                    int totalTechMatchesOnCards = 0;
                    foreach (var iCard in cardplayInfo.iCardsUsed)
                    {
                        InvestmentCard investmentCard = GameState.GetCard(iCard, CardType.Investment) as InvestmentCard;
                        if (investmentCard.HoldingPlayer != iPlayerFaction)
                            return (false, _doesNotHoldCardError);
                        if (investmentCard.Techs.Contains(attemptingToPlayTech.ID)) totalTechMatchesOnCards++;
                    }

                    if (totalTechMatchesOnCards >= 2)
                        return (true, null);
                    else
                        return (false, "Would not be enough factories on played cards for industry upgrade");
                case CardPlayType.Insurgents:
                    throw new NotImplementedException();
                default: throw new NotImplementedException();
            }

            return (false, "You do not have the right card/'s in hand to play");
        }

        public override (bool, string) Validate()
        {
            return CardplayValid(_cardplayInfo);
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            _cardplayInfo = CardplayInfo.Recreate(ref incomingMessage);
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            _cardplayInfo.Write(ref outgoingMessage);
        }

        public override void Reset()
        {
            _cardplayInfo = new CardplayInfo(CardEffectTargetSelectionType.None, CardPlayType.None, -1, new int[0]);
        }
    }
}