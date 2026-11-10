using System.Diagnostics;
using StackQueueLab.Core.Collections;
using StackQueueLab.Models;
using StackQueueLab.Services;

namespace StackQueueLab.UI.Controllers;

public sealed class QueueLabController
{
    private readonly ProjectPaths _paths;
    private readonly FilePicker _picker;
    private readonly OperationSetRunner _runner;
    private readonly CsvReportWriter _reports;
    private readonly OperationFileParser _parser = new();
    private readonly QueueBenchmarkController _benchmark;

    public QueueLabController(
        ProjectPaths paths,
        FilePicker picker,
        OperationSetRunner runner,
        CsvReportWriter reports)
    {
        _paths = paths;
        _picker = picker;
        _runner = runner;
        _reports = reports;
        _benchmark = new QueueBenchmarkController(paths, runner, reports);
    }

    public void Run()
    {
        while (true)
        {
            ConsoleTheme.BeginScreen("Главное меню / Очередь");
            ConsoleTheme.Title("Очередь", "Связный список и стандартный Queue<T>");
            ConsoleTheme.MenuOption("1", "Выполнить файл", "один набор операций — обе реализации");
            ConsoleTheme.MenuOption("2", "Сравнительный отчёт", "разная длина и разный состав операций");
            ConsoleTheme.MenuOption("0", "Назад");
            ConsoleTheme.Rule();

            var choice = ConsoleInput.ReadMenuChoice(2);

            if (choice == 0)
            {
                return;
            }

            if (choice == 1)
            {
                var file = _picker.PickOperationFile(
                    "Главное меню / Очередь / Выполнение",
                    _paths.QueueInput);

                if (file is not null)
                {
                    Execute(file);
                    ConsoleTheme.Pause("Enter — вернуться в меню очереди");
                }
            }
            else
            {
                _benchmark.Show();
            }
        }
    }

    private void Execute(OperationFile selected)
    {
        var startedAt = DateTime.Now;
        var wallWatch = Stopwatch.StartNew();
        var isStress = Path.GetFileName(selected.Path)
            .Contains("EXTREME", StringComparison.OrdinalIgnoreCase);

        ConsoleTheme.BeginScreen("Главное меню / Очередь / Выполнение");
        ConsoleTheme.Title(Path.GetFileName(selected.Path), "Один набор — две реализации");

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

        var showLog = true;

        if (isStress)
        {
            ConsoleTheme.Warning(
                "Стресс-файл: полный вывод обеих реализаций может занять больше минуты.");
        }
        else if (file.TotalOperations > 3000)
        {
            ConsoleTheme.Warning($"Файл содержит {file.TotalOperations} операций — журнал будет очень большим.");
            showLog = ConsoleInput.Confirm("Показывать каждую операцию для обеих реализаций?");
        }

        var timings = new List<QueueSetTiming>(file.Sets.Count);
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

            // Сначала чистый замер без журнала, затем — потоковое проигрывание.
            var linked = _runner.Execute(set, new LinkedQueueCollectionAdapter(), verbose: false);
            var standard = _runner.Execute(set, new SystemQueueCollectionAdapter(), verbose: false);

            PrintImplementationHeader("Связанная очередь");

            if (showLog)
            {
                _runner.ExecuteStreaming(
                    set,
                    new LinkedQueueCollectionAdapter(),
                    line => ConsoleTheme.WriteLogLine("    " + line.TrimStart()));
            }
            else
            {
                ConsoleTheme.Muted("    журнал скрыт (режим замера)");
            }

            ConsoleTheme.KeyValue(
                "    Время",
                $"{CsvReportWriter.FormatMilliseconds(linked.Milliseconds)} мс");

            PrintImplementationHeader("Стандартный Queue<T>");

            if (showLog)
            {
                _runner.ExecuteStreaming(
                    set,
                    new SystemQueueCollectionAdapter(),
                    line => ConsoleTheme.WriteLogLine("    " + line.TrimStart()));
            }
            else
            {
                ConsoleTheme.Muted("    журнал скрыт (режим замера)");
            }

            ConsoleTheme.KeyValue(
                "    Время",
                $"{CsvReportWriter.FormatMilliseconds(standard.Milliseconds)} мс");

            timings.Add(new QueueSetTiming(
                set.Number,
                set.Count,
                linked.Milliseconds,
                standard.Milliseconds));
        }

        totalWatch.Stop();

        string reportPath;

        try
        {
            reportPath = _reports.WriteQueueReport(selected.Path, startedAt, timings);
        }
        catch (Exception exception)
        {
            ConsoleTheme.Error($"Не удалось сохранить CSV: {exception.Message}");
            ConsoleTheme.Pause();
            return;
        }

        ConsoleTheme.Separator("итоги");
        ConsoleTheme.Success("Все наборы выполнены обеими реализациями.");

        ConsoleTheme.Color(ConsoleColor.DarkGray);
        Console.WriteLine(
            $"  {"Набор",-7}{"Операций",10}{"Связная, мс",15}{"Queue, мс",13}");
        Console.WriteLine($"  {new string('─', 47)}");
        ConsoleTheme.Color(ConsoleColor.Gray);

        foreach (var timing in timings)
        {
            Console.WriteLine(
                $"  {timing.SetNumber,-7}{timing.OperationCount,10}" +
                $"{CsvReportWriter.FormatMilliseconds(timing.LinkedMilliseconds),15}" +
                $"{CsvReportWriter.FormatMilliseconds(timing.StandardMilliseconds),13}");
        }

        ConsoleTheme.Rule();
        ConsoleTheme.KeyValue(
            "Связная очередь",
            $"{CsvReportWriter.FormatMilliseconds(timings.Sum(t => t.LinkedMilliseconds))} мс");
        ConsoleTheme.KeyValue(
            "Стандартный Queue<T>",
            $"{CsvReportWriter.FormatMilliseconds(timings.Sum(t => t.StandardMilliseconds))} мс");
        ConsoleTheme.KeyValue(
            "Всё вместе",
            $"{CsvReportWriter.FormatMilliseconds(totalWatch.Elapsed.TotalMilliseconds)} мс");
        ConsoleTheme.KeyValue("CSV", reportPath);

        wallWatch.Stop();
        ConsoleTheme.KeyValue(
            "Полное время (с выводом)",
            $"{CsvReportWriter.FormatMilliseconds(wallWatch.Elapsed.TotalMilliseconds)} мс");

    }

    private static void PrintImplementationHeader(string name)
    {
        ConsoleTheme.Color(ConsoleColor.DarkCyan);
        Console.WriteLine($"    {name}");
    }
}
