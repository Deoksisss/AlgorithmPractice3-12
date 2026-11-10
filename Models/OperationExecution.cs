namespace StackQueueLab.Models;

/// <summary>
/// Журнал выполнения одного набора операций.
/// </summary>
public sealed record SetExecutionResult(
    int SetNumber,
    int OperationCount,
    double Milliseconds,
    IReadOnlyList<string> Log,
    int EmptyOperations);

public sealed record QueueSetTiming(
    int SetNumber,
    int OperationCount,
    double LinkedMilliseconds,
    double StandardMilliseconds);
