using System;
using UnityEngine.XR;

namespace GameSharedInterfaces
{
    public interface IPlayerAction
    {
        public (bool valid, string reason) TestParameter(params object[] parameter);
        public void AddParameter(params object[] parameter);
        public bool RemoveParameter(params object[] parameter);
        public void SetAllParameters(params object[] parameters);
        public object[] GetParameters();
        public object[] GetData();
        public (bool, string) Validate();
        public void Send(Action<bool> callback);
        public void Reset();
    }
}