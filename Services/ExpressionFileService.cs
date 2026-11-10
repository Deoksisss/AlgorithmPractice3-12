using System.Text;
using StackQueueLab.Models;

namespace StackQueueLab.Services;

/// <summary>
/// Чтение и проверка файлов выражений, сохранение инфиксных формул.
/// </summary>
public sealed class ExpressionFileService
{
    private readonly PostfixValidator _postfixValidator = new();
    private readonly InfixToPostfixConverter _converter = new();

    public ExpressionFile LoadPostfix(string path)
    {
        var lines = new List<PostfixFileLine>();
        var issues = new List<ExpressionFileIssue>();

        if (!File.Exists(path))
        {
            issues.Add(new ExpressionFileIssue(0, "Файл не найден."));
            return new ExpressionFile(path, lines, issues);
        }

        string[] content;

        try
        {
            content = File.ReadAllLines(path);
        }
        catch (Exception exception)
        {
            issues.Add(new ExpressionFileIssue(0, $"Не удалось прочитать файл: {exception.Message}"));
            return new ExpressionFile(path, lines, issues);
        }

        for (var i = 0; i < content.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(content[i]))
            {
                continue;
            }

            var expression = content[i].Trim();
            var validation = _postfixValidator.Validate(expression);

            if (!validation.IsValid)
            {
                issues.Add(new ExpressionFileIssue(
                    i + 1,
                    validation.Error ?? "Некорректное постфиксное выражение."));
                continue;
            }

            lines.Add(new PostfixFileLine(lines.Count + 1, i + 1, expression));
        }

        if (lines.Count == 0 && issues.Count == 0)
        {
            issues.Add(new ExpressionFileIssue(0, "Файл не содержит выражений."));
        }

        return new ExpressionFile(path, lines, issues);
    }

    public ExpressionFile LoadInfix(string path)
    {
        var lines = new List<PostfixFileLine>();
        var issues = new List<ExpressionFileIssue>();

        if (!File.Exists(path))
        {
            issues.Add(new ExpressionFileIssue(0, "Файл не найден."));
            return new ExpressionFile(path, lines, issues);
        }

        string[] content;

        try
        {
            content = File.ReadAllLines(path);
        }
        catch (Exception exception)
        {
            issues.Add(new ExpressionFileIssue(0, $"Не удалось прочитать файл: {exception.Message}"));
            return new ExpressionFile(path, lines, issues);
        }

        for (var i = 0; i < content.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(content[i]))
            {
                continue;
            }

            var expression = content[i].Trim();

            try
            {
                _converter.Convert(expression);
            }
            catch (ExpressionSyntaxException exception)
            {
                issues.Add(new ExpressionFileIssue(i + 1, exception.Message));
                continue;
            }

            lines.Add(new PostfixFileLine(lines.Count + 1, i + 1, expression));
        }

        if (lines.Count == 0 && issues.Count == 0)
        {
            issues.Add(new ExpressionFileIssue(0, "Файл не содержит выражений."));
        }

        return new ExpressionFile(path, lines, issues);
    }

    public string SaveInfix(string directory, string requestedName, string expression)
    {
        var fileName = NormalizeFileName(requestedName);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, expression.Trim() + Environment.NewLine, new UTF8Encoding(true));
        return path;
    }

    public string ResolveInfixPath(string directory, string requestedName) =>
        Path.Combine(directory, NormalizeFileName(requestedName));

    public static bool IsValidInfixName(string requestedName)
    {
        try
        {
            _ = NormalizeFileName(requestedName);
            return true;
        }
        catch (ExpressionSyntaxException)
        {
            return false;
        }
    }

    private static string NormalizeFileName(string requestedName)
    {
        var name = Path.GetFileName((requestedName ?? string.Empty).Trim());

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ExpressionSyntaxException("Имя файла не может быть пустым.");
        }

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            name += ".txt";
        }

        return name;
    }
}
