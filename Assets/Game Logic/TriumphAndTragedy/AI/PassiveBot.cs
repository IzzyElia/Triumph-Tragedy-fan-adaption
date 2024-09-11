using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine.Tilemaps;
using Izzy;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Game_Logic.TriumphAndTragedy.AI
{
    public class PassiveBot : Bot
    {
        
        
        
        public override PlayerAction AI_InitialUnits(TTGameState gameState)
        {
            List<(int iTile, int iCountry, int startingCadres)> startingUnits =
                new List<(int iTile, int iCountry, int startingCadres)>(playedFaction.startingUnits);
            List<(int iTile, byte unitType, int iCountry, byte pips)> builds =
                new List<(int iTile, byte unitType, int iCountry, byte pips)>();
            HashSet<int> fortressesBuiltOn = new HashSet<int>();
            foreach ((int iTile, int iCountry, int startingCadres) startingUnit in IzzyUtility.Shuffle(startingUnits, new Random(DateTime.UtcNow.Ticks.GetHashCode())))
            {
                for (int i = 0; i < startingUnit.startingCadres; i++)
                {
                    if (fortressesBuiltOn.Contains(startingUnit.iTile))
                    {
                        builds.Add((startingUnit.iTile, (byte)gameState.Ruleset.GetNamedUnitType("Infantry").IdAndInitiative, startingUnit.iCountry, 1));
                    }
                    else
                    {
                        builds.Add((startingUnit.iTile, (byte)gameState.Ruleset.GetNamedUnitType("Fortress").IdAndInitiative, startingUnit.iCountry, 1));
                        fortressesBuiltOn.Add(startingUnit.iTile);
                    }
                }
            }

            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "InitialUnitsAction", iPlayerFaction);;
            action.SetAllParameters(builds.ToArray());
            return action;
        }

        public override PlayerAction AI_Production(TTGameState gameState)
        {
            //throw new System.NotImplementedException();
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "ProductionAction", iPlayerFaction);
            return action;
        }

        public override PlayerAction AI_Cardplay(TTGameState gameState)
        {
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "CardplayAction", iPlayerFaction);
            CardplayInfo cardplay = new CardplayInfo(CardEffectTargetSelectionType.Global, CardPlayType.Pass, -1, null);
            action.SetAllParameters(cardplay);
            return action;

        }

        public override PlayerAction AI_SelectCommandsCards(TTGameState gameState)
        {
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "CardplayAction", iPlayerFaction);
            CardplayInfo cardplay = new CardplayInfo(CardEffectTargetSelectionType.Global, CardPlayType.Pass, -1, null);
            action.SetAllParameters(cardplay);
            return action;
        }

        public override PlayerAction AI_Commands(TTGameState gameState)
        {
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "CommandsAction", iPlayerFaction);
            return action;
        }

        public override PlayerAction AI_SelectCombats(TTGameState gameState)
        {
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "SelectCombatsAction", iPlayerFaction);
            foreach (var combatOption in gameState.GetCombatOptions())
            {
                if (!combatOption.IsOptional)
                {
                    action.AddParameter(combatOption);
                }
            }
            return action;
        }

        public override PlayerAction AI_SelectSupports(TTGameState gameState)
        {
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "SelectCombatSupportAction", iPlayerFaction);
            return action;
        }

        public override PlayerAction AI_SelectNextCombat(TTGameState gameState)
        {
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "SelectNextCombatAction", iPlayerFaction);
            return action;
        }

        public override PlayerAction AI_CombatDecision(TTGameState gameState)
        {
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "CombatDecisionAction", iPlayerFaction);
            CombatDiceDistribution diceDistribution = gameState.ActiveCombat.GenerateDefaultDiceDistribution();
            action.SetAllParameters(diceDistribution);
            return action;
        }

        public override void Rebuild(TTGameState gameState)
        {
        }
        
        
        
        // Bot boilerplate ----------------------------------------------------
        public PassiveBot(TTGameState gameState) : base(gameState)
        {
            if (gameState is null) throw new ArgumentException();
        }
    }
}