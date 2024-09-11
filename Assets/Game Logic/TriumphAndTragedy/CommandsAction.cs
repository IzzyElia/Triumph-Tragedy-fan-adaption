using System;
using System.Collections.Generic;
using GameLogic;
using GameSharedInterfaces;
using Izzy.ForcedInitialization;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class CommandsAction : PlayerAction
    {
        static CommandsAction()
        {
            PlayerAction.RegisterPlayerActionType<CommandsAction>();
        }

        private List<MovementActionData> _moves = new List<MovementActionData>();

        public CommandsAction() {}

        public override void Execute()
        {
            _diplomaticActions.Clear();
            _movementActions.Clear();
            foreach (var movementAction in _moves)
            {
                if (movementAction.IsDiploAction) _diplomaticActions.Add(movementAction);
                else _movementActions.Add(movementAction);
            }

            GameFaction PlayerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            for (int i = 0; i < _diplomaticActions.Count; i++)
            {
                MovementActionData diplomaticAction = _diplomaticActions[i];
                if (diplomaticAction.iCadreOriFactionWarTarget != -1)
                {
                    PlayerFaction.DeclareWarOnFaction(diplomaticAction.iCadreOriFactionWarTarget);
                }
                else if (diplomaticAction.iDestinationOriCountryWarTarget != -1)
                {
                    PlayerFaction.DeclareWarOnCountry(diplomaticAction.iDestinationOriCountryWarTarget);
                }
            }
            for (int i = 0; i < _movementActions.Count; i++)
            {
                MovementActionData movementAction = _movementActions[i];
                GameCadre cadre = GameState.GetEntity<GameCadre>(movementAction.iCadreOriFactionWarTarget);
                cadre.PushMove(new int[] {movementAction.iDestinationOriCountryWarTarget});
            }

            
            GameState.EvaluateTerritoryControl();
            GameState.GamePhase = GamePhase.CommitCombats;
            if (GameState.GetCombatOptions().Length == 0)
            {
                GameState.AdvanceCommandingPhasingPlayer();
            }
            else
            {
                GameState.PushGlobalFields();
            }
        }

        private List<MovementActionData> _diplomaticActions = new List<MovementActionData>();
        private List<MovementActionData> _movementActions = new List<MovementActionData>();
        public (bool, string) AreMovesValid(List<MovementActionData> movesToTest)
        {
            _diplomaticActions.Clear();
            _movementActions.Clear();

            foreach (var movementAction in movesToTest)
            {
                if (movementAction.IsDiploAction) _diplomaticActions.Add(movementAction);
                else _movementActions.Add(movementAction);
            }
            
            if (GameState.GamePhase != GamePhase.GiveCommands) return (false, "Not in command phase");
            if (GameState.ActivePlayer != iPlayerFaction) return (false, "Not the active player)");
            
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            if (playerFaction == null)
                return (false, "Nonexistent player faction attempting action");
            if (_movementActions.Count > playerFaction.CommandsAvailable) return (false, "Not enough commands available");
            
            // TODO move validation

            return (true, string.Empty);
        }


        private List<MovementActionData> _testingMovementActions = new List<MovementActionData>();
        public override (bool, string) TestParameter(params object[] parameters)
        {
            _testingMovementActions = new List<MovementActionData>(_moves);
            for (int i = 0; i < parameters.Length; i++)
            {
                try
                {
                    _testingMovementActions.Add((MovementActionData)parameters[i]);
                }
                catch (InvalidCastException e)
                {
                    Debug.LogError("Invalid data type passed to action");
                }
            }

            return AreMovesValid(_testingMovementActions);
        }

        /// <param name="parameters">
        /// Parameter format is a cadres and its movement path - (int iCadre, int[] path)[]
        /// </param>
        public override void AddParameter(params object[] parameters)
        {
            _moves.Add((MovementActionData)parameters[0]);
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            return _moves.Remove((MovementActionData)parameter[0]);
        }

        /// <param name="parameters">
        /// Parameters format is an array of cadres and movement paths - (int iCadre, int[] addedMove)[]
        /// </param>
        public override void SetAllParameters(params object[] parameters)
        {
            throw new NotImplementedException();
        }
        
        public override object[] GetParameters()
        {
            object[] output = new object[_moves.Count];
            for (int i = 0; i < _moves.Count; i++)
            {
                output[i] = _moves[i];
            }

            return output;
        }

        public override object[] GetData()
        {
            return new object[] { _moves.Count };
        }

        public override (bool, string) Validate() => AreMovesValid(this._moves);
        
        public override void Reset()
        {
            _moves.Clear();
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            _moves.Clear();
            int length = incomingMessage.ReadInt();
            for (int i = 0; i < length; i++)
            {
                _moves.Add(MovementActionData.Recreate(ref incomingMessage));
            } 
        }
        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(_moves.Count);
            for (int i = 0; i < _moves.Count; i++)
            {
                _moves[i].Write(ref outgoingMessage);
            }
        }
    }
}