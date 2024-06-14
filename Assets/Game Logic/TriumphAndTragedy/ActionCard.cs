using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [RegisterEntityAsBaseType]
    public class ActionCard : GameCard, IActionCard
    {
        static ActionCard()
        {
            RegisterCardType<ActionCard>();
        }
        
        private List<int> _countries = new List<int>();
        public IReadOnlyList<int> Countries => _countries;
        public int Initiative { get; set; }
        public int NumActions { get; set; }

        protected override void Init()
        {
            base.Init();
            CardType = CardType.Action;
        }

        public void SetCountries(params int[] countries)
        {
            _countries.Clear();
            for (int i = 0; i < countries.Length; i++)
            {
                _countries.Add(countries[i]);
            }
        }
        
        protected override void WriteCardDetails(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteByte((byte)Initiative);
            outgoingMessage.WriteByte((byte)NumActions);

            outgoingMessage.WriteByte((byte)Countries.Count);
            for (int i = 0; i < Countries.Count; i++)
            {
                outgoingMessage.WriteShort((short)Countries[i]);
            }
            
        }

        protected override void ReadCardDetails(ref DataStreamReader incomingMessage)
        {
            Initiative = incomingMessage.ReadByte();
            NumActions = incomingMessage.ReadByte();
            byte countriesCount = incomingMessage.ReadByte();
            _countries.Clear();
            for (int i = 0; i < countriesCount; i++)
            {
                _countries[i] = incomingMessage.ReadShort();
            }
        }

        public override int HashFullState(int asPlayer)
        {
            if (IsVisibleToPlayer(asPlayer))
            {
                int hash = HashCode.Combine(base.HashFullState(asPlayer), Initiative, NumActions);
                unchecked
                {
                    for (int i = 0; i < _countries.Count; i++)
                    {
                        hash *= _countries[i].GetHashCode();
                    }
                }

                return hash;
            }
            else
            {
                int hash = base.HashFullState(asPlayer);
                return hash;
            }
        }


        // Utility functions


    }
}