namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IGameEntity
    {
        public int ID { get; }

        public void PushFullState();
        //public object GetExposedData(string key);
    }
}