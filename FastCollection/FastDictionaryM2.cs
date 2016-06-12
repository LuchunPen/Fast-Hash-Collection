/*
Copyright (c) Luchunpen (bwolf88).  All rights reserved.
Date: 04.03.2016 6:42:37
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Nano3.HashCollection
{
    public class FastDictionaryM2<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
    {
        //private static readonly string stringUID = "CC78FEF1FDAD4302";

        protected int[] _bucket;

        protected TKey[] _keys;
        protected TValue[] _values;
        protected int[] _next;
        protected bool[] _fillmarker;

        protected int _count;
        protected int _freeCount;

        protected int _nextFree;
        protected int _size;
        protected int _mask;

        public FastDictionaryM2() : this (4) { }
        public FastDictionaryM2(int capacity)
        {
            int cap = HashPrimes.GetPrimeM2(capacity);
            _size = cap;

            _bucket = new int[_size];

            _keys = new TKey[_size];
            _next = new int[_size];
            _values = new TValue[_size];
            _fillmarker = new bool[_size];

            _count = 1;
            _nextFree = 0;
            _mask = _size - 1;
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
                int hash = key.GetHashCode() & _mask;
                int itempos = _bucket[hash];

                int next = 0;
                if (itempos > 0)
                {
                    next = itempos;
                    for (int i = itempos; i > 0; i = _next[i])
                    {
                        if (_keys[i].Equals(key)) { _values[i] = value; return; }
                    }
                }
                if (_freeCount > 0)
                {
                    int pos = _bucket[hash] = _nextFree;
                    _nextFree = _next[_nextFree];
                    _next[pos] = next; _values[pos] = value; _keys[pos] = key; _fillmarker[pos] = true;
                    _freeCount--;
                }
                else
                {
                    _next[_count] = next; _values[_count] = value; _keys[_count] = key; _fillmarker[_count] = true;
                    _bucket[hash] = _count;
                    _count = _count + 1;
                    if (_count >= _size) { Resize(_size * 2); }
                }
            }
        }

        public int Count
        {
            get { return _count - _freeCount - 1; }
        }

        public bool IsReadOnly
        {
            get { return false; }
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
        public virtual void Add(TKey key, TValue value)
        {
            int hash = key.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int next = 0;
            if (itempos > 0)
            {
                next = itempos;
                //check to match ===================
                for (int i = itempos; i > 0; i = _next[i])
                {
                    if (_keys[i].Equals(key))
                    {
                        throw new Exception("This id already exist");
                    }
                }
                //=========================================
            }
            if (_freeCount > 0)
            {
                int pos = _bucket[hash] = _nextFree;
                _nextFree = _next[_nextFree];
                _next[pos] = next; _values[pos] = value; _keys[pos] = key; _fillmarker[pos] = true;
                _freeCount--;
            }
            else
            {
                _next[_count] = next; _values[_count] = value; _keys[_count] = key; _fillmarker[_count] = true;
                _bucket[hash] = _count;
                _count = _count + 1;
                if (_count >= _size) { Resize(_size * 2); }
            }
        }
        public bool Remove(TKey key)
        {
            int hash = key.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int prev = 0;

            FINDMATCH:
            if (itempos == 0) { return false; }
            if (_keys[itempos].Equals(key))
            {
                if (prev == 0) { _bucket[hash] = _next[itempos]; }
                else { _next[prev] = _next[itempos]; }

                _values[itempos] = default(TValue);
                _keys[itempos] = default(TKey);
                _next[itempos] = _nextFree;
                _fillmarker[itempos] = false;

                _nextFree = itempos;
                _freeCount++;
                return true;
            }
            else
            {
                prev = itempos;
                itempos = _next[itempos];
                goto FINDMATCH;
            }
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
                _mask = _size - 1;
            }
        }

        public TKey[] GetKeys()
        {
            TKey[] k = new TKey[Count];
            int id = 0;
            for (int i = 0; i < _count; i++)
            {
                if (!_fillmarker[i]) continue;
                k[id++] = _keys[i];
            }
            return k;
        }
        public TValue[] GetValues()
        {
            TValue[] v = new TValue[Count];
            int id = 0;
            for (int i = 0; i < _count; i++)
            {
                if (!_fillmarker[i]) continue;
                v[id++] = _values[i];
            }
            return v;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int itempos = _bucket[(item.Key.GetHashCode() & _mask)];

            FINDMATCH:
            if (itempos == 0) { return false; }
            if (_keys[itempos].Equals(item.Key))
            {
                if (_values[itempos].Equals(item)) return true;
                return false;
            }
            else
            {
                itempos = _next[itempos];
                goto FINDMATCH;
            }
        }
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item)) { return Remove(item.Key); }
            return false;
        }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null) { throw new ArgumentNullException("Array is null"); }
            if (arrayIndex < 0 || arrayIndex > array.Length) { throw new ArgumentOutOfRangeException(); }
            if (array.Length - arrayIndex < Count) { throw new ArgumentOutOfRangeException("Destination array is to small"); }

            int id = 0;
            for (int i = 0; i < _count; i++)
            {
                if (!_fillmarker[i]) continue;
                array[id++] = new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
            }
        }

        protected void Resize(int nsize)
        {
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

            for (int i = 1; i < _size; i++)
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

        public ICollection<TKey> Keys
        {
            get { return GetKeys(); }
        }

        public ICollection<TValue> Values
        {
            get { return GetValues(); }
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
    }
}
