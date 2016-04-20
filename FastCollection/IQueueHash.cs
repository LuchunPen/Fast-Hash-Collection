/*
Copyright (c) Luchunpen (bwolf88).  All rights reserved.
Date: 31/03/2016 09:18
*/

using System;
using System.Collections.Generic;

namespace Nano3.HashCollection
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
        bool Enqueue(TKey key, TValue value);
        TValue Dequeue();
        TValue Peek();
        bool ContainsKey(TKey key);

        TValue[] DequeueAll();
        TValue[] GetValues();
        void Clear();
    }
}
