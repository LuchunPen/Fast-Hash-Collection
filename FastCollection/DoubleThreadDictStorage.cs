/*
Copyright (c) Luchunpen (bwolf88).  All rights reserved.
Date: 14.05.2016 19:00:18
*/

using System;
using System.Threading;
using System.Collections.Generic;

namespace Nano3.Collection.Special
{
    /// <summary>
    /// Контейнер для Хеш коллекции на основе FastDictionary, позволяющая добавлять объекты в одном потоке
    /// и делать выборку всех добавленных объектов с очисткой активной коллекции из другого потока
    /// </summary>
    public class DoubleThreadDictionaryStrorage<TDictionary, TKey, TValue>
    where TDictionary : FastDictionaryM2<TKey, TValue>, new()
    where TKey : struct, IEquatable<TKey>
    {
        public TDictionary[] containers;
        private int _receiveIndex;
        private int _transmitIndex;

        public int Count
        {
            get
            {
                if (_transmitIndex == 1) { return containers[0].Count; }
                else { return containers[1].Count; }
            }
        }

        public DoubleThreadDictionaryStrorage()
        {
            containers = new TDictionary[2];
            containers[0] = new TDictionary();
            containers[1] = new TDictionary();
            _receiveIndex = 0;
            _transmitIndex = 1;
        }

        /// <summary>
        /// Добавляет ключ/значение без блокирования. LockFree добавление может осуществляться только из одного потока
        /// Добавление элементов из двух и более потоков НЕпотокобезопасно
        /// </summary>
        public void AddItem(TKey key, TValue item)
        {
            int index = Interlocked.Exchange(ref _receiveIndex, 2);
            containers[index].Add(key, item);
            Interlocked.Exchange(ref _receiveIndex, index);
        }

        /// <summary>
        /// Добавляет ключ/значение без блокирования. LockFree добавление может осуществляться только из одного потока
        /// Добавление элементов из двух и более потоков НЕпотокобезопасно
        /// </summary>
        public void AddItem(KeyValuePair<TKey, TValue> item)
        {
            int index = Interlocked.Exchange(ref _receiveIndex, 2);
            containers[index].Add(item.Key, item.Value);
            Interlocked.Exchange(ref _receiveIndex, index);
        }

        /// <summary>
        /// Выборка заполненной хеш коллекции.
        /// Выборка возможна только из одного любого потока, либо из потока который производит заполнение
        /// До последующей выборки коллекция должна освободиться.
        /// Несколько выборок из разных потоков до завершения первой выборки НЕпотокобезопасно.
        /// </summary>
        public TDictionary GetActiveContainer()
        {
            int rec = _transmitIndex == 0 ? 1 : 0;
            while ((Interlocked.CompareExchange(ref _receiveIndex, _transmitIndex, rec)) != rec) { }
            _transmitIndex = rec;
            return containers[_transmitIndex];
        }

        /// <summary>
        /// Выборка всех элементов заполненной коллекции.
        /// Выборка возможна только из одного любого потока, либо из потока который производит заполнение
        /// Несколько выборок из разных потоков до завершения первой выборки НЕпотокобезопасно.
        /// </summary>
        public TValue[] PullValues()
        {
            int rec = _transmitIndex == 0 ? 1 : 0;
            while ((Interlocked.CompareExchange(ref _receiveIndex, _transmitIndex, rec)) != rec) { }
            _transmitIndex = rec;
            TValue[] result = containers[_transmitIndex].GetValues();
            containers[_transmitIndex].Clear();
            return result;
        }

        /// <summary>
        /// Выборка всех пар ключ/значение заполненной коллекции.
        /// Выборка возможна только из одного любого потока, либо из потока который производит заполнение
        /// Несколько выборок из разных потоков до завершения первой выборки НЕпотокобезопасно.
        /// </summary>
        public KeyValuePair<TKey, TValue>[] PullKeyValues()
        {
            int rec = _transmitIndex == 0 ? 1 : 0;
            while ((Interlocked.CompareExchange(ref _receiveIndex, _transmitIndex, rec)) != rec) { }
            _transmitIndex = rec;
            KeyValuePair<TKey, TValue>[] result = containers[_transmitIndex].GetKeyValues();
            containers[_transmitIndex].Clear();
            return result;
        }
    }
}
