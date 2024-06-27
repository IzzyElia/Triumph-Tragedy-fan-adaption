using System;
using Unity.Collections;

namespace GameSharedInterfaces
{
    public enum CardEffectTargetSelectionType
    {
        None,
        Faction,
        Country,
        Tile,
        Tech,
        Global,
    }

    public enum CardPlayType
    {
        None,
        Diplomacy,
        Insurgents,
        Command,
        Tech,
        Industry,
        Pass,
    }
    public struct CardplayInfo
    {
        public CardPlayType CardPlayType;
        public CardEffectTargetSelectionType TargetType;
        public int iTarget;
        private int[] _iCardsUsed;
        public int[] iCardsUsed => _iCardsUsed ?? new int[0];
        

        public CardplayInfo (CardEffectTargetSelectionType targetType, CardPlayType cardPlayType, int iTarget, int[] iCardsUsed)
        {
            if (iCardsUsed == null) iCardsUsed = new int[0];
            this.TargetType = targetType;
            this.CardPlayType = cardPlayType;
            this.iTarget = iTarget;
            this._iCardsUsed = iCardsUsed;
        }
        
        public static CardplayInfo Recreate(ref DataStreamReader incomingMessage)
        {
            CardPlayType playType = (CardPlayType)incomingMessage.ReadByte();
            CardEffectTargetSelectionType targetType = (CardEffectTargetSelectionType)incomingMessage.ReadByte();
            int target = incomingMessage.ReadInt();
            int cardsLength = incomingMessage.ReadInt();
            int[] cardsUsed = new int[cardsLength];
            for (int i = 0; i < cardsLength; i++)
            {
                cardsUsed[i] = incomingMessage.ReadInt();
            }
            return new CardplayInfo(targetType, playType, target, cardsUsed);
        }

        public void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteByte((byte)CardPlayType);
            outgoingMessage.WriteByte((byte)TargetType);
            outgoingMessage.WriteInt(iTarget);
            outgoingMessage.WriteInt(iCardsUsed.Length);
            for (int i = 0; i < iCardsUsed.Length; i++)
            {
                outgoingMessage.WriteInt(iCardsUsed[i]);
            }
        }
    }
}