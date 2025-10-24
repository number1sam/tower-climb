using System;

namespace TowerClimb.Utils
{
    /// <summary>
    /// Seeded PRNG using xoshiro128** algorithm
    /// CRITICAL: Must produce identical output to TypeScript version for anti-cheat
    /// </summary>
    public class SeededRandom
    {
        private uint[] state = new uint[4];

        public SeededRandom(long seed)
        {
            Seed(seed);
        }

        /// <summary>
        /// Initialize state from seed using SplitMix64
        /// </summary>
        private void Seed(long seed)
        {
            ulong s = (ulong)seed;

            for (int i = 0; i < 4; i++)
            {
                s += 0x9e3779b97f4a7c15UL;
                ulong z = s;
                z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
                z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
                z = z ^ (z >> 31);
                state[i] = (uint)(z & 0xffffffffUL);
            }
        }

        /// <summary>
        /// xoshiro128** next() - returns 32-bit unsigned integer
        /// </summary>
        private uint Next()
        {
            uint result = RotateLeft(state[1] * 5, 7) * 9;
            uint t = state[1] << 9;

            state[2] ^= state[0];
            state[3] ^= state[1];
            state[1] ^= state[2];
            state[0] ^= state[3];

            state[2] ^= t;
            state[3] = RotateLeft(state[3], 11);

            return result;
        }

        private uint RotateLeft(uint x, int k)
        {
            return (x << k) | (x >> (32 - k));
        }

        /// <summary>
        /// Returns float in [0, 1)
        /// </summary>
        public float NextFloat()
        {
            return Next() / (float)0x100000000UL;
        }

        /// <summary>
        /// Returns integer in [0, max)
        /// </summary>
        public int NextInt(int max)
        {
            return (int)Math.Floor(NextFloat() * max);
        }

        /// <summary>
        /// Returns integer in [min, max)
        /// </summary>
        public int NextRange(int min, int max)
        {
            return min + NextInt(max - min);
        }
    }

    /// <summary>
    /// Weighted random choice helper
    /// </summary>
    public static class WeightedChoice
    {
        public static T Choose<T>(T[] items, float[] weights, SeededRandom rng)
        {
            if (items.Length != weights.Length)
            {
                throw new ArgumentException("Items and weights must have same length");
            }

            float totalWeight = 0f;
            foreach (float w in weights)
            {
                totalWeight += w;
            }

            float random = rng.NextFloat() * totalWeight;

            for (int i = 0; i < items.Length; i++)
            {
                random -= weights[i];
                if (random <= 0)
                {
                    return items[i];
                }
            }

            return items[items.Length - 1]; // fallback
        }
    }
}
