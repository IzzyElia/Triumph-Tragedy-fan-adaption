using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic;
using GameSharedInterfaces;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Remember to apply <see cref="RegisterEntityAsBaseTypeAttribute"/> to derived types if you want all cards to share the same id system</remarks>
    public abstract class GameCard : GameEntity, ICard
    {
        static readonly List<Type> _cardTypes = new ();

        public static void RegisterCardType<T>() where T : GameCard
        {
            _cardTypes.Add(typeof(T));
        }
        
        static readonly List<GameCard> _cardsReturned = new ();
        public static GameCard[] GetAllCards(GameState gameState)
        {
            _cardsReturned.Clear();
            foreach (var cardType in _cardTypes)
            {
                foreach (var gameEntity in gameState.GetEntitiesOfType(cardType))
                {
                    _cardsReturned.Add((GameCard)gameEntity);
                }
            }

            return _cardsReturned.ToArray();
        }
        
        public int HoldingPlayer { get; set; }
        public CardType CardType { get; set; }

        public bool IsVisibleToPlayer(int iPlayer)
        {
            if (iPlayer == HoldingPlayer) return true;
            // Todo add support for situations where other players can see your hand (like Code Break)
            return false;
        }
        
        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new System.NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            bool isVisibleToMe = incomingMessage.ReadByte() == 1;
            if (isVisibleToMe)
            {
                HoldingPlayer = (int)incomingMessage.ReadByte();
                ReadCardDetails(ref incomingMessage);
            }
            else
            {
                HoldingPlayer = (int)incomingMessage.ReadByte();
            }
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            if (HoldingPlayer == targetPlayer)
            {
                outgoingMessage.WriteByte(1);
                outgoingMessage.WriteByte((byte)HoldingPlayer);
                WriteCardDetails(ref outgoingMessage);
            }
            else
            {
                outgoingMessage.WriteByte(0);
                outgoingMessage.WriteByte((byte)HoldingPlayer);
            }
        }

        public override int HashFullState(int asPlayer)
        {
            return HashCode.Combine(HoldingPlayer);
        }

        protected abstract void WriteCardDetails(ref DataStreamWriter outgoingMessage);
        protected abstract void ReadCardDetails(ref DataStreamReader incomingMessage);
    }
}