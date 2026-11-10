using System.Diagnostics;
using StackQueueLab.Core.Collections;
using StackQueueLab.Models;
using StackQueueLab.Services;

namespace StackQueueLab.UI.Controllers;

public sealed class StackLabController
{
    private readonly ProjectPaths _paths;
    private readonly FilePicker _picker;
    private readonly OperationSetRunner _runner;
    private readonly CsvReportWriter _reports;
    private readonly OperationFileParser _parser = new();

    public StackLabController(
        ProjectPaths paths,
        FilePicker picker,
        OperationSetRunner runner,
        CsvReportWriter reports)
    {
        _paths = paths;
        _picker = picker;
        _runner = runner;
        _reports = reports;
    }

    public void Run()
    {
        while (true)
        {
            var file = _picker.PickOperationFile(
                "Главное меню / Стек",
                _paths.StackInput);

            if (file is null)
            {
                return;
            }

            Execute(file);
            ConsoleTheme.Pause("Enter — выбрать другой файл");
        }
    }

    private void Execute(OperationFile selected)
    {
        var startedAt = DateTime.Now;
        var wallWatch = Stopwatch.StartNew();
        var isStress = Path.GetFileName(selected.Path)
            .Contains("EXTREME", StringComparison.OrdinalIgnoreCase);

        ConsoleTheme.BeginScreen("Главное меню / Стек / Выполнение");
        ConsoleTheme.Title(Path.GetFileName(selected.Path), "Собственный стек на связном списке");

        if (isStress)
        {
            ConsoleTheme.Warning("Стресс-файл: полный вывод может занять больше минуты.");
        }

        var totalWatch = Stopwatch.StartNew();
        var readingWatch = Stopwatch.StartNew();
        var parsed = _parser.Parse(selected.Path);
        readingWatch.Stop();

        if (!parsed.IsValid || parsed.File is null)
        {
            ConsoleTheme.Error("Файл изменился или содержит ошибки. Выполнение отменено.");
            ConsoleTheme.Pause("Enter — назад");
            return;
        }

        var file = parsed.File;
        ConsoleTheme.KeyValue("Наборов", file.Sets.Count.ToString());
        ConsoleTheme.KeyValue("Операций", file.TotalOperations.ToString());
        ConsoleTheme.KeyValue("Чтение и разбор", $"{CsvReportWriter.FormatMilliseconds(readingWatch.Elapsed.TotalMilliseconds)} мс");

        var results = new List<SetExecutionResult>(file.Sets.Count);
        ConsoleTheme.Section("Журнал выполнения");

        for (var i = 0; i < file.Sets.Count; i++)
        {
            var set = file.Sets[i];

            if (i > 0)
            {
                ConsoleTheme.Separator($"набор {i + 1} из {file.Sets.Count}");
            }

            ConsoleTheme.Accent(
                $"  Набор {set.Number} · строка {set.SourceLine} · операций: {set.Count}");

            // Замер — без построения журнала. Затем набор проигрывается ещё раз
            // с потоковым выводом: журнал не копится в памяти.
            var result = _runner.Execute(set, new StackCollectionAdapter(), verbose: false);
            results.Add(result);

            _runner.ExecuteStreaming(
                set,
                new StackCollectionAdapter(),
                ConsoleTheme.WriteLogLine);

            ConsoleTheme.KeyValue(
                "Время набора",
                $"{CsvReportWriter.FormatMilliseconds(result.Milliseconds)} мс");

            if (result.EmptyOperations > 0)
            {
                ConsoleTheme.Muted($"  Пустых обращений: {result.EmptyOperations} (операция пропущена).");
            }
        }

        totalWatch.Stop();

        string reportPath;

        try
        {
            reportPath = _reports.WriteStackReport(selected.Path, startedAt, results);
        }
        catch (Exception exception)
        {
            ConsoleTheme.Error($"Не удалось сохранить CSV: {exception.Message}");
            ConsoleTheme.Pause();
            return;
        }

        ConsoleTheme.Separator("итоги");
        ConsoleTheme.Success("Все наборы выполнены.");

        var header = $"  {"Набор",-7}{"Операций",12}{"Время, мс",16}";
        ConsoleTheme.Color(ConsoleColor.DarkGray);
        Console.WriteLine(header);
        Console.WriteLine($"  {new string('─', 35)}");
        ConsoleTheme.Color(ConsoleColor.Gray);

        foreach (var result in results)
        {
            Console.WriteLine(
                $"  {result.SetNumber,-7}{result.OperationCount,12}{CsvReportWriter.FormatMilliseconds(result.Milliseconds),16}");
        }

        var executionTime = results.Sum(result => result.Milliseconds);
        ConsoleTheme.Rule();
        ConsoleTheme.KeyValue("Операций всего", file.TotalOperations.ToString());
        ConsoleTheme.KeyValue("Суммарно операции", $"{CsvReportWriter.FormatMilliseconds(executionTime)} мс");
        ConsoleTheme.KeyValue("Всё вместе", $"{CsvReportWriter.FormatMilliseconds(totalWatch.Elapsed.TotalMilliseconds)} мс");
        ConsoleTheme.KeyValue("CSV", reportPath);
        ConsoleTheme.Muted("  Эти же строки «количество операций; время» сохранены в отчёте.");

        wallWatch.Stop();
        ConsoleTheme.KeyValue(
            "Полное время (с выводом)",
            $"{CsvReportWriter.FormatMilliseconds(wallWatch.Elapsed.TotalMilliseconds)} мс");

    }
}
