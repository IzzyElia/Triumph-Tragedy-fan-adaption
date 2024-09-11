using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [RegisterEntityAsBaseType]
    public class InvestmentCard : GameCard, IInvestmentCard
    {
        static InvestmentCard()
        {
            RegisterCardType<InvestmentCard>();
        }

        public void SetTechs(params Tech[] techs)
        {
            Techs.Clear();
            for (int i = 0; i < techs.Length; i++)
            {
                Techs.Add(techs[i].ID);
            }
        }

        public List<int> Techs { get; set; } = new List<int>();
        public int FactoryValue { get; set; }

        protected override void Init()
        {
            base.Init();
            CardType = CardType.Investment;
        }
        
        protected override void WriteCardDetails(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteByte((byte)FactoryValue);
            outgoingMessage.WriteByte((byte)Techs.Count);
            for (int i = 0; i < Techs.Count; i++)
            {
                outgoingMessage.WriteUShort((ushort)Techs[i]);
            }
        }

        protected override void ReadCardDetails(ref DataStreamReader incomingMessage)
        {
            FactoryValue = incomingMessage.ReadByte();
            byte techsLength = incomingMessage.ReadByte();
            Techs.Clear();
            for (int i = 0; i < techsLength; i++)
            {
                Techs.Add(incomingMessage.ReadUShort());
            }
        }

        public override int HashFullState(int asPlayer)
        {
            if (IsVisibleToPlayer(asPlayer))
            {
                int hash = Hashing.MurmurHash3_Combine(base.HashFullState(asPlayer), FactoryValue);
                unchecked
                {
                    for (int i = 0; i < Techs.Count; i++)
                    {
                        hash = Hashing.CombineHashes(hash, Hashing.MurmurHash3(Techs[i]));
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

        public override void RefreshMapState() { }
    }
}