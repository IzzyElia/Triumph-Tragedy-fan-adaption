using Unity.Collections;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{

    public struct CombatDiceDistribution
    {
        public short GroundDice;
        public short AirDice;
        public short SeaDice;
        public short SubDice;
        public int TotalDice => AirDice + GroundDice + SeaDice + SubDice;
        
        public static CombatDiceDistribution Recreate(ref DataStreamReader message)
        {
            return new CombatDiceDistribution
            {
                GroundDice = message.ReadShort(),
                AirDice = message.ReadShort(),
                SeaDice = message.ReadShort(),
                SubDice = message.ReadShort()
            };
        }

        public void Write(ref DataStreamWriter message)
        {
            message.WriteShort(GroundDice);
            message.WriteShort(AirDice);
            message.WriteShort(SeaDice);
            message.WriteShort(SubDice);
        }
    }
}