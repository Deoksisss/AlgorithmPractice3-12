using System.Diagnostics;
using StackQueueLab.Core.Interfaces;
using StackQueueLab.Models;

namespace StackQueueLab.Services;

/// <summary>
/// Выполняет набор операций над любым ILinearCollection.
/// Для замеров используется тихий режим, для показа — потоковый вывод:
/// журнал не накапливается в памяти, что важно для стресс-файлов.
/// </summary>
public sealed class OperationSetRunner
{
    public SetExecutionResult Execute(
        OperationSet set,
        ILinearCollection collection,
        bool verbose = true)
    {
        var log = verbose ? new List<string>(set.Count + 1) : null;
        return ExecuteCore(set, collection, log, null);
    }

    public SetExecutionResult ExecuteStreaming(
        OperationSet set,
        ILinearCollection collection,
        Action<string> output)
    {
        return ExecuteCore(set, collection, null, output);
    }

    private static SetExecutionResult ExecuteCore(
        OperationSet set,
        ILinearCollection collection,
        List<string>? log,
        Action<string>? output)
    {
        var emptyOperations = 0;
        var capture = log is not null || output is not null;

        void Emit(string line)
        {
            log?.Add(line);
            output?.Invoke(line);
        }

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < set.Operations.Count; i++)
        {
            var operation = set.Operations[i];

            switch (operation.Code)
            {
                case 1:
                    collection.Add(operation.Value!);

                    if (capture)
                    {
                        Emit($"  {i + 1:00}. {collection.AddName}({operation.Value}) → добавлено; размер = {collection.Count}");
                    }

                    break;

                case 2:
                    if (collection.IsEmpty)
                    {
                        emptyOperations++;

                        if (capture)
                        {
                            Emit($"  {i + 1:00}. {collection.RemoveName}() → пусто, операция пропущена");
                        }
                    }
                    else
                    {
                        var removed = collection.Remove();

                        if (capture)
                        {
                            Emit($"  {i + 1:00}. {collection.RemoveName}() → {removed}; размер = {collection.Count}");
                        }
                    }

                    break;

                case 3:
                    if (collection.IsEmpty)
                    {
                        emptyOperations++;

                        if (capture)
                        {
                            Emit($"  {i + 1:00}. {collection.PeekName}() → пусто");
                        }
                    }
                    else
                    {
                        var top = collection.Peek();

                        if (capture)
                        {
                            Emit($"  {i + 1:00}. {collection.PeekName}() → {top}");
                        }
                    }

                    break;

                case 4:
                    if (capture)
                    {
                        Emit($"  {i + 1:00}. isEmpty() → {(collection.IsEmpty ? "да" : "нет")}");
                    }

                    break;

                case 5:
                    var items = collection.Snapshot();

                    if (capture)
                    {
                        var content = items.Count == 0
                            ? "[ ] — пусто"
                            : $"[ {string.Join(", ", items)} ]  ({collection.OrderHint})";

                        Emit($"  {i + 1:00}. Print() → {content}");
                    }

                    break;
            }
        }

        stopwatch.Stop();

        return new SetExecutionResult(
            set.Number,
            set.Count,
            stopwatch.Elapsed.TotalMilliseconds,
            (IReadOnlyList<string>?)log ?? Array.Empty<string>(),
            emptyOperations);
    }
}
