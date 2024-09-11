using System;

namespace Izzy
{
    public static class Hashing
    {
        public static int CombineHashes(int hash, int newHash)
        {
            unchecked
            {
                hash += 17;
                hash *= newHash;
            }
            return hash;
        }

        public static int MurmurHash3_Combine(params int[] data)
        {
            int hash = 17;
            unchecked
            {
                for (int i = 0; i < data.Length; i++)
                {
                    hash += 17;
                    hash *= MurmurHash3(data[i]);
                }
            }

            return hash;
        }
        public static int MurmurHash3(string data, uint seed = 0)
        {
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;
            const uint r1 = 15;
            const uint r2 = 13;
            const uint m = 5;
            const uint n = 0xe6546b64;

            int len = data.Length;
            uint h = seed;

            // Process the input data in blocks of 4 bytes (32 bits)
            for (int i = 0; i < len; i += 4)
            {
                uint k = (uint)data[i]
                         | ((uint)data[Math.Min(i + 1, len - 1)] << 8)
                         | ((uint)data[Math.Min(i + 2, len - 1)] << 16)
                         | ((uint)data[Math.Min(i + 3, len - 1)] << 24);

                k *= c1;
                k = (k << (int)r1) | (k >> (int)(32 - r1));
                k *= c2;

                h ^= k;
                h = ((h << (int)r2) | (h >> (int)(32 - r2))) * m + n;
            }

            // Finalize the hash
            h ^= (uint)len;
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return (int)h;
        }
        public static int MurmurHash3(byte[] data, uint seed = 0)
        {
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;
            const uint r1 = 15;
            const uint r2 = 13;
            const uint m = 5;
            const uint n = 0xe6546b64;

            int len = data.Length;
            uint h = seed;

            for (int i = 0; i < len; i += 4)
            {
                uint k = BitConverter.ToUInt32(data, i);

                k *= c1;
                k = (k << (int)r1) | (k >> (int)(32 - r1));
                k *= c2;

                h ^= k;
                h = ((h << (int)r2) | (h >> (int)(32 - r2))) * m + n;
            }

            h ^= (uint)len;
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return (int)h;
        }

        public static int MurmurHash3(byte data, uint seed = 0) => MurmurHash3(new byte[] { data }, seed);
    
        public static int MurmurHash3(sbyte data, uint seed = 0) => MurmurHash3(new byte[] { (byte)data }, seed);

        public static int MurmurHash3(short data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);

        public static int MurmurHash3(ushort data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(int data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(uint data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(long data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(ulong data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(float data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(double data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(bool data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(char data, uint seed = 0) => MurmurHash3(BitConverter.GetBytes(data), seed);
    
        public static int MurmurHash3(decimal data, uint seed = 0) {
            int[] bits = decimal.GetBits(data);
            byte[] bytes = new byte[16];
        
            Buffer.BlockCopy(BitConverter.GetBytes(bits[0]), 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(bits[1]), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(bits[2]), 0, bytes, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(bits[3]), 0, bytes, 12, 4);
        
            return MurmurHash3(bytes, seed);
        }
    }
}


