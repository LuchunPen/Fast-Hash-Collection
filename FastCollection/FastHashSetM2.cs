/*
Copyright (c) Luchunpen (bwolf88).  All rights reserved.
Date: 24.01.2016 18:46:14
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Nano3.HashCollection
{
    public class FastHashSetM2<TValue> : ICollection<TValue>
        where TValue : IEquatable<TValue>
    {
        //private static readonly string stringUID = "ABC188E99ACD9C04";

        protected int[] _bucket;

        protected TValue[] _values;
        protected int[] _next;
        protected bool[] _fillMarker;

        protected int _count;
        protected int _freeCount;

        protected int _nextFree;
        protected int _size;
        protected int _mask;

        public FastHashSetM2() : this(4) { }
        public FastHashSetM2(int capacity)
        {
            int cap = HashPrimes.GetPrimeM2(capacity);

            _bucket = new int[cap];

            _next = new int[cap];
            _values = new TValue[cap];
            _fillMarker = new bool[cap];

            _count = 1;
            _nextFree = 0;
            _size = cap;
            _mask = _size - 1;
        }

        public int Count { get { return _count - _freeCount - 1; } }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public virtual void Add(TValue item)
        {
            int hash = item.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int next = 0, pos = _count;
            if (itempos > 0)
            {
                next = itempos;

                //check to match ===================
                for (int i = itempos; i > 0; i = _next[i])
                {
                    if (_values[i].Equals(item))
                    {
                        throw new ArgumentException("This id already exist");
                    }
                }
                //=========================================
            }
            if (_freeCount > 0)
            {
                pos = _bucket[hash] = _nextFree;
                _nextFree = _next[_nextFree];

                _next[pos] = next;
                _values[pos] = item;
                _fillMarker[pos] = true;

                _freeCount--;
            }
            else
            {
                _next[pos] = next;
                _values[pos] = item;
                _fillMarker[pos] = true;

                _bucket[hash] = _count;
                _count = _count + 1;
                if (_count >= _size) { Resize(_size * 2); }
            }
        }

        public bool Remove(TValue item)
        {
            int hash = item.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int prev = 0;

            FINDMATCH:
            if (itempos == 0) { return false; }
            if (_values[itempos].Equals(item))
            {
                if (prev == 0) { _bucket[hash] = _next[itempos]; }
                else { _next[prev] = _next[itempos]; }

                _values[itempos] = default(TValue);
                _next[itempos] = _nextFree;
                _fillMarker[itempos] = false;

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

        public bool Contains(TValue item)
        {
            int itempos = _bucket[item.GetHashCode() & _mask];

            FINDMATCH:
            if (itempos == 0) return false;
            if (_values[itempos].Equals(item)) { return true; }
            else
            {
                itempos = _next[itempos];
                goto FINDMATCH;
            }
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null) { throw new ArgumentNullException("Array is null"); }
            if (arrayIndex < 0 || arrayIndex > array.Length) { throw new ArgumentOutOfRangeException(); }
            if (array.Length - arrayIndex < Count) { throw new ArgumentOutOfRangeException("Destination array is to small"); }

            for (int i = 0; i < _count; i++)
            {
                if (!_fillMarker[i]) continue;
                array[arrayIndex++] = _values[i];
            }
        }

        public virtual void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(_bucket, 0, _bucket.Length);
                Array.Clear(_next, 0, _next.Length);
                Array.Clear(_values, 0, _values.Length);
                Array.Clear(_fillMarker, 0, _fillMarker.Length);

                _count = 1;
                _freeCount = 0;
                _nextFree = 0;
                _mask = _size - 1;
            }
        }

        protected void Resize(int nsize)
        {
            int newSize = nsize;
            int newMask = newSize - 1;

            int[] newBucket = new int[newSize];
            int[] newnext = new int[newSize];

            bool[] newfillmarker = new bool[newSize];
            Array.Copy(_fillMarker, newfillmarker, _size);

            TValue[] newvalues = new TValue[newSize];
            Array.Copy(_values, newvalues, _size);

            for (int i = 1; i < _size; i++)
            {
                int bucket = newvalues[i].GetHashCode() & newMask;
                newnext[i] = newBucket[bucket];
                newBucket[bucket] = i;
            }

            _bucket = newBucket;
            _next = newnext;
            _values = newvalues;
            _fillMarker = newfillmarker;

            _size = newSize;
            _mask = newMask;
        }

        public TValue[] GetValues()
        {
            TValue[] v = new TValue[Count];
            int id = 0;
            for (int i = 0; i < _count; i++)
            {
                if (!_fillMarker[i]) continue;
                v[id++] = _values[i];
            }
            return v;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                if (!_fillMarker[i]) continue;
                yield return _values[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
