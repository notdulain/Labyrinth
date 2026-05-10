using System.Collections.Generic;

/// <summary>
/// A simple min-priority queue for A* nodes.
/// </summary>
public class PriorityQueue
{
    private readonly List<Node> items = new List<Node>();

    public int Count
    {
        get { return items.Count; }
    }

    public void Clear()
    {
        items.Clear();
    }

    public void Enqueue(Node node)
    {
        items.Add(node);
        SortUp(items.Count - 1);
    }

    public Node Dequeue()
    {
        if (items.Count == 0)
        {
            return null;
        }

        Node firstItem = items[0];
        int lastIndex = items.Count - 1;

        items[0] = items[lastIndex];
        items.RemoveAt(lastIndex);

        if (items.Count > 0)
        {
            SortDown(0);
        }

        return firstItem;
    }

    public bool Contains(Node node)
    {
        return items.Contains(node);
    }

    public void UpdateItem(Node node)
    {
        int index = items.IndexOf(node);
        if (index < 0)
        {
            return;
        }

        SortUp(index);
    }

    private void SortUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (Compare(items[index], items[parentIndex]) >= 0)
            {
                break;
            }

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void SortDown(int index)
    {
        while (true)
        {
            int leftChildIndex = index * 2 + 1;
            int rightChildIndex = index * 2 + 2;
            int smallestIndex = index;

            if (leftChildIndex < items.Count && Compare(items[leftChildIndex], items[smallestIndex]) < 0)
            {
                smallestIndex = leftChildIndex;
            }

            if (rightChildIndex < items.Count && Compare(items[rightChildIndex], items[smallestIndex]) < 0)
            {
                smallestIndex = rightChildIndex;
            }

            if (smallestIndex == index)
            {
                break;
            }

            Swap(index, smallestIndex);
            index = smallestIndex;
        }
    }

    private int Compare(Node a, Node b)
    {
        if (a.fCost != b.fCost)
        {
            return a.fCost.CompareTo(b.fCost);
        }

        return a.hCost.CompareTo(b.hCost);
    }

    private void Swap(int firstIndex, int secondIndex)
    {
        Node temporary = items[firstIndex];
        items[firstIndex] = items[secondIndex];
        items[secondIndex] = temporary;
    }
}
