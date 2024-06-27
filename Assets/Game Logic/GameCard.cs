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
        private const byte _holdingPlayerUpdateHeader = 0;
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
                    if (gameEntity.Active) _cardsReturned.Add((GameCard)gameEntity);
                }
            }

            return _cardsReturned.ToArray();
        }

        public int HoldingPlayer { get; set; } = -1;
        public CardType CardType { get; set; }

        public bool IsVisibleToPlayer(int iPlayer)
        {
            if (iPlayer == HoldingPlayer) return true;
            // Todo add support for situations where other players can see your hand (like Code Break)
            return false;
        }
        public void SetHoldingPlayerAndPush(int holdingPlayer)
        {
            HoldingPlayer = holdingPlayer;
            for (int i = 0; i < GameState.PlayerCount; i++)
            {
                DataStreamWriter update = StartCustomUpdate(_holdingPlayerUpdateHeader, i);
                update.WriteByte((byte)holdingPlayer);
                PushCustomUpdate(i, ref update);
            }
        }
        
        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            switch (header)
            {
                case _holdingPlayerUpdateHeader:
                    HoldingPlayer = incomingMessage.ReadByte();
                    break;
                default: throw new NotImplementedException();
            }
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            bool isVisibleToMe = incomingMessage.ReadByte() == 1;
            if (isVisibleToMe)
            {
                HoldingPlayer = (int)incomingMessage.ReadShort();
                ReadCardDetails(ref incomingMessage);
            }
            else
            {
                HoldingPlayer = (int)incomingMessage.ReadShort();
            }
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            if (HoldingPlayer == targetPlayer)
            {
                outgoingMessage.WriteByte(1);
                outgoingMessage.WriteShort((short)HoldingPlayer);
                WriteCardDetails(ref outgoingMessage);
            }
            else
            {
                outgoingMessage.WriteByte(0);
                outgoingMessage.WriteShort((short)HoldingPlayer);
            }
        }

        public override int HashFullState(int asPlayer)
        {
            return HashCode.Combine(HoldingPlayer);
        }

        protected abstract void WriteCardDetails(ref DataStreamWriter outgoingMessage);
        protected abstract void ReadCardDetails(ref DataStreamReader incomingMessage);
        
        public static List<T> GetCardsInDeck <T>(GameState gameState) where T : GameCard
        {
            List<T> deck = new List<T>();
            T[] allCards = gameState.GetEntitiesOfType<T>();
            for (int i = 0; i < allCards.Length; i++)
            {
                T card = allCards[i];
                if (card != null && card.Active && card.HoldingPlayer == -1)
                {
                    deck.Add(card);
                }
            }
            return deck;
        }
    }
}