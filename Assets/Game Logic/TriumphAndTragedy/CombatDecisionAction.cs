using System;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy.ForcedInitialization;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class CombatDecisionAction : PlayerAction
    {
        static CombatDecisionAction ()
        {
            RegisterPlayerActionType<CombatDecisionAction>();
        }

        private CombatDiceDistribution _diceDistribution;

        public override void Execute()
        {
            Debug.Log("Attempting combat action...");
            GameState.ActiveCombat.RollCombatAtCurrentInitiative(_diceDistribution);
        }

        public override (bool, string) TestParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override void AddParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override void SetAllParameters(params object[] parameters)
        {
            _diceDistribution = (CombatDiceDistribution)parameters[0];
        }

        public override object[] GetParameters()
        {
            throw new System.NotImplementedException();
        }

        public override object[] GetData()
        {
            throw new System.NotImplementedException();
        }

        public override (bool, string) Validate()
        {
            if (GameState.ActiveCombat == null)
            {
                return (false, "No combat ongoing");
            }

            // If the action was submitted by a combat panel controlled by another local client then it is still valid
            if (GameState.ActiveCombat.iPhasingPlayer != iPlayerFaction)
            {
                return (false, "You are not the phasing player in combat");
            }

            IGameCadre[] cadres = GameState.ActiveCombat.CalculateInvolvedCadreInterfaces();
            bool validatedAir = _diceDistribution.AirDice <= 0;
            bool validatedGround = _diceDistribution.GroundDice <= 0;
            bool validatedSea = _diceDistribution.SeaDice <= 0;
            bool validatedSub = _diceDistribution.SubDice <= 0;
            if (_diceDistribution.TotalDice < GameState.ActiveCombat.numDiceAvailable)
                return (false, "Not enough dice assigned");
            else if (_diceDistribution.TotalDice > GameState.ActiveCombat.numDiceAvailable)
                return (false, "Too many dice assigned");
            foreach (var cadre in cadres)
            {
                if (cadre.IFaction.ID == iPlayerFaction)
                {
                    // TODO validate dice counts match unit counts
                }
                else 
                {
                    switch (cadre.UnitType.Category)
                    {
                        case UnitCategory.Air:
                            validatedAir = true;
                            break;
                        case UnitCategory.Ground:
                            validatedGround = true;
                            break;
                        case UnitCategory.Sea:
                            validatedSea = true;
                            break;
                        case UnitCategory.Sub:
                            validatedSub = true;
                            break;
                        default: throw new NotImplementedException();
                    }
                }
            }
            if (!(validatedAir && validatedGround && validatedSea && validatedSub))
                return (false, "Some dice are set to target unit types which are not present");
            return (true, null);
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            _diceDistribution = CombatDiceDistribution.Recreate(ref incomingMessage);
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            _diceDistribution.Write(ref outgoingMessage);
        }

        public override void Reset()
        {
            _diceDistribution = default;
        }
    }
}