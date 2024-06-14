using System;

namespace Izzy.ForcedInitialization
{
    /// <summary>
    /// Calling ForceInitializer.InitializeUninitializedTypes() will force this class to run its static constructor if it hasn't already
    /// </summary>
    public class ForceInitializeAttribute : Attribute
    {
        public bool IncludeDerived { get; private set; }
        public ForceInitializeAttribute() : this (false) { }
        public ForceInitializeAttribute(bool includeDerived)
        {
            this.IncludeDerived = includeDerived;
        }
    }
}