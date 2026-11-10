using StackQueueLab.Core.Interfaces;
using StackQueueLab.Core.Structures;

namespace StackQueueLab.Core.Collections;

/// <summary>
/// Адаптер связанной очереди для единого исполнителя операций.
/// </summary>
public sealed class LinkedQueueCollectionAdapter : ILinearCollection
{
    private readonly LinkedQueue<string> _queue = new();

    public string DisplayName => "Очередь (связный список)";

    public string AddName => "Enqueue";

    public string RemoveName => "Dequeue";

    public string PeekName => "Peek";

    public string OrderHint => "начало → конец";

    public int Count => _queue.Count;

    public bool IsEmpty => _queue.IsEmpty;

    public void Add(string value) => _queue.Enqueue(value);

    public string Remove() => _queue.Dequeue();

    public string Peek() => _queue.Peek();

    public IReadOnlyList<string> Snapshot() => _queue.Snapshot();
}
