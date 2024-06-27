using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using Izzy;
using Izzy.ForcedInitialization;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class MoveUnits : PlayerAction
    {
        static MoveUnits()
        {
            PlayerAction.RegisterPlayerActionType<MoveUnits>();
        }

        private List<MovementActionData> _moves = new List<MovementActionData>();

        public MoveUnits() {}

        public override void Execute()
        {
            for (int i = 0; i < _moves.Count; i++)
            {
                MovementActionData movementAction = _moves[i];
                GameCadre cadre = GameState.GetEntity<GameCadre>(movementAction.iCadre);
                cadre.PushMove(new int[] {movementAction.iDestination});
            }

            GameState.GamePhase = GamePhase.SelectCombat;
            GameState.PushGlobalFields();
        }

        public (bool, string) AreMovesValid(List<MovementActionData> movesToTest)
        {
            if (GameState.GamePhase != GamePhase.GiveCommands) return (false, "Not in command phase");
            if (GameState.ActivePlayer != iPlayerFaction) return (false, "Not the active player)");
            
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            if (playerFaction == null)
                return (false, "Nonexistent player faction attempting action");
            if (movesToTest.Count > playerFaction.CommandsAvailable) return (false, "Not enough commands available");

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