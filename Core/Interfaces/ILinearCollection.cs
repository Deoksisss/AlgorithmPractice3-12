namespace StackQueueLab.Core.Interfaces;

/// <summary>
/// Общий интерфейс для исполнителя операций 1–5. Скрывает разницу между
/// стеком, связанной очередью и стандартным Queue&lt;T&gt;.
/// </summary>
public interface ILinearCollection
{
    string DisplayName { get; }

    string AddName { get; }

    string RemoveName { get; }

    string PeekName { get; }

    string OrderHint { get; }

    int Count { get; }

    bool IsEmpty { get; }

    void Add(string value);

    string Remove();

    string Peek();

    IReadOnlyList<string> Snapshot();
}
