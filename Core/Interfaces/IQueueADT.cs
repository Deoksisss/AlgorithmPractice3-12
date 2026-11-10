namespace StackQueueLab.Core.Interfaces;

/// <summary>
/// Контракт очереди. Реализуется связным списком и адаптером над Queue&lt;T&gt;.
/// </summary>
public interface IQueueADT<T>
{
    int Count { get; }

    bool IsEmpty { get; }

    void Enqueue(T item);

    T Dequeue();

    T Peek();

    IReadOnlyList<T> Snapshot();
}
