using System.Globalization;
using System.Text;
using StackQueueLab.Models;

namespace StackQueueLab.Services;

/// <summary>
/// Формирует CSV с разделителем «;» (удобно для русской локали Excel).
/// Имя отчёта: исходный_файл-дата_время.csv.
/// </summary>
public sealed class CsvReportWriter
{
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private readonly ProjectPaths _paths;

    public CsvReportWriter(ProjectPaths paths)
    {
        _paths = paths;
    }

    public string WriteStackReport(
        string sourcePath,
        DateTime startedAt,
        IReadOnlyList<SetExecutionResult> sets)
    {
        var lines = new List<string>
        {
            "Количество операций;Время выполнения (мс)"
        };

        lines.AddRange(sets.Select(set =>
            $"{set.OperationCount};{FormatMilliseconds(set.Milliseconds)}"));

        return WriteReport(sourcePath, startedAt, lines);
    }

    public string WriteQueueReport(
        string sourcePath,
        DateTime startedAt,
        IReadOnlyList<QueueSetTiming> sets)
    {
        var lines = new List<string>
        {
            "Количество операций;Связанная очередь (мс);Стандартная Queue (мс)"
        };

        lines.AddRange(sets.Select(set =>
            $"{set.OperationCount};{FormatMilliseconds(set.LinkedMilliseconds)};{FormatMilliseconds(set.StandardMilliseconds)}"));

        return WriteReport(sourcePath, startedAt, lines);
    }

    public string WritePostfixReport(
        string sourcePath,
        DateTime startedAt,
        IReadOnlyList<PostfixLineResult> results)
    {
        var lines = new List<string>
        {
            "Количество операций;Время выполнения (мс);Сложность"
        };

        lines.AddRange(results.Select(result =>
            $"{result.Complexity.OperationCount};{FormatMilliseconds(result.Milliseconds)};{Csv(result.Complexity.Description)}"));

        return WriteReport(sourcePath, startedAt, lines);
    }

    public string WriteInfixReport(
        string sourcePath,
        DateTime startedAt,
        IReadOnlyList<InfixLineResult> results)
    {
        var lines = new List<string>
        {
            "Инфиксная запись;Постфиксная запись;Результат;Время (мс);Сложность"
        };

        lines.AddRange(results.Select(result =>
            $"{Csv(result.Infix)};{Csv(result.Postfix ?? string.Empty)};{Csv(result.Error ?? FormatValue(result.Value))};{FormatMilliseconds(result.Milliseconds)};{Csv(result.Complexity.Description)}"));

        return WriteReport(sourcePath, startedAt, lines);
    }

    public string WriteBenchmarkReport(DateTime startedAt, IReadOnlyList<BenchmarkRow> rows)
    {
        var lines = new List<string>
        {
            "Файл;Набор;Количество операций;Связанная очередь (мс);Стандартная Queue (мс);Разница (мс);Отношение"
        };

        lines.AddRange(rows.Select(row =>
            $"{Csv(row.FileName)};{row.SetNumber};{row.OperationCount};" +
            $"{FormatMilliseconds(row.LinkedMilliseconds)};{FormatMilliseconds(row.StandardMilliseconds)};" +
            $"{FormatMilliseconds(row.DifferenceMilliseconds)};{row.Ratio.ToString("0.####", CultureInfo.InvariantCulture)}"));

        var path = Path.Combine(
            _paths.Output,
            $"queue_benchmark_{startedAt:yyyy-MM-dd_HH-mm-ss}.csv");

        path = MakeUnique(path);
        File.WriteAllLines(path, lines, Utf8WithBom);
        return path;
    }

    public static string FormatMilliseconds(double milliseconds) =>
        milliseconds.ToString("0.######", CultureInfo.InvariantCulture);

    public static string FormatValue(double? value) =>
        value is null ? "—" : NumberText.Format(value.Value);

    private string WriteReport(
        string sourcePath,
        DateTime startedAt,
        IReadOnlyList<string> lines)
    {
        var path = CreateReportPath(sourcePath, startedAt);
        File.WriteAllLines(path, lines, Utf8WithBom);
        return path;
    }

    private string CreateReportPath(string sourcePath, DateTime startedAt)
    {
        var sourceName = Path.GetFileName(sourcePath);
        var name = $"{sourceName}-{startedAt:yyyy-MM-dd_HH-mm-ss}.csv";
        var path = Path.Combine(_paths.Output, name);
        return MakeUnique(path);
    }

    private static string MakeUnique(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var counter = 2;

        while (true)
        {
            var candidate = Path.Combine(directory, $"{stem}-{counter}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }

    private static string Csv(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
