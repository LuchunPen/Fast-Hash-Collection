﻿/*
Copyright (c) Luchunpen.
Date: 24.01.2016 18:46:14
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Nano3.Collection
{
    public class FastHashSetM2<TValue> : ICollection<TValue>, IReadOnlyCollection<TValue>
        where TValue : IEquatable<TValue>
    {
        //private static readonly string stringUID = "ABC188E99ACD9C04";

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

        protected TValue[] _values;
        protected int[] _next;
        protected bool[] _fillMarker;

        protected int _count;
        protected int _freeCount;

        protected int _nextFree;
        protected int _size;
        protected int _mask;
        protected bool _isReadOnly;

        protected DoubleKeyMode _dkMode;
        public DoubleKeyMode DKMode { get { return _dkMode; } }

        public FastHashSetM2() : this(4, DoubleKeyMode.KeepExist) { }
        public FastHashSetM2(DoubleKeyMode dkmode) : this(4, dkmode) { }
        public FastHashSetM2(int capacity, DoubleKeyMode dkmode)
        {
            int cap = GetPrimeM2(capacity);

            _bucket = new int[cap];

            _next = new int[cap];
            _values = new TValue[cap];
            _fillMarker = new bool[cap];

            _count = 1;
            _nextFree = 0;
            _size = cap;
            _mask = _size - 1;

            _dkMode = dkmode;
        }

        public int Count { get { return _count - _freeCount - 1; } }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; }
        }

        public virtual void Add(TValue item)
        {
            if (_isReadOnly) { throw new NotImplementedException(); }

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
                        if (_dkMode == DoubleKeyMode.Repcale){
                            _values[i] = item;
                        }
                        else if (_dkMode == DoubleKeyMode.ThrowException){
                            throw new ArgumentException("this key is already exist");
                        }
                        return;
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
                if (_count >= _size * 0.75f) { Resize(_size * 2); }
            }
        }

        public void Add(TValue[] items)
        {
            if (_isReadOnly) { throw new NotImplementedException(); }

            if (items == null || items.Length == 0) { return; }
            for (int i = 0; i < items.Length; i++)
            {
                Add(items[i]);
            }
        }

        public void Add(List<TValue> items)
        {
            if (_isReadOnly) { throw new NotImplementedException(); }

            if (items == null || items.Count == 0) { return; }
            int count = items.Count;
            for (int i = 0; i < count; i++)
            {
                Add(items[i]);
            }
        }

        public bool Remove(TValue item)
        {
            if (_isReadOnly) { throw new NotImplementedException(); }

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
            if (_isReadOnly) { throw new NotImplementedException(); }

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
            Array.Copy(_fillMarker, newfillmarker, _count);

            TValue[] newvalues = new TValue[newSize];
            Array.Copy(_values, newvalues, _count);

            for (int i = 1; i < _count; i++)
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

        public TValue[] GetValuesArray()
        {
            if (Count <= 0) { return new TValue[0]; }

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
