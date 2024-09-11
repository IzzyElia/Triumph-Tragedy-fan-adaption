using System;
using GameLogic;
using GameSharedInterfaces;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    // The fallback action. Called when a bot fails to provide a valid action to keep the game from stalling
    public class PassAction : PlayerAction
    {
        public override void Execute()
        {
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            PlayerAction fallbackAction;
            switch (GameState.GamePhase)
            { 
                case GamePhase.InitialPlacement:
                    fallbackAction = new InitialUnitsAction();
                    foreach ((int iTile, int iCountry, int startingCadres) in playerFaction.startingUnits)
                    {
                        fallbackAction.AddParameter((iTile, GameState.Ruleset.FallbackUnitType, iCountry, 1));
                    }
                    break;
                case GamePhase.Production:
                    fallbackAction = new ProductionAction();
                    break;
                case GamePhase.Diplomacy:
                    fallbackAction = new CardplayAction();
                    fallbackAction.SetAllParameters(new CardplayInfo(CardEffectTargetSelectionType.None, CardPlayType.Pass, -1, null));
                    break;
                case GamePhase.SelectCommandCards:
                    fallbackAction = new CardplayAction();
                    fallbackAction.SetAllParameters(new CardplayInfo(CardEffectTargetSelectionType.None, CardPlayType.Pass, -1, null));
                    break;
                case GamePhase.GiveCommands:
                    fallbackAction = new CommandsAction();
                    break;
                case GamePhase.CommitCombats:
                    fallbackAction = new SelectCombatsAction();
                    break;
                case GamePhase.SelectSupport:
                    fallbackAction = new SelectCombatSupportAction();
                    break;
                case GamePhase.Combat:
                    fallbackAction = new CombatDecisionAction();
                    fallbackAction.SetAllParameters(new CombatDiceDecisionData(0, 0, 0, 0));
                    break;
                case GamePhase.SelectNextCombat:
                    fallbackAction = new SelectNextCombatAction();
                    bool selectedCombat = false;
                    foreach (var combatOption in GameState.CommittedCombats)
                    {
                        if (combatOption.IsOptional) continue;
                        fallbackAction.SetAllParameters(combatOption);
                        selectedCombat = true;
                        break;
                    }

                    if (!selectedCombat)
                    {
                        fallbackAction.SetAllParameters(null);
                    }
                    break;
                
                default: throw new NotImplementedException();
            }
        }

        public override (bool, string) TestParameter(params object[] parameter)
        {
            throw new System.NotSupportedException();
        }

        public override void AddParameter(params object[] parameter)
        {
            throw new System.NotSupportedException();
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            throw new System.NotSupportedException();
        }

        public override void SetAllParameters(params object[] parameters)
        {
            throw new System.NotSupportedException();
        }

        public override object[] GetParameters()
        {
            throw new System.NotSupportedException();
        }

        public override object[] GetData()
        {
            throw new System.NotSupportedException();
        }

        public override (bool, string) Validate()
        {
            return (true, null);
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            // Nothing to read
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            // Nothing to write
        }

        public override void Reset()
        {
            
        }
    }
}