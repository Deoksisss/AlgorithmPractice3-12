namespace StackQueueLab.Services;

/// <summary>
/// Определяет корень проекта и создаёт рабочие папки. Поиск устойчив к запуску
/// как через dotnet run, так и из bin/Debug/netX.
/// </summary>
public sealed class ProjectPaths
{
    private const string ProjectMarker = "StackQueueLab.csproj";

    private ProjectPaths(string root)
    {
        Root = root;
        StackInput = Path.Combine(root, "StackInput");
        QueueInput = Path.Combine(root, "QueueInput");
        PostfixInput = Path.Combine(root, "PostfixInput");
        InfixInput = Path.Combine(root, "InfixInput");
        Output = Path.Combine(root, "output");

        Directory.CreateDirectory(StackInput);
        Directory.CreateDirectory(QueueInput);
        Directory.CreateDirectory(PostfixInput);
        Directory.CreateDirectory(InfixInput);
        Directory.CreateDirectory(Output);
    }

    public string Root { get; }

    public string StackInput { get; }

    public string QueueInput { get; }

    public string PostfixInput { get; }

    public string InfixInput { get; }

    public string Output { get; }

    public static ProjectPaths Create()
    {
        var root = FindRoot()
            ?? Directory.GetCurrentDirectory();

        return new ProjectPaths(root);
    }

    private static string? FindRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                var marker = Path.Combine(directory.FullName, ProjectMarker);
                if (File.Exists(marker))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
