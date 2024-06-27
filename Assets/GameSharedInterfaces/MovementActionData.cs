using Unity.Collections;

namespace GameSharedInterfaces
{
    public struct MovementActionData
    {
        public int iCadre;
        public int iDestination;

        public MovementActionData(int iCadre, int iDestination)
        {
            this.iCadre = iCadre;
            this.iDestination = iDestination;
        }

        public static MovementActionData Recreate(ref DataStreamReader message)
        {
            int iCadre = (int)message.ReadShort();
            int iDestination = (int)message.ReadShort();
            return new MovementActionData(iCadre: iCadre, iDestination: iDestination);
        }
        public void Write(ref DataStreamWriter message)
        {
            message.WriteShort((short)iCadre);
            message.WriteShort((short)iDestination);
        }
    }
}