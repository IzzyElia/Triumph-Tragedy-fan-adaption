using Unity.Collections;
using UnityEngine.Serialization;

namespace GameSharedInterfaces
{
    public struct MovementActionData
    {
        public bool IsDiploAction;
        public int iCadreOriFactionWarTarget;
        public int iDestinationOriCountryWarTarget;

        public MovementActionData(bool isDiploAction, int iCadreOriFactionWarTarget, int iDestinationOriCountryWarTarget)
        {
            this.IsDiploAction = isDiploAction;
            this.iCadreOriFactionWarTarget = iCadreOriFactionWarTarget;
            this.iDestinationOriCountryWarTarget = iDestinationOriCountryWarTarget;
        }

        public static MovementActionData Recreate(ref DataStreamReader message)
        {
            bool isDiploAction = message.ReadByte() == 1;
            int iCadre = (int)message.ReadShort();
            int iDestination = (int)message.ReadShort();
            return new MovementActionData(isDiploAction: isDiploAction, iCadreOriFactionWarTarget: iCadre, iDestinationOriCountryWarTarget: iDestination);
        }
        public void Write(ref DataStreamWriter message)
        {
            message.WriteByte((byte)(IsDiploAction ? 1 : 0));
            message.WriteShort((short)iCadreOriFactionWarTarget);
            message.WriteShort((short)iDestinationOriCountryWarTarget);
        }
    }
}