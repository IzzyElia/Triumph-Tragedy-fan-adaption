using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy.ForcedInitialization;
using Unity.Collections;
using NotImplementedException = System.NotImplementedException;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class SelectCombatSupportAction : PlayerAction
    {
        static SelectCombatSupportAction()
        {
            RegisterPlayerActionType<SelectCombatSupportAction>();
        }

        private Dictionary<int, int> _supportSelections = new Dictionary<int, int>();

        public override void Execute()
        {
            foreach ((int iCadre, int iTile) in _supportSelections)
            {
                GameState.CombatSupports.Add(iCadre, iTile);
            }

            GameState.PlayerCommitted[iPlayerFaction] = true;

            GameState.AdvanceToCombatIfAllPlayersDoneWithSupport(push: false);
            GameState.PushGlobalFields();
        }

        public override (bool, string) TestParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        /// <remarks>
        /// Will change the tile the cadre is supporting if it's already set
        /// </remarks>
        public override void AddParameter(params object[] parameter)
        {
            int iCadre = (int)parameter[0];
            int iTile = (int)parameter[1];
            if (!_supportSelections.TryAdd(iCadre, iTile))
            {
                _supportSelections[iCadre] = iTile;
            }
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            int iCadre = (int)parameter[0];
            return _supportSelections.Remove(iCadre);
        }

        public override void SetAllParameters(params object[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public override object[] GetParameters()
        {
            object[] supportSelections = new object[_supportSelections.Count];
            int i = 0;
            foreach ((int iCadre, int iTile) in _supportSelections)
            {
                supportSelections[i] = (iCadre, iTile);
                i++;
            }

            return supportSelections;
        }

        public override object[] GetData()
        {
            throw new System.NotImplementedException();
        }

        public override (bool, string) Validate()
        {
            if (GameState.PlayerCommitted[iPlayerFaction]) return (false, "Already committed support action");
            foreach ((int iCadre, int iTile) in _supportSelections)
            {
                if (GameState.GetEntity<GameCadre>(iCadre).Faction.ID != iPlayerFaction)
                    return (false, "Not all selected cadres belong to your faction");
                if (!GameState.CalculateAccessibleTiles(iCadre, MoveType.Support).Contains(iTile))
                    return (false, "Not all selected cadres can access the combats they are supporting");
            }

            return (true, null);
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            int combatSupportsLength = incomingMessage.ReadUShort();
            for (int i = 0; i < combatSupportsLength; i++)
            {
                int iCadre = incomingMessage.ReadUShort();
                int iTile = incomingMessage.ReadShort();
                _supportSelections.Add(iCadre, iTile);
            }
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteUShort((ushort)_supportSelections.Count);
            foreach ((int iCadre, int iTile) in _supportSelections)
            {
                outgoingMessage.WriteUShort((ushort)iCadre);
                outgoingMessage.WriteShort((short)iTile);
            }
        }

        public override void Reset()
        {
            _supportSelections.Clear();
        }
    }
}