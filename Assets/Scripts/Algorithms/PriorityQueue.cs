using System;
using System.Collections.Generic;

/// <summary>
/// Generic min-heap priority queue used by pathfinding algorithms.
/// </summary>
public class PriorityQueue<T>
{
    private readonly List<(T Item, float Priority)> heap = new List<(T Item, float Priority)>();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        HeapifyUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
        {
            throw new InvalidOperationException("Cannot dequeue from an empty queue.");
        }

        T item = heap[0].Item;
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        if (heap.Count > 0)
        {
            HeapifyDown(0);
        }

        return item;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[parent].Priority <= heap[index].Priority)
            {
                break;
            }

            Swap(parent, index);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int left = (2 * index) + 1;
            int right = (2 * index) + 2;
            int smallest = index;

            if (left < heap.Count && heap[left].Priority < heap[smallest].Priority)
            {
                smallest = left;
            }

            if (right < heap.Count && heap[right].Priority < heap[smallest].Priority)
            {
                smallest = right;
            }

            if (smallest == index)
            {
                break;
            }

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (heap[a], heap[b]) = (heap[b], heap[a]);
    }
}
