using System;
using System.Collections.Generic;

namespace Izzy
{
    public readonly struct UnorderedPair<T> : IEquatable<UnorderedPair<T>>
    {
        public T First { get; }
        public T Second { get; }

        public UnorderedPair(T first, T second)
        {
            First = first;
            Second = second;
        }

        public override bool Equals(object obj)
        {
            return obj is UnorderedPair<T> pair && Equals(pair);
        }

        public bool Equals(UnorderedPair<T> other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hashFirst = EqualityComparer<T>.Default.GetHashCode(First);
            int hashSecond = EqualityComparer<T>.Default.GetHashCode(Second);

            // Combine the hash codes in an order-independent way
            return hashFirst ^ hashSecond;
        }

        public static bool operator ==(UnorderedPair<T> left, UnorderedPair<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnorderedPair<T> left, UnorderedPair<T> right)
        {
            return !(left == right);
        }
    }
}