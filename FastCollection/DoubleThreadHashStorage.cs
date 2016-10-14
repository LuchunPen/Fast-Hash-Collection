/*
Copyright (c) Luchunpen (bwolf88).  All rights reserved.
Date: 14.05.2016 18:58:59
*/

using System;
using System.Threading;

namespace Nano3.Collection.Special
{
    /// <summary>
    /// Контейнер для хеш коллекции на основе FastHashSet, позволяющая добавлять объекты в одном потоке
    /// и делать выборку всех добавленных объектов с очисткой активной коллекции из другого потока
    /// </summary>
    public class DoubleThreadHashStorage<THash, TValue>
    where THash : FastHashSetM2<TValue>, new()
    where TValue : struct, IEquatable<TValue>
    {
        public THash[] containers;
        private int _receiveIndex;
        private int _transmitIndex;

        public int Count {
            get {
                if (_transmitIndex == 1) { return containers[0].Count; }
                else { return containers[1].Count; }
            }
        }

        public DoubleThreadHashStorage()
        {
            containers = new THash[2];
            containers[0] = new THash();
            containers[1] = new THash();
            _receiveIndex = 0;
            _transmitIndex = 1;
        }

        /// <summary>
        /// Добавляет элемент без блокирования. LockFree добавление может осуществляться только из одного потока
        /// Добавление элементов из двух и более потоков НЕпотокобезопасно
        /// </summary>
        public void AddItem(TValue item)
        {
            int index = Interlocked.Exchange(ref _receiveIndex, 2);
            containers[index].Add(item);
            Interlocked.Exchange(ref _receiveIndex, index);
        }

        /// <summary>
        /// Выборка заполненной хеш коллекции.
        /// Выборка возможна только из одного любого потока, либо из потока который производит заполнение
        /// До последующей выборки коллекция должна освободиться.
        /// Несколько выборок из разных потоков до завершения первой выборки НЕпотокобезопасно.
        /// </summary>
        public THash GetActiveContainer()
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
    }
}
