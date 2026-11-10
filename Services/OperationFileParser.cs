using StackQueueLab.Models;

namespace StackQueueLab.Services;

/// <summary>
/// Строгий построчный разбор файлов операций 1–5.
/// </summary>
public sealed class OperationFileParser
{
    public OperationFileParseResult Parse(string path)
    {
        var sets = new List<OperationSet>();
        var issues = new List<FileIssue>();

        if (!File.Exists(path))
        {
            issues.Add(new FileIssue(0, string.Empty, "Файл не найден."));
            return new OperationFileParseResult(path, sets, issues);
        }

        string[] lines;

        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception exception)
        {
            issues.Add(new FileIssue(0, string.Empty, $"Не удалось прочитать файл: {exception.Message}"));
            return new OperationFileParseResult(path, sets, issues);
        }

        var setNumber = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            setNumber++;
            var operations = new List<CollectionOperation>();
            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            for (var tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                var token = tokens[tokenIndex];

                if (token.StartsWith("1,", StringComparison.Ordinal))
                {
                    var value = token[2..].Trim();

                    if (value.Length == 0)
                    {
                        issues.Add(new FileIssue(
                            i + 1,
                            token,
                            "После запятой должно быть значение для Push/Enqueue."));
                        continue;
                    }

                    if (value.Contains(','))
                    {
                        issues.Add(new FileIssue(
                            i + 1,
                            token,
                            "Лишняя информация: значение не должно содержать запятых."));
                        continue;
                    }

                    operations.Add(new CollectionOperation(1, value));
                    continue;
                }

                if (!int.TryParse(token, out var code) || code is < 1 or > 5)
                {
                    issues.Add(new FileIssue(
                        i + 1,
                        token,
                        "Лишняя или неизвестная информация. Допустимы только операции 1–5."));
                    continue;
                }

                if (code == 1)
                {
                    issues.Add(new FileIssue(
                        i + 1,
                        token,
                        "Для операции 1 значение указывается через запятую: 1,значение."));
                    continue;
                }

                operations.Add(new CollectionOperation(code, null));
            }

            if (operations.Count > 0)
            {
                sets.Add(new OperationSet(setNumber, i + 1, operations));
            }
        }

        if (sets.Count == 0 && issues.Count == 0)
        {
            issues.Add(new FileIssue(0, string.Empty, "Файл пуст: нет ни одного набора операций."));
        }

        return new OperationFileParseResult(path, sets, issues);
    }
}
