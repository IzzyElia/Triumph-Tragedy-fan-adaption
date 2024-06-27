using System;
using GameBoard;
using GameLogic;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    
    public class GameCountry : GameEntity
    {
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
                switch (AppliedInfluence)
                {
                    case 0: return FactionMembershipStatus.Unaligned;
                    case 1: return FactionMembershipStatus.Associate;
                    case 2: return FactionMembershipStatus.Protectorate;
                    case >= 3: return FactionMembershipStatus.Ally;
                    case -1: return FactionMembershipStatus.InitialMember;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (value)
                {
                    case FactionMembershipStatus.Unaligned:
                        AppliedInfluence = 0;
                        break;
                    case FactionMembershipStatus.Associate:
                        AppliedInfluence = 1;
                        break;
                    case FactionMembershipStatus.Protectorate:
                        AppliedInfluence = 2;
                        break;
                    case FactionMembershipStatus.Ally:
                        AppliedInfluence = 3;
                        break;
                    case FactionMembershipStatus.InitialMember:
                        AppliedInfluence = -1;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public int iFaction = -1;
        public GameFaction Faction
        {
            get
            {
                if (iColonialOverlord != -1)
                {
                    return ColonialOverlord.Faction;
                }
                else if (iFaction >= 0)
                    return ((TTGameState)GameState).GetEntity<GameFaction>(iFaction);
                else return null;
            }
        }
        
        public int iColonialOverlord = -1;

        public GameCountry ColonialOverlord
        {
            get
            {
                if (iColonialOverlord >= 0)
                    return ((TTGameState)GameState).GetOrCreateEntity<GameCountry>(iColonialOverlord);
                else
                    return null;
            }
        }

        public bool IsColony => iColonialOverlord != -1;
        
        public void ApplyToMap()
        {
            if (GameState.IsServer) return;
            if (MapRenderer is null) return;
            GameState.NetworkMember.NetworkingLog($"Applying Country State for {MapCountry.name}", DebuggingLevel.IndividualMessages);
            if (MapCountry is null) throw new InvalidOperationException("No map country corresponding to game country with id {ID}");
            MapCountry.SetFaction(iFaction, MembershipStatus);
            MapCountry.SetColonialOverlord(iColonialOverlord);
        }
        
        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new System.NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            iFaction = incomingMessage.ReadInt();
            iColonialOverlord = incomingMessage.ReadInt();
            AppliedInfluence = incomingMessage.ReadShort();
            int influencePlayedByPlayerLength = (int)incomingMessage.ReadByte();
            influencePlayedByPlayer = new int[influencePlayedByPlayerLength];
            for (int i = 0; i < influencePlayedByPlayerLength; i++)
            {
                influencePlayedByPlayer[i] = incomingMessage.ReadShort();
            }
            if (iFaction != -1 && iColonialOverlord != -1) Debug.LogError($"{MapCountry.name} has both a colonial overlord {MapCountry.colonialOverlord.name} and belongs to a faction {MapCountry.faction.name}. This should not happen");
            ApplyToMap();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
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
            return HashCode.Combine(iFaction, iColonialOverlord);
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
            ApplyToMap();
        }
    }
}