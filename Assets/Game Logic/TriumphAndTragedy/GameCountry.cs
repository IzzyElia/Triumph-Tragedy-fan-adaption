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
        public Color Color => MapCountry.color;
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
                    return ((TTGameState)GameState).GetOrCreateEntity<GameFaction>(iFaction);
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
        
        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new System.NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            iFaction = incomingMessage.ReadInt();
            iColonialOverlord = incomingMessage.ReadInt();
            MapCountry.SetFaction(iFaction);
            MapCountry.SetColonialOverlord(iColonialOverlord);
            if (iFaction != -1 && iColonialOverlord != -1) Debug.LogError($"{MapCountry.name} has both a colonial overlord {MapCountry.colonialOverlord.name} and belongs to a faction {MapCountry.faction.name}. This should not happen");
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(iFaction);
            outgoingMessage.WriteInt(iColonialOverlord);
        }

        public override int HashFullState(int asPlayer)
        {
            return HashCode.Combine(iFaction, iColonialOverlord);
        }
    }
}