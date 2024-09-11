using System;


namespace GameBoard
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ShowInDebugWindowAttribute : Attribute
    {
    }
}