using StackQueueLab.Core.Collections;
using StackQueueLab.Models;

namespace StackQueueLab.Services;

/// <summary>
/// Прогоняет одинаковые наборы через связанную очередь и стандартный Queue&lt;T&gt;.
/// Журнал отключается, чтобы замер отражал стоимость операций, а не вывода.
/// </summary>
public sealed class QueueBenchmarkService
{
    private readonly OperationSetRunner _runner;
    private readonly CsvReportWriter _reportWriter;

    public QueueBenchmarkService(OperationSetRunner runner, CsvReportWriter reportWriter)
    {
        _runner = runner;
        _reportWriter = reportWriter;
    }

    public BenchmarkReport Run(
        IReadOnlyList<OperationFile> files,
        DateTime startedAt,
        Action<string>? progress = null)
    {
        var rows = new List<BenchmarkRow>();
        var summaries = new List<BenchmarkFileSummary>();
        WarmUp();

        foreach (var file in files)
        {
            progress?.Invoke(Path.GetFileName(file.Path));

            var linkedTotal = 0.0;
            var standardTotal = 0.0;
            var totalOperations = 0;

            foreach (var set in file.Sets)
            {
                var linked = new LinkedQueueCollectionAdapter();
                var standard = new SystemQueueCollectionAdapter();

                var linkedRun = _runner.Execute(set, linked, verbose: false);
                var standardRun = _runner.Execute(set, standard, verbose: false);

                linkedTotal += linkedRun.Milliseconds;
                standardTotal += standardRun.Milliseconds;
                totalOperations += set.Count;

                rows.Add(new BenchmarkRow(
                    Path.GetFileName(file.Path),
                    set.Number,
                    set.Count,
                    linkedRun.Milliseconds,
                    standardRun.Milliseconds));
            }

            summaries.Add(new BenchmarkFileSummary(
                Path.GetFileName(file.Path),
                file.Sets.Count,
                totalOperations,
                linkedTotal,
                standardTotal));
        }

        var reportPath = _reportWriter.WriteBenchmarkReport(startedAt, rows);

        return new BenchmarkReport(rows, summaries, reportPath);
    }

    private void WarmUp()
    {
        var operations = new List<CollectionOperation>(200);

        for (var i = 0; i < 100; i++)
        {
            operations.Add(new CollectionOperation(1, "warmup"));
            operations.Add(new CollectionOperation(2, null));
        }

        var set = new OperationSet(0, 0, operations);
        _runner.Execute(set, new LinkedQueueCollectionAdapter(), verbose: false);
        _runner.Execute(set, new SystemQueueCollectionAdapter(), verbose: false);
    }
}
