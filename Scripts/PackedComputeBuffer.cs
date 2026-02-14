using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PackedComputeBuffer<Key, Value> : IDisposable, IEnumerable<(Key, Value)> where Value : notnull {
    Dictionary<Key, int> _keyToIndex = new Dictionary<Key, int>();
    Key[] _indexToKey;
    Value[] _data;
    public ComputeBuffer Buffer { get; private set; }
    int _currentIndex;
    int _low, _high; // Marks range [low, high] that has modification

    readonly int _minSize;
    readonly int _strideSize;

    public int Capacity => _data.Length;
    public int Count => _currentIndex;

    public event Action<int, int> OnSwap; // <old index, new index>
    public event Action<int> OnResize; // <new size>

    public PackedComputeBuffer(int minSize, int strideSize) {
        if (minSize <= 0) {
            throw new ArgumentException("MinSize must be a positive non zero number.");
        }
        _minSize = minSize;
        _strideSize = strideSize;

        Buffer = new ComputeBuffer(minSize, strideSize);
        _indexToKey = new Key[minSize];
        _data = new Value[minSize];

        _currentIndex = 0;
        _high = -1;
        _low = int.MaxValue;
    }

    public void Resize(int size) {
        size = Math.Max(_minSize, size);
        if (size == Capacity) {
            return;
        }

        if (Buffer.IsValid()) {
            Buffer.Release();
        }
        Buffer = new ComputeBuffer(size, _strideSize);
        Buffer.SetData(_data, 0, 0, _currentIndex);

        var newIndexToKey = new Key[size];
        Array.Copy(_indexToKey, newIndexToKey, _currentIndex);
        _indexToKey = newIndexToKey;

        var newData = new Value[size];
        Array.Copy(_data, newData, _currentIndex);
        _data = newData;
        OnResize?.Invoke(size);
    }

    public void Add(Key key, Value data) {
        if (Capacity == Count) {
            Resize(Capacity * 2);
        }

        _data[_currentIndex] = data;
        _indexToKey[_currentIndex] = key;
        _keyToIndex[key] = _currentIndex;

        _low = Math.Min(_low, _currentIndex);
        _high = Math.Max(_high, _currentIndex);
        _currentIndex++;
    }

    public bool Remove(Key key) {
        if (!_keyToIndex.TryGetValue(key, out var removeIdx)) {
            return false;
        }

        _currentIndex--;
        _low = Math.Min(_low, Math.Min(_currentIndex, removeIdx));
        _high = Math.Max(_high, Math.Max(_currentIndex, removeIdx));

        _data[removeIdx] = _data[_currentIndex];

        Key swapKey = _indexToKey[_currentIndex];
        _keyToIndex[swapKey] = removeIdx;
        _indexToKey[removeIdx] = swapKey;
        _keyToIndex.Remove(key);

        if (_currentIndex != removeIdx) {
            OnSwap?.Invoke(_currentIndex, removeIdx);
        }

        if (Count < Capacity / 4) {
            Resize(Capacity / 2);
        }

        return true;
    }

    public void SetValue(Key key, Value value) {
        int idx = _keyToIndex[key];
        _data[idx] = value;
        _low = Math.Min(_low, idx);
        _high = Math.Max(_high, idx);
    }

    /// Update buffer on the GPU
    public bool UpdateBuffer() {
        if (_high == -1 || !Buffer.IsValid()) { // TODO is !Buffer.IsValid() correct?
            if (!Buffer.IsValid())
                Debug.Log("NOT valid");
            return false;
        }
        int count = _high - _low + 1;
        Buffer.SetData(_data, _low, _low, count);

        _high = -1;
        _low = int.MaxValue;
        return true;
    }

    public void Release() {
        Buffer.Release();
    }

    public void Dispose() {
        Buffer.Release();
    }

    ~PackedComputeBuffer() {
        Buffer.Release();
    }

    public bool IsDirty() {
        return _high != -1;
    }

    public IEnumerator<(Key, Value)> GetEnumerator() {
        for (int i = 0; i < _currentIndex; i++) {
            Key key = _indexToKey[i];
            yield return (key, _data[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
