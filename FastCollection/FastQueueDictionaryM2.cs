﻿/*
Copyright (c) Luchunpen.
Date: 04.03.2016 7:07:14
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Nano3.Collection
{
    public interface IQueueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey,TValue>>
    {
        TValue this[TKey key] { get; set; }

        int Count { get; }
        bool ContainsKey(TKey key);
        bool ContainsValue(TValue value);
        bool Enqueue(TKey key, TValue value);
        TValue Dequeue();
        TValue Peek();

        KeyValuePair<TKey, TValue> DequeuePair();
        KeyValuePair<TKey, TValue> PeekPair();
        KeyValuePair<TKey, TValue>[] DequeueAllKeyValues();
        TValue[] ValuesToArray();
        TKey[] KeysToArray();
        
        void Clear();
    }

    public class FastQueueDictionaryM2<TKey, TValue> : IQueueDictionary<TKey, TValue>, IReadOnlyDictionary<TKey,TValue>
        where TKey : struct, IEquatable<TKey>
    {
        //private static readonly string stringUID = "BC12A1ACDE9EA403";

        public enum DoubleKeyMode
        {
            KeepExist = 0,
            Repcale = 1,
            ThrowException = 2
        }

        protected static int MinCapacity = 4;

        protected static int GetPrimeM2(int capacity)
        {
            int result = MinCapacity;
            for (int i = MinCapacity; i < int.MaxValue; i *= 2)
            {
                result = i;
                if (capacity < result) { continue; }
                break;
            }
            return result;
        }

        protected int[] _bucket;

        protected TKey[] _keys;
        protected TValue[] _values;
        protected int[] _next;
        protected bool[] _fillmarker;
        protected int[] _queue;

        protected int _count;
        protected int _freeCount;

        protected int _nextFree;
        protected int _size;
        protected int _mask;

        protected int _qmask;
        protected int _head;
        protected int _tail;

        protected KeyCollection _keyCollection;
        protected ValueCollection _valueCollection;

        protected DoubleKeyMode _dkMode;
        public DoubleKeyMode DkMode { get { return _dkMode; } }

        public FastQueueDictionaryM2() : this (4, DoubleKeyMode.Repcale) { }
        public FastQueueDictionaryM2(DoubleKeyMode dkmode) : this (4, dkmode){ }
        public FastQueueDictionaryM2(int capacity, DoubleKeyMode dkmode)
        {
            int cap = GetPrimeM2(capacity);
            _size = cap;

            _bucket = new int[_size];

            _keys = new TKey[_size];
            _next = new int[_size];
            _values = new TValue[_size];
            _fillmarker = new bool[_size];
            _queue = new int[_size];

            _count = 1;
            _nextFree = 0;
            _mask = _size - 1;
            _qmask = _size - 1;
            _head = 0;
            _tail = 0;

            _dkMode = dkmode;
        }

        public virtual TValue this[TKey key]
        {
            get
            {
                int itempos = _bucket[(key.GetHashCode() & _mask)];

                FINDMATCH:
                if (itempos == 0) { throw new Exception("This id not exist"); }
                if (_keys[itempos].Equals(key)) { return _values[itempos]; }
                else
                {
                    itempos = _next[itempos];
                    goto FINDMATCH;
                }
            }
            set
            {
                Enqueue(key, value);
            }
        }
        public int Count
        {
            get { return _count - _freeCount - 1; }
        }

        public bool ContainsKey(TKey key)
        {
            int itempos = _bucket[(key.GetHashCode() & _mask)];

            FINDMATCH:
            if (itempos == 0) { return false; }
            if (_keys[itempos].Equals(key)) { return true; }
            else
            {
                itempos = _next[itempos];
                goto FINDMATCH;
            }
        }

        public bool ContainsValue(TValue value)
        {
            EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;
            for (int i = 0; i < _count; i++)
            {
                if (_fillmarker[i] == true && c.Equals(_values[i], value)) return true;
            }
            return false;
        }

        public bool Enqueue(TKey key, TValue value)
        {
            int hash = key.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int next = 0, pos = _count;
            if (itempos > 0)
            {
                next = itempos;
                for (int i = itempos; i > 0; i = _next[i])
                {
                    if (_keys[i].Equals(key))
                    {
                        if (_dkMode == DoubleKeyMode.Repcale){
                            _values[i] = value;
                            return true;
                        }
                        else if (_dkMode == DoubleKeyMode.ThrowException){
                            throw new ArgumentException("this key is already exist");
                        }
                        return false;
                    }
                }
            }

            if (_freeCount > 0)
            {
                pos = _bucket[hash] = _nextFree;
                _nextFree = _next[_nextFree];

                _next[pos] = next;
                _values[pos] = value;
                _keys[pos] = key;
                _fillmarker[pos] = true;

                _queue[_tail] = pos;
                _tail = (_tail + 1) & _qmask;

                _freeCount--;
            }
            else
            {
                _next[pos] = next;
                _values[pos] = value;
                _keys[pos] = key;
                _fillmarker[pos] = true;

                _queue[_tail] = pos;

                _tail = (_tail + 1) & _qmask;

                _bucket[hash] = _count;
                _count = _count + 1;
                if (_count >= _size * 0.75f) { Resize(_size * 2); }
                if (Count >= _queue.Length) { ResizeQueue(_queue.Length * 2); }
            }
            return true;
        }

        public TValue Dequeue()
        {
            int qkey = _queue[_head];
            _queue[_head] = 0;
            _head = (_head + 1) & _qmask;

            TKey key = _keys[qkey];

            int hash = key.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int prev = 0;

            FINDMATCH:
            if (itempos == 0) { throw new ArgumentOutOfRangeException("No items"); }
            if (_keys[itempos].Equals(key))
            {
                if (prev == 0) { _bucket[hash] = _next[itempos]; }
                else { _next[prev] = _next[itempos]; }

                TValue result = _values[itempos];

                _values[itempos] = default(TValue);
                _keys[itempos] = default(TKey);
                _next[itempos] = _nextFree;
                _fillmarker[itempos] = false;

                _nextFree = itempos;
                _freeCount++;

                return result;
            }
            else
            {
                prev = itempos;
                itempos = _next[itempos];
                goto FINDMATCH;
            }
        }

        public TValue Peek()
        {
            if (Count == 0) { throw new ArgumentOutOfRangeException("No items"); }
            int qkey = _queue[_head];
            return _values[qkey];
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int itempos = _bucket[(key.GetHashCode() & _mask)];

        FINDMATCH:
            if (itempos == 0) { value = default(TValue); return false; }
            if (_keys[itempos].Equals(key)) { value = _values[itempos]; return true; }
            else
            {
                itempos = _next[itempos];
                goto FINDMATCH;
            }
        }

        protected void Resize(int nsize)
        {
            Console.WriteLine(_count + "/" + nsize);

            int newSize = nsize;
            int newMask = newSize - 1;
            int[] newBucket = new int[newSize];
            int[] newnext = new int[newSize];

            bool[] newfillmarker = new bool[newSize];
            Array.Copy(_fillmarker, newfillmarker, _count);

            TKey[] newkeys = new TKey[newSize];
            Array.Copy(_keys, newkeys, _count);

            TValue[] newvalues = new TValue[newSize];
            Array.Copy(_values, newvalues, _count);

            for (int i = 1; i < _count; i++)
            {
                int bucket = newkeys[i].GetHashCode() & newMask;
                newnext[i] = newBucket[bucket];
                newBucket[bucket] = i;
            }

            _bucket = newBucket;
            _next = newnext;
            _keys = newkeys;
            _values = newvalues;
            _fillmarker = newfillmarker;

            _size = newSize;
            _mask = newMask;
        }
        protected void ResizeQueue(int newsize)
        {
            int[] newQueue = new int[newsize];
            if (_head < _tail) { Array.Copy(_queue, _head, newQueue, 0, Count); }
            else
            {
                Array.Copy(_queue, _head, newQueue, 0, _queue.Length - _head);
                Array.Copy(_queue, 0, newQueue, _queue.Length - _head, _tail);
            }
            _head = 0;
            _tail = (Count == newsize) ? 0 : Count;
            _queue = newQueue;
            _qmask = newsize - 1;
        }

        public virtual void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(_bucket, 0, _bucket.Length);
                Array.Clear(_next, 0, _next.Length);
                Array.Clear(_keys, 0, _keys.Length);
                Array.Clear(_values, 0, _values.Length);
                Array.Clear(_fillmarker, 0, _fillmarker.Length);

                _count = 1;
                _freeCount = 0;
                _nextFree = 0;

                Array.Clear(_queue, 0, _queue.Length);
                _head = 0;
                _tail = 0;
            }
        }

        public KeyValuePair<TKey, TValue> DequeuePair()
        {
            int qkey = _queue[_head];
            _queue[_head] = 0;
            _head = (_head + 1) & _qmask;

            TKey key = _keys[qkey];

            int hash = key.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int prev = 0;

            FINDMATCH:
            if (itempos == 0) { throw new ArgumentOutOfRangeException("No items"); }
            if (_keys[itempos].Equals(key))
            {
                if (prev == 0) { _bucket[hash] = _next[itempos]; }
                else { _next[prev] = _next[itempos]; }

                KeyValuePair<TKey, TValue> result = new KeyValuePair<TKey, TValue>(_keys[itempos], _values[itempos]);

                _values[itempos] = default(TValue);
                _keys[itempos] = default(TKey);
                _next[itempos] = _nextFree;
                _fillmarker[itempos] = false;

                _nextFree = itempos;
                _freeCount++;

                return result;
            }
            else
            {
                prev = itempos;
                itempos = _next[itempos];
                goto FINDMATCH;
            }
        }
        public KeyValuePair<TKey, TValue> PeekPair()
        {
            if (Count == 0) { throw new ArgumentOutOfRangeException("No items"); }
            int qkey = _queue[_head];
            return new KeyValuePair<TKey, TValue>(_keys[qkey], _values[qkey]);
        }
        public KeyValuePair<TKey, TValue>[] DequeueAllKeyValues()
        {
            if (Count <= 0) { return new KeyValuePair<TKey, TValue>[0]; }

            KeyValuePair<TKey, TValue>[] kv = new KeyValuePair<TKey, TValue>[Count];
            int id = 0;
            for (int i = 0; i < _count; i++)
            {
                if (!_fillmarker[i]) continue;
                kv[id++] = new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
            }
            Clear();
            return kv;
        }

        public TValue[] ValuesToArray()
        {
            if (Count <= 0) { return new TValue[0]; }

            TValue[] v = new TValue[Count];
            int id = 0;
            for (int i = 0; i < _count; i++)
            {
                if (!_fillmarker[i]) continue;
                v[id++] = _values[i];
            }
            return v;
        }
        public TKey[] KeysToArray()
        {
            if (Count < 0) { return new TKey[0]; }

            TKey[] k = new TKey[Count];
            int id = 0;
            for (int i = 0; i < _count; i++)
            {
                if (!_fillmarker[i]) continue;
                k[id] = _keys[i];
                id++;
            }
            return k;
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (_keyCollection == null) { _keyCollection = new KeyCollection(this); }
                return _keyCollection;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (_valueCollection == null) { _valueCollection = new ValueCollection(this); }
                return _valueCollection;
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                if (_keyCollection == null) { _keyCollection = new KeyCollection(this); }
                return _keyCollection;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                if (_valueCollection == null) { _valueCollection = new ValueCollection(this); }
                return _valueCollection;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                if (!_fillmarker[i]) continue;
                yield return new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public sealed class KeyCollection : IEnumerable<TKey>, ICollection<TKey>
        {
            private FastQueueDictionaryM2<TKey, TValue> _queue;
            public KeyCollection(FastQueueDictionaryM2<TKey, TValue> queue)
            {
                _queue = queue;
            }

            public int Count { get { return _queue.Count; } }

            public bool IsReadOnly { get { return true; } }

            public bool Contains(TKey item)
            {
                return _queue.ContainsKey(item);
            }

            public void Add(TKey item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(TKey item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array == null) { throw new ArgumentNullException("Array is null"); }
                if (arrayIndex < 0 || arrayIndex > array.Length) { throw new ArgumentOutOfRangeException(); }
                if (array.Length - arrayIndex < Count) { throw new ArgumentOutOfRangeException("Destination array is to small"); }

                int id = 0;
                for (int i = 0; i < _queue._count; i++)
                {
                    if (!_queue._fillmarker[i]) continue;
                    array[id++] = _queue._keys[i];
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                for (int i = 0; i < _queue._count; i++)
                {
                    if (!_queue._fillmarker[i]) continue;
                    yield return _queue._keys[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        public sealed class ValueCollection : IEnumerable<TValue>, ICollection<TValue>
        {
            private FastQueueDictionaryM2<TKey, TValue> _queue;
            public ValueCollection(FastQueueDictionaryM2<TKey, TValue> queue)
            {
                _queue = queue;
            }

            public int Count { get { return _queue.Count; } }

            public bool IsReadOnly { get { return true; } }

            public bool Contains(TValue item)
            {
                return _queue.ContainsValue(item);
            }

            public void Add(TValue item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(TValue item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null) { throw new ArgumentNullException("Array is null"); }
                if (arrayIndex < 0 || arrayIndex > array.Length) { throw new ArgumentOutOfRangeException(); }
                if (array.Length - arrayIndex < Count) { throw new ArgumentOutOfRangeException("Destination array is to small"); }

                int id = 0;
                for (int i = 0; i < _queue._count; i++)
                {
                    if (!_queue._fillmarker[i]) continue;
                    array[id++] = _queue._values[i];
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                for (int i = 0; i < _queue._count; i++)
                {
                    if (!_queue._fillmarker[i]) continue;
                    yield return _queue._values[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
