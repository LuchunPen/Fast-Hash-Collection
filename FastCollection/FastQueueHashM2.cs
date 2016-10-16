/*
Copyright (c) Luchunpen (bwolf88).  All rights reserved.
Date: 01.02.2016 17:28:56
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Nano3.Collection
{
    public class FastQueueHashM2<TValue> : IQueueHash<TValue>
        where TValue : IEquatable<TValue>
    {
        //private static readonly string stringUID = "ABC188DDDF3DD103";

        protected int[] _bucket;

        protected TValue[] _values;
        protected int[] _next;
        protected bool[] _fillMarker;

        protected int _count;
        protected int _freeCount;

        protected int _nextFree;
        protected int _size;
        protected int _mask;

        #region queue_var
        protected int[] _queue;
        protected int _qmask;
        protected int _head;
        protected int _tail;
        #endregion queue_var

        public FastQueueHashM2() : this(4) { }
        public FastQueueHashM2(int capacity)
        {
            int cap = HashPrimes.GetPrimeM2(capacity);

            _bucket = new int[cap];
            _next = new int[cap];
            _values = new TValue[cap];
            _fillMarker = new bool[cap];
            _queue = new int[cap];

            _count = 1;
            _nextFree = 0;
            _size = cap;
            _mask = cap - 1;

            _qmask = cap - 1;
            _head = 0;
            _tail = 0;
        }

        public int Count { get { return _count - _freeCount - 1; } }

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

        public bool Enqueue(TValue item)
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
                    if (_values[i].Equals(item)) { _values[i] = item; return false; }
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

                _queue[_tail] = pos;
                _tail = (_tail + 1) & _qmask;

                _freeCount--;
            }
            else
            {
                _next[pos] = next;
                _values[pos] = item;
                _fillMarker[pos] = true;

                _queue[_tail] = pos;

                _tail = (_tail + 1) & _qmask;

                _bucket[hash] = _count;
                _count = _count + 1;
                if (_count >= _size) { Resize(_size * 2); }
                if (Count >= _queue.Length) { ResizeQueue(_queue.Length * 2); }
            }
            return true;
        }
        public TValue Dequeue()
        {
            int qkey = _queue[_head];
            _queue[_head] = 0;
            _head = (_head + 1) & _qmask;

            TValue key = _values[qkey];

            int hash = key.GetHashCode() & _mask;
            int itempos = _bucket[hash];

            int prev = 0;

            FINDMATCH:
            if (itempos == 0) { throw new ArgumentException("No items"); }
            if (_values[itempos].Equals(key))
            {
                if (prev == 0) { _bucket[hash] = _next[itempos]; }
                else { _next[prev] = _next[itempos]; }

                TValue result = _values[itempos];

                _values[itempos] = default(TValue);
                _next[itempos] = _nextFree;
                _fillMarker[itempos] = false;

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

        public TValue[] DequeueAll()
        {
            TValue[] v = GetValues();
            Clear();
            return v;
        }

        public virtual void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(_bucket, 0, _bucket.Length);
                Array.Clear(_next, 0, _next.Length);
                Array.Clear(_values, 0, _values.Length);
                Array.Clear(_fillMarker, 0, _fillMarker.Length);
                Array.Clear(_queue, 0, _queue.Length);

                _count = 1;
                _freeCount = 0;
                _nextFree = 0;
                _head = 0;
                _tail = 0;
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
