using System;
using Unity.Collections;

namespace GameSharedInterfaces
{
    public struct CombatOption
    {
        public int iTile;
        public int iAttacker;
        public int iDefender;
        public bool IsOptional;

        public CombatOption(int iTile, int iAttacker, int iDefender, bool isOptional = true)
        {
            this.iTile = iTile;
            this.iDefender = iDefender;
            this.iAttacker = iAttacker;
            IsOptional = isOptional;
        }
        
        public static CombatOption Recreate(ref DataStreamReader message)
        {
            int iTile = (int)message.ReadShort();
            int iAttacker = (int)message.ReadShort();
            int iDefender = (int)message.ReadShort();
            bool isOptional = message.ReadByte() == 1;
            return new CombatOption(iTile: iTile, iAttacker:iAttacker, iDefender: iDefender, isOptional: isOptional);
        }
        public void Write(ref DataStreamWriter message)
        {
            message.WriteShort((short)iTile);
            message.WriteShort((short)iAttacker);
            message.WriteShort((short)iDefender);
            message.WriteByte((byte)(IsOptional ? 1 : 0));
        }
    }
}