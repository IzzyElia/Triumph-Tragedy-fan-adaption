using System;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    
    public class GameCountry : GameEntity
    {
        public string InternalName; // The name used for etc finding game resources
        public string DisplayName => InternalName; // The name actually shown to the player
        public int iCapital;
        public GameTile Capital => iCapital == -1 ? null : GameState.GetEntity<GameTile>(iCapital);
        public ProjectedMembershipStatus FactionProjection = default;
        public MapCountry MapCountry
        {
            get
            {
                if (GameState.IsServer) throw new InvalidOperationException("Map rendering objects can only be accessed on the client side");
                return GameState.MapRenderer.MapCountriesByID[ID];
            }
        } 
        public int[] influencePlayedByPlayer = new int[0];
        public int AppliedInfluence = 0; // -1 for initial member

        public FactionMembershipStatus MembershipStatus
        {
            get
            {
                return TTUtilityFunctions.InfluenceToMembershipStatus(AppliedInfluence);
            }
            set
            {
                AppliedInfluence = TTUtilityFunctions.MembershipStatusToInfluence(value);
            }
        }
        public int iFaction = -1;

        public GameFaction AssociatedFaction
        {
            get
            {
                if (iColonialOverlord != -1)
                {
                    return ColonialOverlord.AssociatedFaction;
                }
                else if (iFaction >= 0)
                    return ((TTGameState)GameState).GetEntity<GameFaction>(iFaction);
                else return null; // neutral
            }
        }
        public GameFaction Faction
        {
            get
            {
                if (iColonialOverlord != -1)
                {
                    return ColonialOverlord.Faction;
                }
                else if (iFaction >= 0 && TTUtilityFunctions.IsFullMember(MembershipStatus))
                    return ((TTGameState)GameState).GetEntity<GameFaction>(iFaction);
                else return null; // neutral
            }
        }
        
        public int iColonialOverlord = -1;

        public GameCountry ColonialOverlord
        {
            get
            {
                if (iColonialOverlord >= 0)
                    return ((TTGameState)GameState).GetEntity<GameCountry>(iColonialOverlord);
                else
                    return null;
            }
        }
        public bool IsColony => iColonialOverlord != -1;
        public bool IsNeutral => Faction == null && !IsColony;

        public int CalculateLargestCitySize()
        {
            int largestCitySize = 0;
            foreach (var gameTile in GameState.GetEntitiesOfType<GameTile>())
            {
                if (gameTile.Country == this)
                {
                    largestCitySize = Mathf.Max(largestCitySize, gameTile.CitySize);
                }
            }

            return largestCitySize;
        }
        
        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new System.NotImplementedException();
        }

        
        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            InternalName = incomingMessage.ReadFixedString64().ToString();
            iCapital = incomingMessage.ReadInt();
            iFaction = incomingMessage.ReadInt();
            iColonialOverlord = incomingMessage.ReadInt();
            AppliedInfluence = incomingMessage.ReadShort();
            
            int influencePlayedByPlayerLength = (int)incomingMessage.ReadByte();
            influencePlayedByPlayer = new int[influencePlayedByPlayerLength];
            for (int i = 0; i < influencePlayedByPlayerLength; i++)
            {
                influencePlayedByPlayer[i] = incomingMessage.ReadShort();
            }
            if (iFaction != -1 && iColonialOverlord != -1) Debug.LogError($"{MapCountry.name} has both a colonial overlord {MapCountry.colonialOverlord.name} and belongs to a faction {MapCountry.associatedFaction.name}. This should not happen");
            ((TTGameState)GameState).needsProjectionUpdate = true;
            if (GameState.NetworkMember.GameStarted && GameState.IsSynced) RefreshMapState();
        }

        public override void RefreshMapState()
        {
            if (((TTGameState)GameState).needsProjectionUpdate) ((TTGameState)GameState).CalculateProjectedMembershipStatus();
            if (GameState.IsServer) return;
            if (MapRenderer is null) return;
            GameState.NetworkMember.NetworkingLog($"Applying Country State for {MapCountry.name}", DebuggingLevel.IndividualMessages);
            if (MapCountry is null) throw new InvalidOperationException("No map country corresponding to game country with id {ID}");
            MapCountry.InternalName = InternalName;
            MapCountry.DisplayName = DisplayName;
            MapCountry.SetFaction(FactionProjection.iFaction, FactionProjection.MembershipStatus, recalculateTileAppearance:false);
            MapCountry.SetColonialOverlord(iColonialOverlord);
            MapCountry.RecalculateTileAppearance();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            if (InternalName == null) InternalName = "unnamed_country";
            outgoingMessage.WriteFixedString64(InternalName);
            outgoingMessage.WriteInt(iCapital);
            outgoingMessage.WriteInt(iFaction);
            outgoingMessage.WriteInt(iColonialOverlord);
            outgoingMessage.WriteShort((short)AppliedInfluence);
            outgoingMessage.WriteByte((byte)influencePlayedByPlayer.Length);
            for (int i = 0; i < influencePlayedByPlayer.Length; i++)
            {
                outgoingMessage.WriteShort((short)influencePlayedByPlayer[i]);
            }
        }

        public override int HashFullState(int asPlayer)
        {
            return Hashing.MurmurHash3_Combine(iCapital, iFaction, iColonialOverlord);
        }

        protected override void Init()
        {
            base.Init();
            if (GameState.PlayerCount >= 0) influencePlayedByPlayer = new int[GameState.PlayerCount];
        }

        public override void OnPlayerCountChanged(int value)
        {
            base.OnPlayerCountChanged(value);
            int[] updatedInfluencePlayedByPlayer = new int[GameState.PlayerCount];
            for (int i = 0; i < Math.Min(influencePlayedByPlayer.Length, GameState.PlayerCount); i++)
            {
                updatedInfluencePlayedByPlayer[i] = influencePlayedByPlayer[i];
            }
            influencePlayedByPlayer = updatedInfluencePlayedByPlayer;
            if (GameState.NetworkMember.GameStarted) RefreshMapState();
        }
    }
}