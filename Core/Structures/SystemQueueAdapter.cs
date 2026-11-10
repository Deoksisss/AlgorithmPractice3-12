using StackQueueLab.Core.Interfaces;

namespace StackQueueLab.Core.Structures;

/// <summary>
/// Адаптер стандартного Queue&lt;T&gt; к учебному контракту очереди.
/// </summary>
public sealed class SystemQueueAdapter<T> : IQueueADT<T>
{
    private readonly Queue<T> _queue = new();

    public int Count => _queue.Count;

    public bool IsEmpty => _queue.Count == 0;

    public void Enqueue(T item) => _queue.Enqueue(item);

    public T Dequeue() => _queue.Dequeue();

    public T Peek() => _queue.Peek();

    public IReadOnlyList<T> Snapshot() => _queue.ToArray();
}
