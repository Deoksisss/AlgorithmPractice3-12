using StackQueueLab.Core.Interfaces;

namespace StackQueueLab.Core.Structures;

/// <summary>
/// Стек на собственном односвязном списке.
/// Вершина стека — первый узел списка.
/// </summary>
public sealed class LinkedStack<T> : IStackADT<T>
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

    public int Count { get; private set; }

    public bool IsEmpty => _head is null;

    public void Push(T item)
    {
        var node = new Node(item)
        {
            Next = _head
        };

        _head = node;
        Count++;
    }

    public T Pop()
    {
        if (_head is null)
        {
            throw new InvalidOperationException("Стек пуст: операция Pop невозможна.");
        }

        var value = _head.Value;
        _head = _head.Next;
        Count--;
        return value;
    }

    public T Peek()
    {
        if (_head is null)
        {
            throw new InvalidOperationException("Стек пуст: операция Top невозможна.");
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
