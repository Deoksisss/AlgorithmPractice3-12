using System.Collections.Generic;

namespace DataStructuresVisualizer.Services;

public sealed record LinkedListInputFile(
    string Name,
    string Path,
    IReadOnlyList<int> Left,
    IReadOnlyList<int> Right,
    string? Error);

public static class LinkedListFileService
{
    public static IReadOnlyList<LinkedListInputFile> LoadFiles()
    {
        var folder = Path.Combine(FindProjectRoot(), "LinkedListInput");

        if (!Directory.Exists(folder))
        {
            return Array.Empty<LinkedListInputFile>();
        }

        return Directory
            .EnumerateFiles(folder, "*.txt", SearchOption.TopDirectoryOnly)
            .Where(path => !Path.GetFileName(path).Contains("EXTREME", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
            .Select(ReadFile)
            .ToArray();
    }

    private static LinkedListInputFile ReadFile(string path)
    {
        var name = Path.GetFileName(path);

        try
        {
            var lines = File.ReadAllLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (lines.Length != 2)
            {
                return new LinkedListInputFile(name, path, [], [], "Ожидаются две строки: список L и список E.");
            }

            if (!TryParseValues(lines[0], out var left) || !TryParseValues(lines[1], out var right))
            {
                return new LinkedListInputFile(name, path, [], [], "Обе строки должны содержать целые числа.");
            }

            return new LinkedListInputFile(name, path, left, right, null);
        }
        catch (Exception exception)
        {
            return new LinkedListInputFile(name, path, [], [], exception.Message);
        }
    }

    private static bool TryParseValues(string line, out IReadOnlyList<int> values)
    {
        var result = new List<int>();

        foreach (var token in line.Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(token, out var value))
            {
                values = Array.Empty<int>();
                return false;
            }

            result.Add(value);
        }

        values = result;
        return true;
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
