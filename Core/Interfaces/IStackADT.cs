namespace StackQueueLab.Core.Interfaces;

/// <summary>
/// Контракт стека. Реализуется собственным связным списком.
/// </summary>
public interface IStackADT<T>
{
    int Count { get; }

    bool IsEmpty { get; }

    void Push(T item);

    T Pop();

    T Peek();

    IReadOnlyList<T> Snapshot();
}
