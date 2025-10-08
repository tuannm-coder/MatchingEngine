using System.Collections;

namespace MatchEngine.DataStructures;

/// <summary>
/// Heap data structure that supports O(1) peek, O(log n) insert/remove, and O(log n) remove by value
/// </summary>
public class IndexedHeap<T> : IEnumerable<T> where T : IComparable<T>
{
    private readonly List<T> _heap;
    private readonly Dictionary<T, int> _indexMap;
    private readonly bool _isMaxHeap;

    public IndexedHeap(bool isMaxHeap = true)
    {
        _heap = new List<T>();
        _indexMap = new Dictionary<T, int>();
        _isMaxHeap = isMaxHeap;
    }

    public int Count => _heap.Count;
    public bool IsEmpty => _heap.Count == 0;

    public T? Peek() => _heap.Count > 0 ? _heap[0] : default;

    public void Insert(T item)
    {
        if (_indexMap.ContainsKey(item))
            throw new ArgumentException("Item already exists in heap");

        _heap.Add(item);
        _indexMap[item] = _heap.Count - 1;
        HeapifyUp(_heap.Count - 1);
    }

    public T? Extract()
    {
        if (_heap.Count == 0) return default;

        var root = _heap[0];
        RemoveAt(0);
        return root;
    }

    public bool Remove(T item)
    {
        if (!_indexMap.TryGetValue(item, out var index))
            return false;

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _heap.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var lastIndex = _heap.Count - 1;
        if (index == lastIndex)
        {
            var item = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);
            _indexMap.Remove(item);
            return;
        }

        // Swap with last element
        Swap(index, lastIndex);
        var removedItem = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);
        _indexMap.Remove(removedItem);

        // Restore heap property
        if (index < _heap.Count)
        {
            HeapifyUp(index);
            HeapifyDown(index);
        }
    }

    public bool Contains(T item) => _indexMap.ContainsKey(item);

    public void Clear()
    {
        _heap.Clear();
        _indexMap.Clear();
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;
            if (Compare(_heap[index], _heap[parentIndex]) <= 0)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            var largest = index;
            var leftChild = 2 * index + 1;
            var rightChild = 2 * index + 2;

            if (leftChild < _heap.Count && Compare(_heap[leftChild], _heap[largest]) > 0)
                largest = leftChild;

            if (rightChild < _heap.Count && Compare(_heap[rightChild], _heap[largest]) > 0)
                largest = rightChild;

            if (largest == index) break;

            Swap(index, largest);
            index = largest;
        }
    }

    private int Compare(T a, T b)
    {
        var result = a.CompareTo(b);
        return _isMaxHeap ? result : -result;
    }

    private void Swap(int i, int j)
    {
        (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
        _indexMap[_heap[i]] = i;
        _indexMap[_heap[j]] = j;
    }

    public IEnumerator<T> GetEnumerator() => _heap.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Get all elements in sorted order without destroying the heap
    /// More efficient than rebuilding sorted list from dictionary keys
    /// </summary>
    public List<T> GetSortedElements()
    {
        if (_heap.Count == 0)
            return new List<T>();

        // Clone the heap to avoid destroying original
        var tempHeap = new List<T>(_heap);
        var tempIndexMap = new Dictionary<T, int>(_indexMap);
        var result = new List<T>(_heap.Count);

        // Extract all elements (they come out in sorted order)
        while (tempHeap.Count > 0)
        {
            var root = tempHeap[0];
            result.Add(root);

            // Remove root (simplified extract without full heap maintenance)
            var lastIndex = tempHeap.Count - 1;
            if (lastIndex == 0)
            {
                tempHeap.RemoveAt(0);
                break;
            }

            // Move last to root and heapify down
            tempHeap[0] = tempHeap[lastIndex];
            tempHeap.RemoveAt(lastIndex);
            HeapifyDownTemp(tempHeap, 0);
        }

        return result;
    }

    private void HeapifyDownTemp(List<T> heap, int index)
    {
        while (true)
        {
            var largest = index;
            var leftChild = 2 * index + 1;
            var rightChild = 2 * index + 2;

            if (leftChild < heap.Count && Compare(heap[leftChild], heap[largest]) > 0)
                largest = leftChild;

            if (rightChild < heap.Count && Compare(heap[rightChild], heap[largest]) > 0)
                largest = rightChild;

            if (largest == index) break;

            (heap[index], heap[largest]) = (heap[largest], heap[index]);
            index = largest;
        }
    }
}
