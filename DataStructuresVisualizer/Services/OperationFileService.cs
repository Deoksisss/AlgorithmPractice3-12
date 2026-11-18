using System.Collections.Generic;
using System.Diagnostics;

namespace DataStructuresVisualizer.Services;

public sealed record ImportedOperation(int Code, string? Value);

public sealed record ImportedOperationSet(
    int Number,
    int SourceLine,
    IReadOnlyList<ImportedOperation> Operations);

public sealed record ImportedOperationFile(
    string Name,
    string Path,
    IReadOnlyList<ImportedOperationSet> Sets,
    string? Error,
    double ReadMilliseconds = 0);

public static class OperationFileService
{
    public static IReadOnlyList<ImportedOperationFile> LoadFiles(string folderName)
    {
        var root = FindProjectRoot();
        var folder = Path.Combine(root, folderName);

        if (!Directory.Exists(folder))
        {
            return Array.Empty<ImportedOperationFile>();
        }

        return Directory
            .EnumerateFiles(folder, "*.txt", SearchOption.TopDirectoryOnly)
            .Where(path => !Path.GetFileName(path).Contains("EXTREME", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
            .Select(ReadFileTimed)
            .ToArray();
    }

    private static ImportedOperationFile ReadFileTimed(string path)
    {
        var watch = Stopwatch.StartNew();
        var file = ReadFile(path);
        watch.Stop();
        return file with { ReadMilliseconds = watch.Elapsed.TotalMilliseconds };
    }

    private static ImportedOperationFile ReadFile(string path)
    {
        var name = Path.GetFileName(path);

        try
        {
            var sets = new List<ImportedOperationSet>();
            var lines = File.ReadAllLines(path);

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }

                var tokens = lines[lineIndex]
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                var operations = new List<ImportedOperation>();

                foreach (var token in tokens)
                {
                    if (token.StartsWith("1,", StringComparison.Ordinal))
                    {
                        var value = token[2..].Trim();

                        if (value.Length == 0)
                        {
                            return new ImportedOperationFile(
                                name,
                                path,
                                sets,
                                $"Строка {lineIndex + 1}: после 1, нет значения.");
                        }

                        operations.Add(new ImportedOperation(1, value));
                        continue;
                    }

                    if (!int.TryParse(token, out var code) || code is < 2 or > 5)
                    {
                        return new ImportedOperationFile(
                            name,
                            path,
                            sets,
                            $"Строка {lineIndex + 1}: лишняя информация «{token}».");
                    }

                    operations.Add(new ImportedOperation(code, null));
                }

                if (operations.Count > 0)
                {
                    sets.Add(new ImportedOperationSet(sets.Count + 1, lineIndex + 1, operations));
                }
            }

            if (sets.Count == 0)
            {
                return new ImportedOperationFile(name, path, sets, "Файл не содержит наборов.");
            }

            return new ImportedOperationFile(name, path, sets, null);
        }
        catch (Exception exception)
        {
            return new ImportedOperationFile(name, path, [], exception.Message);
        }
    }

    private static string FindProjectRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "StackQueueLab.csproj")) ||
                    File.Exists(Path.Combine(directory.FullName, "DataStructuresVisualizer.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}
