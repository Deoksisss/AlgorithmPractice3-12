using System.Diagnostics;
using StackQueueLab.Models;
using StackQueueLab.Services;

namespace StackQueueLab.UI.Controllers;

public sealed class QueueBenchmarkController
{
    private readonly ProjectPaths _paths;
    private readonly OperationSetRunner _runner;
    private readonly CsvReportWriter _reports;
    private readonly OperationFileParser _parser = new();

    public QueueBenchmarkController(
        ProjectPaths paths,
        OperationSetRunner runner,
        CsvReportWriter reports)
    {
        _paths = paths;
        _runner = runner;
        _reports = reports;
    }

    public void Show()
    {
        ConsoleTheme.BeginScreen("Главное меню / Очередь / Сравнение");
        ConsoleTheme.Title("Сравнительный анализ", "Связанная очередь против стандартного Queue<T>");

        var files = FileCatalog.ListTextFiles(_paths.QueueInput)
            .Where(file => !file.Name.Contains("EXTREME", StringComparison.OrdinalIgnoreCase))
            .Select(file => (File: file, Parsed: _parser.Parse(file.Path)))
            .ToList();

        var valid = files
            .Where(entry => entry.Parsed.IsValid && entry.Parsed.File is not null)
            .Select(entry => entry.Parsed.File!)
            .ToList();

        var invalid = files.Where(entry => !entry.Parsed.IsValid).ToList();

        if (valid.Count == 0)
        {
            ConsoleTheme.Warning("Нет корректных файлов для сравнения.");
            ConsoleTheme.Pause("Enter — назад");
            return;
        }

        if (invalid.Count > 0)
        {
            ConsoleTheme.Warning($"Пропущено файлов с ошибками: {invalid.Count}.");
        }

        var totalSets = valid.Sum(file => file.Sets.Count);
        var totalOperations = valid.Sum(file => file.TotalOperations);

        ConsoleTheme.KeyValue("Файлов", valid.Count.ToString());
        ConsoleTheme.KeyValue("Наборов", totalSets.ToString());
        ConsoleTheme.KeyValue("Операций", totalOperations.ToString());
        ConsoleTheme.KeyValue("Папка", _paths.QueueInput);

        ConsoleTheme.Section("Что будет измерено");
        ConsoleTheme.Text("1. Наборы разной длины: 10, 100 и 1000 операций.");
        ConsoleTheme.Text("2. Одинаковая длина с разным составом: push-heavy, read-heavy, mixed.");
        ConsoleTheme.Text("Журнал отключён, поэтому замер отражает стоимость операций.");

        if (!ConsoleInput.Confirm("Запустить сравнение?"))
        {
            return;
        }

        ConsoleTheme.BeginScreen("Главное меню / Очередь / Сравнение");
        ConsoleTheme.Title("Выполняется сравнение…", "Не закрывайте программу");

        var startedAt = DateTime.Now;
        var watch = Stopwatch.StartNew();
        var report = new QueueBenchmarkService(_runner, _reports).Run(
            valid,
            startedAt,
            fileName => ConsoleTheme.Muted($"  обработан: {fileName}"));
        watch.Stop();

        DisplayReport(report, totalOperations, watch.Elapsed.TotalMilliseconds);
    }

