using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using Nano3.HashCollection;
namespace HashCollectionBenchTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string s = "R";
            while (s == "R")
            {
                RunFastDictionaryBenchTest(100);
                RunFastDictionaryBenchTest(1000);
                RunFastDictionaryBenchTest(10000);
                RunFastDictionaryBenchTest(100000);

                Console.WriteLine("Press 'R' to repeat");
                s = Console.ReadLine().ToUpper();

            }

            Console.ReadLine().ToUpper();
        }

        public static void RunFastDictionaryBenchTest(int size)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("FastDictionary speed test " + size + Environment.NewLine);

            long[] Arr = new long[size];
            Random rand = new Random(123);
            int tick = 0;

            string s = null;
            while (s != "E")
            {
                for (int i = 0; i < Arr.Length; i++)
                {
                    Arr[i] = tick * tick + tick;
                    tick++;
                }

                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                Dictionary<long, long> dict = new Dictionary<long, long>();
                string dres = RunTest("Dictionary add", DictAddBench, Arr, dict);
                dres += ", " + RunTest("contains", DictContainsBench, Arr, dict);
                dres += ", " + RunTest("get", DictGetBench, Arr, dict);
                dres += ", " + RunTest("try get", DictTryGetBench, Arr, dict);
                dres += ", " + RunTest("remove", DictRemoveBench, Arr, dict);
                Console.WriteLine(dres);

                FastDictionaryM2<long, long> Fdict = new FastDictionaryM2<long, long>();
                string res = RunTest("FastDictionary add", DictAddBench, Arr, Fdict);
                res += ", " + RunTest("contains", DictContainsBench, Arr, Fdict);
                res += ", " + RunTest("get", DictGetBench, Arr, Fdict);
                res += ", " + RunTest("try get", DictTryGetBench, Arr, Fdict);
                res += ", " + RunTest("remove", DictRemoveBench, Arr, Fdict);
                Console.WriteLine(res);

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                s = "E";
            }
        }

        public static string RunTest<T1,T2>(string name, Func<T1,T2, object> func, T1 param1, T2 param2)
        {
            Stopwatch sw = Stopwatch.StartNew();
            func(param1, param2);
            sw.Stop(); TimeSpan ts = sw.Elapsed;
            GC.Collect();

            return name + ": " + ts.TotalMilliseconds;
        }

        public static object DictAddBench<TKey, TDict>(TKey[] items, TDict dict)
           where TDict : IDictionary<TKey, TKey>
           where TKey : struct, IEquatable<TKey>
        {
            TKey k;
            for (int i = 0; i < items.Length; i++)
            {
                k = items[i];
                dict.Add(k, k);
            }
            return null;
        }
        public static object DictContainsBench<TKey, TDict>(TKey[] items, TDict dict)
            where TDict : IDictionary<TKey, TKey>
            where TKey : struct, IEquatable<TKey>
        {
            for (int i = 0; i < items.Length; i++)
            {
                bool b = dict.ContainsKey(items[i]);
            }
            return null;
        }
        public static object DictGetBench<TKey, TDict>(TKey[] items, TDict dict)
            where TDict : IDictionary<TKey, TKey>
            where TKey : struct, IEquatable<TKey>
        {
            for (int i = 0; i < items.Length; i++)
            {
                TKey k = dict[items[i]];
            }
            return null;
        }
        public static object DictTryGetBench<TKey, TDict>(TKey[] items, TDict dict)
            where TDict : IDictionary<TKey, TKey>
            where TKey : struct, IEquatable<TKey>
        {
            for (int i = 0; i < items.Length; i++)
            {
                TKey k; dict.TryGetValue(items[i], out k);
            }
            return null;
        }
        public static object DictRemoveBench<TKey, TDict>(TKey[] items, TDict dict)
            where TDict : IDictionary<TKey, TKey>
            where TKey : struct, IEquatable<TKey>
        {
            for (int i = 0; i < items.Length; i++)
            {
                bool b = dict.Remove(items[i]);
            }
            return null;
        }
    }
}
