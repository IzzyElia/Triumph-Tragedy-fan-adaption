using System;
using UnityEngine.XR;

namespace GameSharedInterfaces
{
    public interface IPlayerAction
    {
        public (bool, string) TestParameter(params object[] parameter);
        public void AddParameter(params object[] parameter);
        public void SetAllParameters(params object[] parameters);
        public object[] GetParameters();
        public (bool, string) Validate();
        public void Send(Action<bool> callback);
        public void Reset();
    }
}