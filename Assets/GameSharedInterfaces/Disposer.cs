using System;
using System.Collections.Generic;

namespace GameSharedInterfaces
{
    public class Disposer
    {
        public static List<IDisposable> disposeOnApplicationExit = new List<IDisposable>();

        public static void Register(IDisposable disposable) => disposeOnApplicationExit.Add(disposable);
        public static void DisposeAll()
        {
            foreach (var threadRisk in disposeOnApplicationExit)
            {
                threadRisk.Dispose();
            }
        }
    }
}