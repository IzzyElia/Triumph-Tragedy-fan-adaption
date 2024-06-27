using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using Izzy;
using Izzy.ForcedInitialization;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class BuildInitialUnits : PlayerAction
    {
        static BuildInitialUnits()
        {
            PlayerAction.RegisterPlayerActionType<BuildInitialUnits>();
        }

        private (int iTile, byte unitType, int iCountry, byte pipsToAdd)[] _builds = Array.Empty<(int, byte, int, byte)>();

        public BuildInitialUnits() {}

        public override void Execute()
        {
            foreach ((int iTile, byte unitType, int iCountry, byte pips) in _builds)
            {
                GameTile tile = GameState.GetEntity<GameTile>(iTile);
                GameCadre cadre = GameCadre.CreateCadre(GameState, unitType, iCountry, iTile);
                cadre.RecalculateDerivedValuesAndPushFullState();
            }

            GameState.PlayerCommitted[iPlayerFaction] = true;
            if (GameState.AllPlayersAreCommitted)
            {
                GameState.Year--; // to counteract the year-increment in StartNewYear()
                GameState.StartNewYear();
            }
            
            GameState.PushGlobalFields();
        }

        private static Dictionary<(int, int), int> _cadresPerTile = new();
        private static Dictionary<(int, int), int> expectedCadresPerTile = new();
        public (bool, string) AreBuildsValid((int iTile, byte unitType, int iCountry, byte pipsToAdd)[] buildsToTest)
        {
            if (GameState.GamePhase != GamePhase.InitialPlacement) return (false, "Not in initial placement phase");
            if (!GameState.IsWaitingOnPlayer(iPlayerFaction)) return (false, "Initial placement already submitted");
            
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            if (playerFaction == null)
                return (false, "Nonexistent player faction attempting action");

            _cadresPerTile.Clear();
            expectedCadresPerTile.Clear();
            foreach ((int iTile, int iCountry, int startingCadres) in playerFaction.startingUnits)
            {
                expectedCadresPerTile.Add((iTile, iCountry), startingCadres);
            }

            foreach ((int iTile, int iCountry) in expectedCadresPerTile.Keys)
            {
                _cadresPerTile.Add((iTile, iCountry), 0);
            }
            foreach ((int iTile, byte iUnitType, int iCountry, byte pips) in buildsToTest)
            {
                if (iUnitType < 0 || iUnitType >= GameState.Ruleset.unitTypes.Length) return (false, "Not all units placed");
                UnitType unitType = GameState.Ruleset.unitTypes[iUnitType];
                if (pips != 1) return (false, "Starting cadres may only be one pip strong");
                if (iTile < 0 || iTile >= GameState.MaxEntityID<GameTile>()) return (false, "Invalid tile id");
                if (!unitType.IsBuildableThroughNormalPlacementRules) return (false, "Unit type cannot be placed manually");
                if (!expectedCadresPerTile.ContainsKey((iTile, iCountry))) return (false, "No starting units go on tile");
                _cadresPerTile[(iTile, iCountry)]++;
                if (_cadresPerTile[(iTile, iCountry)] > expectedCadresPerTile[(iTile, iCountry)])
                    return (false, "Too many cadres on tile");
            }
            
            return (true, "");
        }

        /// <param name="parameters">
        /// Parameter format is a cadres movement and paths - (int iCadre, int[] path)
        /// </param>
        public override (bool, string) TestParameter(params object[] parameters)
        {
            throw new NotImplementedException();
        }
        
        public override void AddParameter(params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            throw new NotImplementedException();
        }

        /// <param name="parameters">
        /// Parameters format is an array of units, each entry declaring the tile, unit type, country, and starting pips - (int iTile, byte unitType, int iCountry, byte pipsToAdd)[]
        /// </param>
        public override void SetAllParameters(params object[] parameters)
        {
            this._builds = parameters[0] as (int iTile, byte unitType, int iCountry, byte pipsToAdd)[];
        }
        
        public override object[] GetParameters()
        {
            return new object[] { _builds };
        }

        public override object[] GetData()
        {
            throw new NotImplementedException();
        }

        public override (bool, string) Validate() => AreBuildsValid(_builds);
        
        public override void Reset()
        {
            _builds = Array.Empty<(int, byte, int, byte)>();
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            int length = incomingMessage.ReadInt();
            this._builds = new (int, byte, int, byte)[length];
    
            for (int i = 0; i < length; i++)
            {
                int iTile = incomingMessage.ReadInt();
                byte unitType = incomingMessage.ReadByte();
                int iCountry = incomingMessage.ReadInt();
                byte pipsToAdd = incomingMessage.ReadByte();
        
                _builds[i] = (iTile, unitType, iCountry, pipsToAdd);
            }
        }
        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(_builds.Length);
            for (int i = 0; i < _builds.Length; i++)
            {
                outgoingMessage.WriteInt(_builds[i].iTile);
                outgoingMessage.WriteByte(_builds[i].unitType);
                outgoingMessage.WriteInt(_builds[i].iCountry);
                outgoingMessage.WriteByte(_builds[i].pipsToAdd);
            }
        }
    }
}