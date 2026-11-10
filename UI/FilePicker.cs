using StackQueueLab.Models;
using StackQueueLab.Services;

namespace StackQueueLab.UI;

/// <summary>
/// Список файлов с ленивой проверкой: файл разбирается только после выбора.
/// Это принципиально для EXTREME-файлов с миллионами токенов.
/// </summary>
public sealed class FilePicker
{
    private readonly OperationFileParser _operationParser = new();
    private readonly ExpressionFileService _expressionService = new();

    public OperationFile? PickOperationFile(string title, string directory)
    {
        while (true)
        {
            ConsoleTheme.BeginScreen(title);
            ConsoleTheme.Title("Выбор файла", $"Папка: {directory}");

            var files = FileCatalog.ListTextFiles(directory);

            if (files.Count == 0)
            {
                ConsoleTheme.Warning("В папке нет файлов .txt.");
                ConsoleTheme.Text("Добавьте файл с наборами операций и повторите попытку.");
                ConsoleTheme.Pause("Enter — назад");
                return null;
            }

            for (var i = 0; i < files.Count; i++)
            {
                PrintFileRow(i + 1, files[i]);
            }

            ConsoleTheme.Rule();
            ConsoleTheme.Muted("  0 — назад    R — обновить список");
            ConsoleTheme.Muted("  Файл проверяется после выбора.");

            var choice = ConsoleInput.ReadMenuChoice(files.Count);

            if (choice == 0)
            {
                return null;
            }

            if (choice == -2)
            {
                continue;
            }

            var selected = files[choice - 1];
            ConsoleTheme.Muted($"  Проверка файла: {selected.Name} …");

            var parsed = _operationParser.Parse(selected.Path);

            if (!parsed.IsValid)
            {
                ShowOperationIssues(title, parsed);
                continue;
            }

            return parsed.File;
        }
    }

    public ExpressionFile? PickPostfixFile(string title, string directory) =>
        PickExpressionFile(title, directory, _expressionService.LoadPostfix);

    public ExpressionFile? PickInfixFile(string title, string directory) =>
        PickExpressionFile(title, directory, _expressionService.LoadInfix);

    private static ExpressionFile? PickExpressionFile(
        string title,
        string directory,
        Func<string, ExpressionFile> loader)
    {
        while (true)
        {
            ConsoleTheme.BeginScreen(title);
            ConsoleTheme.Title("Выбор файла", $"Папка: {directory}");

            var files = FileCatalog.ListTextFiles(directory);

            if (files.Count == 0)
            {
                ConsoleTheme.Warning("В папке нет файлов .txt.");
                ConsoleTheme.Pause("Enter — назад");
                return null;
            }

            for (var i = 0; i < files.Count; i++)
            {
                PrintFileRow(i + 1, files[i]);
            }

            ConsoleTheme.Rule();
            ConsoleTheme.Muted("  0 — назад    R — обновить список");
            ConsoleTheme.Muted("  Файл проверяется после выбора.");

            var choice = ConsoleInput.ReadMenuChoice(files.Count);

            if (choice == 0)
            {
                return null;
            }

            if (choice == -2)
            {
                continue;
            }

            var selected = files[choice - 1];
            ConsoleTheme.Muted($"  Проверка файла: {selected.Name} …");

            var loaded = loader(selected.Path);

            if (!loaded.IsValid)
            {
                ShowExpressionIssues(title, loaded);
                continue;
            }

            return loaded;
        }
    }

    private static void PrintFileRow(int number, FileCatalogItem file)
    {
        var isStress = file.Name.Contains("EXTREME", StringComparison.OrdinalIgnoreCase);

        Console.Write("  ");
        ConsoleTheme.Color(ConsoleColor.DarkCyan);
        Console.Write($"[{number,2}]");
        ConsoleTheme.Color(ConsoleColor.Gray);
        Console.Write($"  {Shorten(file.Name, 52),-52}");
        ConsoleTheme.Color(ConsoleColor.DarkGray);
        Console.Write($"{FileCatalog.FormatSize(file.SizeBytes),9}");

        if (isStress)
        {
            ConsoleTheme.Color(ConsoleColor.Yellow);
            Console.Write("   стресс");
        }

        Console.WriteLine();
        ConsoleTheme.Color(ConsoleColor.Gray);
    }

    private static void ShowOperationIssues(string title, OperationFileParseResult parsed)
    {
        ConsoleTheme.BeginScreen(title);
        ConsoleTheme.Title("Файл отклонён");
        ConsoleTheme.Error("Файл содержит лишнюю или некорректную информацию.");
        PrintIssues(parsed.Issues.Select(issue =>
            $"строка {issue.Line}, токен «{issue.Token}»: {issue.Message}"));
        ConsoleTheme.Pause("Enter — выбрать другой файл");
    }

    private static void ShowExpressionIssues(string title, ExpressionFile expressionFile)
    {
        ConsoleTheme.BeginScreen(title);
        ConsoleTheme.Title("Файл отклонён");
        ConsoleTheme.Error("Файл содержит лишнюю или некорректную информацию.");
        PrintIssues(expressionFile.Issues.Select(issue =>
            $"строка {issue.Line}: {issue.Message}"));
        ConsoleTheme.Pause("Enter — выбрать другой файл");
    }

    private static void PrintIssues(IEnumerable<string> issues)
    {
        var shown = 0;

        foreach (var issue in issues)
        {
            if (shown == 8)
            {
                ConsoleTheme.Muted("  … показаны первые 8 замечаний");
                break;
            }

            ConsoleTheme.Warning(issue);
            shown++;
        }
    }

    private static string Shorten(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
