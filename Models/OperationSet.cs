namespace StackQueueLab.Models;

/// <summary>
/// Один набор операций — непустая строка входного файла.
/// </summary>
public sealed record OperationSet(int Number, int SourceLine, IReadOnlyList<CollectionOperation> Operations)
{
    public int Count => Operations.Count;
}
