/*
Copyright (c) Luchunpen.
Date: 31/03/2016 09:18
*/

using System;
using System.Collections.Generic;

namespace Nano3.Collection
{
    public interface IQueueHash<TValue> : IEnumerable<TValue>
    {
        int Count { get; }
        bool Enqueue(TValue item);
        TValue Dequeue();
        TValue Peek();
        bool Contains(TValue item);

        TValue[] DequeueAll();
        TValue[] GetValues();
                   
        void Clear();
    }

    public interface IQueueDictionary<TKey, TValue> : IEnumerable<TValue>
    {
        TValue this[TKey key] { get; set; }

        int Count { get; }
        bool ContainsKey(TKey key);
        bool Enqueue(TKey key, TValue value);
        TValue Dequeue();
        TValue Peek();

        KeyValuePair<TKey, TValue> DequeuePair();
        KeyValuePair<TKey, TValue> PeekPair();
        KeyValuePair<TKey, TValue>[] DequeueAllKeyValues();

        TValue[] DequeueAll();
        TValue[] GetValues();
        void Clear();
    }
}
