using System.Diagnostics;
using StackQueueLab.Models;
using StackQueueLab.Services;

namespace StackQueueLab.UI.Controllers;

public sealed class PostfixLabController
{
    private readonly ProjectPaths _paths;
    private readonly FilePicker _picker;
    private readonly ExpressionFileService _expressions;
    private readonly CsvReportWriter _reports;
    private readonly PostfixEvaluator _evaluator = new();
    private readonly ComplexityAnalyzer _complexity = new();

    public PostfixLabController(
        ProjectPaths paths,
        FilePicker picker,
        ExpressionFileService expressions,
        CsvReportWriter reports)
    {
        _paths = paths;
        _picker = picker;
        _expressions = expressions;
        _reports = reports;
    }

    public void Run()
    {
        while (true)
        {
            var file = _picker.PickPostfixFile(
                "Главное меню / Постфиксные выражения",
                _paths.PostfixInput);

            if (file is null)
            {
                return;
            }

            Execute(file);
            ConsoleTheme.Pause("Enter — выбрать другой файл");
        }
    }

    private void Execute(ExpressionFile selected)
    {
        var startedAt = DateTime.Now;
        var wallWatch = Stopwatch.StartNew();
        var isStress = Path.GetFileName(selected.Path)
            .Contains("EXTREME", StringComparison.OrdinalIgnoreCase);

        ConsoleTheme.BeginScreen("Главное меню / Постфиксные выражения / Вычисление");
        ConsoleTheme.Title(Path.GetFileName(selected.Path), "Постфиксная запись: число операций · время · сложность");

        if (isStress)
        {
            ConsoleTheme.Warning("Стресс-файл: пошаговая трассировка может занять больше минуты.");
        }

        var totalWatch = Stopwatch.StartNew();
        var readingWatch = Stopwatch.StartNew();
        var file = _expressions.LoadPostfix(selected.Path);
        readingWatch.Stop();

        if (!file.IsValid)
        {
            ConsoleTheme.Error("Файл изменился или содержит ошибки. Выполнение отменено.");
            ConsoleTheme.Pause("Enter — назад");
            return;
        }

        ConsoleTheme.KeyValue("Выражений", file.Lines.Count.ToString());
        ConsoleTheme.KeyValue("Чтение и разбор", $"{CsvReportWriter.FormatMilliseconds(readingWatch.Elapsed.TotalMilliseconds)} мс");
        ConsoleTheme.Section("Вычисления");

        var results = new List<PostfixLineResult>(file.Lines.Count);

        foreach (var line in file.Lines)
        {
            var watch = Stopwatch.StartNew();
            double? value = null;
            string? error = null;

            try
            {
                value = _evaluator.Evaluate(line.Expression);
            }
            catch (ExpressionEvaluationException exception)
            {
                error = exception.Message;
            }

            watch.Stop();

            var complexity = _complexity.AnalyzePostfix(line.Expression);
            var result = new PostfixLineResult(
                line.Number,
                line.SourceLine,
                line.Expression,
                value,
                error,
                watch.Elapsed.TotalMilliseconds,
                complexity);

            results.Add(result);
            PrintResult(result, file.Lines.Count);

            if (isStress && result.IsSuccess)
            {
                PrintTrace(line.Expression);
            }
        }

        totalWatch.Stop();

        string reportPath;

        try
        {
            reportPath = _reports.WritePostfixReport(selected.Path, startedAt, results);
        }
        catch (Exception exception)
        {
            ConsoleTheme.Error($"Не удалось сохранить CSV: {exception.Message}");
            ConsoleTheme.Pause();
            return;
        }

        ConsoleTheme.Separator("итоги");
        ConsoleTheme.Success("Все выражения обработаны.");

        var successful = results.Count(result => result.IsSuccess);
        var failed = results.Count - successful;
        var totalTokens = results.Sum(result => result.Complexity.OperationCount);
        var totalExecution = results.Sum(result => result.Milliseconds);

        ConsoleTheme.KeyValue("Успешно", successful.ToString());
        ConsoleTheme.KeyValue("С ошибкой", failed.ToString());
        ConsoleTheme.KeyValue("Токенов всего", totalTokens.ToString());
        ConsoleTheme.KeyValue("Суммарно вычисление", $"{CsvReportWriter.FormatMilliseconds(totalExecution)} мс");

        if (totalTokens > 0)
        {
            ConsoleTheme.KeyValue(
                "Среднее на токен",
                $"{CsvReportWriter.FormatMilliseconds(totalExecution / totalTokens)} мс");
        }

        ConsoleTheme.KeyValue("Всё вместе", $"{CsvReportWriter.FormatMilliseconds(totalWatch.Elapsed.TotalMilliseconds)} мс");
        ConsoleTheme.KeyValue("Сложность", "O(n) по времени, O(n) по памяти для каждого выражения");
        ConsoleTheme.KeyValue("CSV", reportPath);
        ConsoleTheme.Muted("  Формат отчёта: количество операций; время (мс); расчёт сложности.");

        wallWatch.Stop();
        ConsoleTheme.KeyValue(
            "Полное время (с выводом)",
            $"{CsvReportWriter.FormatMilliseconds(wallWatch.Elapsed.TotalMilliseconds)} мс");

    }

    private void PrintTrace(string expression)
    {
        ConsoleTheme.Muted("         трассировка: токен → глубина стека");
        ConsoleTheme.Color(ConsoleColor.DarkGray);

        _evaluator.Trace(expression, (index, token, depth) =>
            Console.WriteLine($"  {index,9}: {token,-12} стек={depth}"));

        ConsoleTheme.Color(ConsoleColor.Gray);
        Console.WriteLine();
    }

    private static void PrintResult(PostfixLineResult result, int total)
    {
        ConsoleTheme.Color(ConsoleColor.DarkGray);
        Console.Write($"  [{result.Number,4}/{total}] ");
        ConsoleTheme.Color(ConsoleColor.Gray);

        var preview = result.Expression.Length <= 180
            ? result.Expression
            : result.Expression[..180] + $" … ({result.Expression.Length} символов)";

        Console.WriteLine($"строка {result.SourceLine}: {preview}");

        if (result.IsSuccess)
        {
            ConsoleTheme.Accent($"         = {CsvReportWriter.FormatValue(result.Value)}");
        }
        else
        {
            ConsoleTheme.Error($"ошибка: {result.Error}");
        }

        ConsoleTheme.Muted(
            $"         {TextFormat.Count(result.Complexity.OperationCount, "операция", "операции", "операций")} : " +
            $"{CsvReportWriter.FormatMilliseconds(result.Milliseconds)} мс : " +
            result.Complexity.Description);
        Console.WriteLine();
    }
}
