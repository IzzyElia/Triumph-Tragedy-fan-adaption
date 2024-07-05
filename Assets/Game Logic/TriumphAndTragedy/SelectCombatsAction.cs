using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy.ForcedInitialization;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class SelectCombatsAction : PlayerAction
    {
        static SelectCombatsAction()
        {
            RegisterPlayerActionType<SelectCombatsAction>();
        }

        private HashSet<CombatOption> _selections = new HashSet<CombatOption>();
        
        public override void Execute()
        {
            // Commit Combats
            GameState.CommittedCombats.Clear();
            foreach (var combatOption in _selections)
            {
                GameState.CommittedCombats.Add(combatOption);
            }
            
            // Calculate players who can support in combats
            GameState.ResetPlayerStatuses(push:false);
            for (int i = 0; i < GameState.PlayerCommitted.Length; i++)
            {
                GameState.PlayerCommitted[i] = true;
            }
            foreach (var cadre in GameState.GetEntitiesOfType<GameCadre>())
            {
                if (cadre == null || !cadre.Active) continue;
                if (cadre.Faction.ID > GameState.PlayerCount) continue; // Is presumably an unplayed/neutral faction
                if (GameState.PlayerCommitted[cadre.Faction.ID] == false) continue;
                if (cadre.Faction.ID == GameState.ActivePlayer) continue;
                int[] iTiles = GameState.CalculateAccessibleTiles(cadre.ID, MoveType.Support);
                foreach (var combat in GameState.CommittedCombats)
                {
                    if (combat.iDefender != cadre.Faction.ID) continue;
                    if (iTiles.Contains(combat.iTile))
                    {
                        GameState.PlayerCommitted[cadre.Faction.ID] = false;
                    }
                }
            }
            for (int i = 0; i < GameState.PlayerCommitted.Length; i++)
            {
                Debug.Log($"{i} committed: {GameState.PlayerCommitted[i]}");
            }

            // Set the game phase and push
            GameState.GamePhase = GamePhase.SelectSupport;
            GameState.AdvanceToCombatIfAllPlayersDoneWithSupport(push:false);
            GameState.PushGlobalFields();
        }

        public override (bool, string) TestParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override void AddParameter(params object[] parameter)
        {
            _selections.Add((CombatOption)parameter[0]);
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            return _selections.Remove((CombatOption)parameter[0]);
        }

        public override void SetAllParameters(params object[] parameters)
        {
            _selections.Clear();
            for (int i = 0; i < parameters.Length; i++)
            {
                _selections.Add((CombatOption)parameters[0]);
            }
        }

        public override object[] GetParameters()
        {
            CombatOption[] results = new CombatOption[_selections.Count];
            int i = 0;
            foreach (CombatOption selectedTile in _selections)
            {
                results[i] = selectedTile;
                i++;
            }

            return new object[] { results };
        }

        public override object[] GetData()
        {
            throw new System.NotImplementedException();
        }

        HashSet<int> c_tilesSelected = new HashSet<int>();
        public override (bool, string) Validate()
        {
            if (GameState.GamePhase != GamePhase.CommitCombats) return (false, "Not in commit combats phase");
            lock (c_tilesSelected)
            {
                lock (_selections)
                {
                    c_tilesSelected.Clear();
                    foreach (CombatOption combatSelection in _selections)
                    {
                        if (c_tilesSelected.Contains(combatSelection.iTile))
                            return (false, "Same tile selected multiple times");
                        c_tilesSelected.Add(combatSelection.iTile);
                        GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
                        GameFaction defenderFaction = GameState.GetEntity<GameFaction>(combatSelection.iDefender);
                        GameTile tile = GameState.GetEntity<GameTile>(combatSelection.iTile);
                        if (tile is null) return (false, "Invalid tile id");
                        if (defenderFaction is null) return (false, "Invalid defender faction id");
                        bool attackerUnitPresent = false;
                        bool defenderUnitPresent = false;
                        foreach (var cadre in GameState.GetEntitiesOfType<GameCadre>())
                        {
                            if (cadre.Faction == playerFaction) attackerUnitPresent = true;
                            else if (cadre.Faction == defenderFaction) defenderUnitPresent = true;
                            if (attackerUnitPresent && defenderUnitPresent) break;
                        }

                        if (!attackerUnitPresent) return (false, "You have no units present");
                        if (!defenderUnitPresent) return (false, "There are no units defending");
                    }

                    return (true, null);
                }
            }
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            _selections.Clear();
            int numSelections = incomingMessage.ReadUShort();
            for (int i = 0; i < numSelections; i++)
            {
                CombatOption combatSelection = CombatOption.Recreate(ref incomingMessage);
                _selections.Add(combatSelection);
            }
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteUShort((ushort)_selections.Count);
            foreach (var combatSelection in _selections)
            {
                combatSelection.Write(ref outgoingMessage);
            }
        }

        public override void Reset()
        {
            _selections.Clear();
        }
    }
}