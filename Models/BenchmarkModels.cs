namespace StackQueueLab.Models;

public sealed record BenchmarkRow(
    string FileName,
    int SetNumber,
    int OperationCount,
    double LinkedMilliseconds,
    double StandardMilliseconds)
{
    public double DifferenceMilliseconds => StandardMilliseconds - LinkedMilliseconds;

    public double Ratio => LinkedMilliseconds <= 0.0
        ? 0.0
        : StandardMilliseconds / LinkedMilliseconds;
}

public sealed record BenchmarkFileSummary(
    string FileName,
    int SetCount,
    int TotalOperations,
    double LinkedMilliseconds,
    double StandardMilliseconds)
{
    public double Ratio => LinkedMilliseconds <= 0.0
        ? 0.0
        : StandardMilliseconds / LinkedMilliseconds;
}

public sealed record BenchmarkReport(
    IReadOnlyList<BenchmarkRow> Rows,
    IReadOnlyList<BenchmarkFileSummary> Files,
    string ReportPath);
