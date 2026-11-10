namespace StackQueueLab.Services;

public sealed record FileCatalogItem(string Path, string Name, long SizeBytes);

public static class FileCatalog
{
    public static IReadOnlyList<FileCatalogItem> ListTextFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return Array.Empty<FileCatalogItem>();
        }

        return Directory
            .EnumerateFiles(directory, "*.txt", SearchOption.TopDirectoryOnly)
            .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new FileCatalogItem(path, info.Name, info.Length);
            })
            .ToArray();
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} Б";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:0.0} КБ";
        }

        return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
    }
}
