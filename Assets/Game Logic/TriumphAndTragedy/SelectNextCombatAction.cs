using System;
using System.Linq;
using GameLogic;
using GameSharedInterfaces;
using Izzy.ForcedInitialization;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class SelectNextCombatAction : PlayerAction
    {
        static SelectNextCombatAction()
        {
            RegisterPlayerActionType<SelectNextCombatAction>();
        }

        private CombatOption? _selectedCombatOption = null;

        public override void Execute()
        {
            if (!_selectedCombatOption.HasValue) throw new InvalidOperationException();
            GameState.GamePhase = GamePhase.Combat;
            GameState.PushGlobalFields();
            
            CombatOption combatOption = _selectedCombatOption.Value;
            GameState.CommittedCombats.Remove(combatOption);
            GameCombat combat = GameCombat.CreateCombat(GameState, iTile:combatOption.iTile, combatOption.iAttacker, combatOption.iDefender);
            combat.StartCombat();
        }

        public override (bool, string) TestParameter(params object[] parameter)
        {
            return IsCombatOptionValid((CombatOption?)parameter[0]);
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
            _selectedCombatOption = (CombatOption?)parameters[0];
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
            return IsCombatOptionValid(_selectedCombatOption);
        }

        (bool, string) IsCombatOptionValid(CombatOption? combatOption)
        {
            if (!combatOption.HasValue) return (false, "No combat option selected");
            if (!GameState.CommittedCombats.Contains(combatOption.Value))
                return (false, "Selected combat option is not a a committed combat option");
            return (true, null);
        } 

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            bool isThereASelectedCombatOption = incomingMessage.ReadByte() == 1;
            if (isThereASelectedCombatOption)
            {
                _selectedCombatOption = CombatOption.Recreate(ref incomingMessage);
            }
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteByte((byte)(_selectedCombatOption.HasValue ? 1 : 0));
            if (_selectedCombatOption.HasValue)
            {
                _selectedCombatOption.Value.Write(ref outgoingMessage);
            }
        }

        public override void Reset()
        {
            _selectedCombatOption = null;
        }
    }
}