using StackQueueLab.Core.Interfaces;

namespace StackQueueLab.Core.Structures;

/// <summary>
/// Очередь на собственном односвязном списке с указателями head и tail.
/// </summary>
public sealed class LinkedQueue<T> : IQueueADT<T>
{
    private sealed class Node
    {
        public Node(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public Node? Next { get; set; }
    }

    private Node? _head;
    private Node? _tail;

    public int Count { get; private set; }

    public bool IsEmpty => _head is null;

    public void Enqueue(T item)
    {
        var node = new Node(item);

        if (_tail is null)
        {
            _head = node;
            _tail = node;
        }
        else
        {
            _tail.Next = node;
            _tail = node;
        }

        Count++;
    }

    public T Dequeue()
    {
        if (_head is null)
        {
            throw new InvalidOperationException("Очередь пуста: операция удаления невозможна.");
        }

        var value = _head.Value;
        _head = _head.Next;

        if (_head is null)
        {
            _tail = null;
        }

        Count--;
        return value;
    }

    public T Peek()
    {
        if (_head is null)
        {
            throw new InvalidOperationException("Очередь пуста: просмотр начала невозможен.");
        }

        return _head.Value;
    }

    public IReadOnlyList<T> Snapshot()
    {
        var result = new List<T>(Count);
        var current = _head;

        while (current is not null)
        {
            result.Add(current.Value);
            current = current.Next;
        }

        return result;
    }
}
