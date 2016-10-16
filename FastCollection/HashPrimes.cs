/*
Copyright (c) Luchunpen (bwolf88).  All rights reserved.
Date: 22.01.2016 9:01:01
*/

using System;
using System.Collections.Generic;

namespace Nano3.Collection
{
    public static class HashPrimes
    {
        public static readonly int[] primes =
        {
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        };

        public static readonly int[] primesM2 =
        {
            4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096,
            8192, 16384, 32768, 65536, 131072, 262144, 524288,
            1048576, 2097152, 4194304, 8388608
        };

        public static int GetPrime(int capacity)
        {
            if (capacity < primes[0]) { return primes[0] + 1; }
            for (int i = 0; i < primes.Length; i++)
            {
                if (primes[i] >= capacity) { return primes[i] + 1; }
            }
            return primes[primes.Length - 1] + 1;
        }

        public static int GetPrimeM2(int capacity)
        {
            if (capacity < primesM2[0]) { return primesM2[0]; }
            for (int i = 0; i < primesM2.Length; i++)
            {
                if (primesM2[i] >= capacity) { return primesM2[i]; }
            }
            return primesM2[primesM2.Length - 1];
        }
    }
}
