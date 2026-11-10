using StackQueueLab.Core.Interfaces;
using StackQueueLab.Core.Structures;

namespace StackQueueLab.Core.Collections;

/// <summary>
/// Адаптер стека для единого исполнителя операций.
/// </summary>
public sealed class StackCollectionAdapter : ILinearCollection
{
    private readonly LinkedStack<string> _stack = new();

    public string DisplayName => "Стек (связный список)";

    public string AddName => "Push";

    public string RemoveName => "Pop";

    public string PeekName => "Top";

    public string OrderHint => "верх → низ";

    public int Count => _stack.Count;

    public bool IsEmpty => _stack.IsEmpty;

    public void Add(string value) => _stack.Push(value);

    public string Remove() => _stack.Pop();

    public string Peek() => _stack.Peek();

    public IReadOnlyList<string> Snapshot() => _stack.Snapshot();
}
