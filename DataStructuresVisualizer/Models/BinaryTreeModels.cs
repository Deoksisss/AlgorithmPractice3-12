namespace DataStructuresVisualizer.Models;

public sealed class BinaryTreeNode
{
    public required char Value { get; init; }

    public BinaryTreeNode? Left { get; set; }

    public BinaryTreeNode? Right { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public bool HasLeft => Left is not null;

    public bool HasRight => Right is not null;
}

public sealed record BinaryTreeBuildResult(
    BinaryTreeNode? Root,
    string? Error,
    int ParsedCharacters,
    int TotalCharacters);
