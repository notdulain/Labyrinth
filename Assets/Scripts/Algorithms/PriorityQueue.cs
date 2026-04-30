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
            throw new InvalidOperationException("Cannot dequeue from an empty priority queue.");
        }

        T minItem = heap[0].Item;
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        if (heap.Count > 0)
        {
            HeapifyDown(0);
        }

        return minItem;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (heap[parentIndex].Priority <= heap[index].Priority)
            {
                break;
            }

            Swap(parentIndex, index);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int leftChild = (2 * index) + 1;
            int rightChild = (2 * index) + 2;
            int smallest = index;

            if (leftChild < heap.Count && heap[leftChild].Priority < heap[smallest].Priority)
            {
                smallest = leftChild;
            }

            if (rightChild < heap.Count && heap[rightChild].Priority < heap[smallest].Priority)
            {
                smallest = rightChild;
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
