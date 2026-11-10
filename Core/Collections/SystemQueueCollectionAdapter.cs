using StackQueueLab.Core.Interfaces;
using StackQueueLab.Core.Structures;

namespace StackQueueLab.Core.Collections;

/// <summary>
/// Адаптер стандартной очереди для единого исполнителя операций.
/// </summary>
public sealed class SystemQueueCollectionAdapter : ILinearCollection
{
    private readonly SystemQueueAdapter<string> _queue = new();

    public string DisplayName => "Очередь (стандартный Queue<T>)";

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
