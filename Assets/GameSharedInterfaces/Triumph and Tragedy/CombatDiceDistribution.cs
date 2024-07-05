using Unity.Collections;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{

    public struct CombatDiceDistribution
    {
        public ushort GroundDice;
        public ushort AirDice;
        public ushort SeaDice;
        public ushort SubDice;

        public static CombatDiceDistribution Recreate(ref DataStreamReader message)
        {
            return new CombatDiceDistribution
            {
                GroundDice = message.ReadUShort(),
                AirDice = message.ReadUShort(),
                SeaDice = message.ReadUShort(),
                SubDice = message.ReadUShort()
            };
        }

        public void Write(ref DataStreamWriter message)
        {
            message.WriteUShort(GroundDice);
            message.WriteUShort(AirDice);
            message.WriteUShort(SeaDice);
            message.WriteUShort(SubDice);
        }
    }
}