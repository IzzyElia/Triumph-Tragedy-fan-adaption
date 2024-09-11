using System;
using System.Collections.Generic;
using System.Linq;

namespace Izzy
{
    public static class IzzyUtility
    {
        public static IEnumerable<T> Shuffle<T>(IEnumerable<T> source, Random rng)
        {
            T[] elements = source.ToArray();
            // Note i > 0 to avoid final pointless iteration
            for (int i = elements.Length-1; i > 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                int swapIndex = rng.Next(i + 1);
                (elements[i], elements[swapIndex]) = (elements[swapIndex], elements[i]);
            }
            // Lazily yield (avoiding aliasing issues etc)
            foreach (T element in elements)
            {
                yield return element;
            }
        }
    }
}