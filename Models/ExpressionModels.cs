namespace StackQueueLab.Models;

public sealed record ComplexityInfo(int OperationCount, int OperandCount, int OperatorCount)
{
    public string TimeComplexity => "O(n)";

    public string SpaceComplexity => "O(n)";

    public string Description => $"O(n), память {SpaceComplexity}";
}

public sealed record ExpressionFileIssue(int Line, string Message);

public sealed record PostfixFileLine(int Number, int SourceLine, string Expression);

/// <summary>
/// Файл с построчными выражениями (постфиксными или инфиксными).
/// </summary>
public sealed record ExpressionFile(
    string Path,
    IReadOnlyList<PostfixFileLine> Lines,
    IReadOnlyList<ExpressionFileIssue> Issues)
{
    public bool IsValid => Issues.Count == 0 && Lines.Count > 0;
}

public sealed record PostfixLineResult(
    int Number,
    int SourceLine,
    string Expression,
    double? Value,
    string? Error,
    double Milliseconds,
    ComplexityInfo Complexity)
{
    public bool IsSuccess => Error is null;
}

public sealed record InfixLineResult(
    int Number,
    int SourceLine,
    string Infix,
    string? Postfix,
    double? Value,
    string? Error,
    double Milliseconds,
    ComplexityInfo Complexity)
{
    public bool IsSuccess => Error is null;
}
