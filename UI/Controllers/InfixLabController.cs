using System.Diagnostics;
using StackQueueLab.Models;
using StackQueueLab.Services;

namespace StackQueueLab.UI.Controllers;

public sealed class InfixLabController
{
    private readonly ProjectPaths _paths;
    private readonly FilePicker _picker;
    private readonly ExpressionFileService _expressions;
    private readonly CsvReportWriter _reports;
    private readonly InfixToPostfixConverter _converter = new();
    private readonly PostfixEvaluator _evaluator = new();
    private readonly ComplexityAnalyzer _complexity = new();

    public InfixLabController(
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
            ConsoleTheme.BeginScreen("Главное меню / Инфикс → постфикс");
            ConsoleTheme.Title("Инфиксная запись", "Формула, понятная человеку → постфиксная запись");
            ConsoleTheme.MenuOption("1", "Ввести формулу вручную", "сохранение в InfixInput");
            ConsoleTheme.MenuOption("2", "Обработать файл выражений", "по одному выражению на строку");
            ConsoleTheme.MenuOption("0", "Назад");
            ConsoleTheme.Rule();

            var choice = ConsoleInput.ReadMenuChoice(2);

            if (choice == 0)
            {
                return;
            }

            if (choice == 1)
            {
                EnterManual();
            }
            else
            {
                var file = _picker.PickInfixFile(
                    "Главное меню / Инфикс → постфикс / Файл",
                    _paths.InfixInput);

                if (file is not null)
                {
                    ExecuteFile(file);
                    ConsoleTheme.Pause("Enter — вернуться в меню");
                }
            }
        }
    }

    private void EnterManual()
    {
        var wallWatch = Stopwatch.StartNew();
        ConsoleTheme.BeginScreen("Главное меню / Инфикс → постфикс / Ввод");
        ConsoleTheme.Title("Ручной ввод", "Имя файла и формула сохраняются в InfixInput");

        string path;
        string name;

        while (true)
        {
            name = ConsoleInput.ReadLine("Имя файла");

            try
            {
                path = _expressions.ResolveInfixPath(_paths.InfixInput, name);
                break;
            }
            catch (ExpressionSyntaxException exception)
            {
                ConsoleTheme.Error(exception.Message);
            }
        }

        if (File.Exists(path) && !ConsoleInput.Confirm("Файл уже существует. Перезаписать?"))
        {
            return;
        }

        string expression;
        string postfix;

        while (true)
        {
            expression = ConsoleInput.ReadLine("Инфиксное выражение");

            try
            {
                postfix = _converter.Convert(expression);
                break;
            }
            catch (ExpressionSyntaxException exception)
            {
                ConsoleTheme.Error(exception.Message);

                if (!ConsoleInput.Confirm("Повторить ввод выражения?"))
                {
                    return;
                }
            }
        }

        try
        {
            path = _expressions.SaveInfix(_paths.InfixInput, name, expression);
        }
        catch (Exception exception)
        {
            ConsoleTheme.Error($"Не удалось сохранить файл: {exception.Message}");
            ConsoleTheme.Pause();
            return;
        }

        var startedAt = DateTime.Now;
        var watch = Stopwatch.StartNew();
        double? value = null;
        string? error = null;

        try
        {
            postfix = _converter.Convert(expression);
            value = _evaluator.Evaluate(postfix);
        }
        catch (ExpressionSyntaxException exception)
        {
            error = exception.Message;
            postfix = "—";
        }
        catch (ExpressionEvaluationException exception)
        {
            error = exception.Message;
        }

        watch.Stop();

        var complexity = _complexity.AnalyzeInfix(expression);
        var result = new InfixLineResult(
            1,
            1,
            expression,
            postfix,
            value,
            error,
            watch.Elapsed.TotalMilliseconds,
            complexity);

        var results = new List<InfixLineResult> { result };
        string reportPath;

        try
        {
            reportPath = _reports.WriteInfixReport(path, startedAt, results);
        }
        catch (Exception exception)
        {
            ConsoleTheme.Error($"Не удалось сохранить CSV: {exception.Message}");
            reportPath = "не сохранён";
        }

        ConsoleTheme.Section("Результат");
        ConsoleTheme.Success($"Формула сохранена: {Path.GetRelativePath(_paths.Root, path)}");
        ConsoleTheme.KeyValue("Инфиксная", expression);
        ConsoleTheme.KeyValue("Постфиксная", postfix);

        if (result.Value is not null)
        {
            ConsoleTheme.KeyValue("Значение", CsvReportWriter.FormatValue(result.Value));
        }

        if (result.Error is not null)
        {
            ConsoleTheme.Error(result.Error);
        }

        ConsoleTheme.KeyValue("Время", $"{CsvReportWriter.FormatMilliseconds(result.Milliseconds)} мс");
        ConsoleTheme.KeyValue("Сложность", result.Complexity.Description);
        ConsoleTheme.KeyValue("CSV", reportPath);

        wallWatch.Stop();
        ConsoleTheme.KeyValue(
            "Полное время (с выводом)",
            $"{CsvReportWriter.FormatMilliseconds(wallWatch.Elapsed.TotalMilliseconds)} мс");

        ConsoleTheme.Pause();
    }

    private void ExecuteFile(ExpressionFile selected)
    {
        var startedAt = DateTime.Now;
        var wallWatch = Stopwatch.StartNew();
        var isStress = Path.GetFileName(selected.Path)
            .Contains("EXTREME", StringComparison.OrdinalIgnoreCase);

        ConsoleTheme.BeginScreen("Главное меню / Инфикс → постфикс / Файл");
        ConsoleTheme.Title(Path.GetFileName(selected.Path), "Инфикс → постфикс → вычисление");

        if (isStress)
        {
            ConsoleTheme.Warning("Стресс-файл: вывод постфиксной записи по токенам может занять больше минуты.");
        }

        var totalWatch = Stopwatch.StartNew();
        var readingWatch = Stopwatch.StartNew();
        var file = _expressions.LoadInfix(selected.Path);
        readingWatch.Stop();

        if (!file.IsValid)
        {
            ConsoleTheme.Error("Файл изменился или содержит ошибки. Выполнение отменено.");
            ConsoleTheme.Pause("Enter — назад");
            return;
        }

        ConsoleTheme.KeyValue("Выражений", file.Lines.Count.ToString());
        ConsoleTheme.KeyValue("Чтение и проверка", $"{CsvReportWriter.FormatMilliseconds(readingWatch.Elapsed.TotalMilliseconds)} мс");
        ConsoleTheme.Section("Обработка");

        var results = new List<InfixLineResult>(file.Lines.Count);

        foreach (var line in file.Lines)
        {
            var watch = Stopwatch.StartNew();
            string? postfix = null;
            double? value = null;
            string? error = null;

            try
            {
                postfix = _converter.Convert(line.Expression);
                value = _evaluator.Evaluate(postfix);
            }
            catch (ExpressionSyntaxException exception)
            {
                error = exception.Message;
            }
            catch (ExpressionEvaluationException exception)
            {
                error = exception.Message;
            }

            watch.Stop();

            var result = new InfixLineResult(
                line.Number,
                line.SourceLine,
                line.Expression,
                postfix,
                value,
                error,
                watch.Elapsed.TotalMilliseconds,
                _complexity.AnalyzeInfix(line.Expression));

            results.Add(result);
            PrintResult(result, file.Lines.Count);

            if (isStress && result.IsSuccess && result.Postfix is not null)
            {
                PrintPostfixTrace(result.Postfix);
            }
        }

        totalWatch.Stop();

        string reportPath;

        try
        {
            reportPath = _reports.WriteInfixReport(selected.Path, startedAt, results);
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

        ConsoleTheme.KeyValue("Успешно", successful.ToString());
        ConsoleTheme.KeyValue("С ошибкой", failed.ToString());
        ConsoleTheme.KeyValue("Токенов всего", totalTokens.ToString());
        ConsoleTheme.KeyValue("Время обработки", $"{CsvReportWriter.FormatMilliseconds(totalWatch.Elapsed.TotalMilliseconds)} мс");
        ConsoleTheme.KeyValue("Сложность", "O(n) по времени, O(n) по памяти для каждого выражения");
        ConsoleTheme.KeyValue("CSV", reportPath);

        wallWatch.Stop();
        ConsoleTheme.KeyValue(
            "Полное время (с выводом)",
            $"{CsvReportWriter.FormatMilliseconds(wallWatch.Elapsed.TotalMilliseconds)} мс");

    }

    private static void PrintPostfixTrace(string postfix)
    {
        ConsoleTheme.Muted("         постфикс по токенам:");
        ConsoleTheme.Color(ConsoleColor.DarkGray);

        var index = 0;
        var token = new System.Text.StringBuilder();

        foreach (var character in postfix)
        {
            if (char.IsWhiteSpace(character))
            {
                if (token.Length > 0)
                {
                    Console.WriteLine($"  {index++,9}: {token}");
                    token.Clear();
                }
            }
            else
            {
                token.Append(character);
            }
        }

        if (token.Length > 0)
        {
            Console.WriteLine($"  {index,9}: {token}");
        }

        ConsoleTheme.Color(ConsoleColor.Gray);
        Console.WriteLine();
    }

    private static void PrintResult(InfixLineResult result, int total)
    {
        ConsoleTheme.Color(ConsoleColor.DarkGray);
        Console.Write($"  [{result.Number,4}/{total}] ");
        ConsoleTheme.Color(ConsoleColor.Gray);

        var infixPreview = result.Infix.Length <= 180
            ? result.Infix
            : result.Infix[..180] + $" … ({result.Infix.Length} символов)";

        Console.WriteLine(infixPreview);

        if (result.Postfix is not null)
        {
            var postfixPreview = result.Postfix.Length <= 180
                ? result.Postfix
                : result.Postfix[..180] + $" … ({result.Postfix.Length} символов)";

            ConsoleTheme.Accent($"         → {postfixPreview}");
        }

        if (result.IsSuccess)
        {
            ConsoleTheme.Muted(
                $"         = {CsvReportWriter.FormatValue(result.Value)} · " +
                $"{CsvReportWriter.FormatMilliseconds(result.Milliseconds)} мс · {result.Complexity.Description}");
        }
        else
        {
            ConsoleTheme.Error($"         {result.Error}");
        }
    }
}