    private static void DisplayReport(
        BenchmarkReport report,
        int totalOperations,
        double totalMilliseconds)
    {
        ConsoleTheme.Separator("сводка по файлам");
        ConsoleTheme.Success("Сравнение завершено.");

        ConsoleTheme.Color(ConsoleColor.DarkGray);
        Console.WriteLine(
            $"  {"Файл",-39}{"Наборов",9}{"Операций",10}{"Связная",10}{"Queue",9}");
        Console.WriteLine($"  {new string('─', 77)}");
        ConsoleTheme.Color(ConsoleColor.Gray);

        foreach (var file in report.Files)
        {
            var name = Shorten(file.FileName, 38);
            Console.WriteLine(
                $"  {name,-39}{file.SetCount,9}{file.TotalOperations,10}" +
                $"{CsvReportWriter.FormatMilliseconds(file.LinkedMilliseconds),10}" +
                $"{CsvReportWriter.FormatMilliseconds(file.StandardMilliseconds),9}");
        }

        ConsoleTheme.Rule();
        ConsoleTheme.KeyValue("Операций всего", totalOperations.ToString());
        ConsoleTheme.KeyValue("Время сравнения", $"{CsvReportWriter.FormatMilliseconds(totalMilliseconds)} мс");

        var differentLength = report.Files
            .GroupBy(file => file.SetCount)
            .FirstOrDefault(group => group.Select(file => file.TotalOperations).Distinct().Count() >= 3);

        if (differentLength is not null)
        {
            ConsoleTheme.Section("Разная длина наборов");
            foreach (var file in differentLength.OrderBy(file => file.TotalOperations))
            {
                var perSet = file.TotalOperations / Math.Max(1, file.SetCount);
                ConsoleTheme.Text(
                    $"{Shorten(file.FileName, 43)}  {perSet,5} оп./набор  " +
                    $"связная {CsvReportWriter.FormatMilliseconds(file.LinkedMilliseconds)} мс");
            }
        }

        var sameLengthGroups = report.Files
            .GroupBy(file => (file.SetCount, file.TotalOperations))
            .Where(group => group.Count() >= 2)
            .ToList();

        foreach (var group in sameLengthGroups)
        {
            DisplaySameLengthGroup(group.ToList());
        }

        var fastest = report.Files
            .Where(file => file.LinkedMilliseconds > 0 && file.TotalOperations >= 1000)
            .OrderBy(file => file.Ratio)
            .FirstOrDefault();

        if (fastest is not null)
        {
            ConsoleTheme.Section("Максимальное расхождение реализаций");
            ConsoleTheme.Text(
                $"{fastest.FileName}: связанная {CsvReportWriter.FormatMilliseconds(fastest.LinkedMilliseconds)} мс, " +
                $"Queue<T> {CsvReportWriter.FormatMilliseconds(fastest.StandardMilliseconds)} мс.");
            ConsoleTheme.Text(
                $"Стандартная очередь быстрее в {1.0 / Math.Max(fastest.Ratio, 0.000001):0.##} раза.");
        }

        ConsoleTheme.Section("Отчёт");
        ConsoleTheme.KeyValue("CSV", report.ReportPath);
        ConsoleTheme.Muted("  В CSV сохранён результат каждого набора: обе реализации, разница и отношение.");

        ConsoleTheme.Pause("Enter — вернуться в меню очереди");
    }

    private static void DisplaySameLengthGroup(IReadOnlyList<BenchmarkFileSummary> files)
    {
        BenchmarkFileSummary? bestA = null;
        BenchmarkFileSummary? bestB = null;
        var bestDivergence = -1.0;

        for (var i = 0; i < files.Count; i++)
        {
            for (var j = i + 1; j < files.Count; j++)
            {
                var a = files[i];
                var b = files[j];
                var denominator = Math.Max(a.LinkedMilliseconds, b.LinkedMilliseconds);
                var divergence = denominator <= 0
                    ? 0
                    : Math.Abs(a.LinkedMilliseconds - b.LinkedMilliseconds) / denominator;

                if (divergence > bestDivergence)
                {
                    bestDivergence = divergence;
                    bestA = a;
                    bestB = b;
                }
            }
        }

        if (bestA is null || bestB is null)
        {
            return;
        }

        ConsoleTheme.Section("Одинаковая длина, разный состав");
        ConsoleTheme.Text(
            $"{bestA.SetCount} наборов · {bestA.TotalOperations} операций · расхождение {bestDivergence * 100:0.0}%");
        ConsoleTheme.Text(
            $"{Shorten(bestA.FileName, 44)}  →  связанная " +
            $"{CsvReportWriter.FormatMilliseconds(bestA.LinkedMilliseconds)} мс");
        ConsoleTheme.Text(
            $"{Shorten(bestB.FileName, 44)}  →  связанная " +
            $"{CsvReportWriter.FormatMilliseconds(bestB.LinkedMilliseconds)} мс");
    }

    private static string Shorten(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
