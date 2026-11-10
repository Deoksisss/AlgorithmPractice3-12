namespace StackQueueLab.Models;

public sealed record FileIssue(int Line, string Token, string Message);

/// <summary>
/// Результат разбора файла операций. Файл считается корректным только при
/// отсутствии замечаний и наличии хотя бы одного набора.
/// </summary>
public sealed class OperationFileParseResult
{
    public OperationFileParseResult(
        string path,
        IReadOnlyList<OperationSet> sets,
        IReadOnlyList<FileIssue> issues)
    {
        Path = path;
        Sets = sets;
        Issues = issues;
    }

    public string Path { get; }

    public IReadOnlyList<OperationSet> Sets { get; }

    public IReadOnlyList<FileIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0 && Sets.Count > 0;

    public int TotalOperations => Sets.Sum(set => set.Count);

    public OperationFile? File => IsValid
        ? new OperationFile(Path, Sets, TotalOperations)
        : null;
}

public sealed record OperationFile(
    string Path,
    IReadOnlyList<OperationSet> Sets,
    int TotalOperations);
