namespace DataStructuresVisualizer.Models;

public readonly record struct MazePoint(int X, int Y);

public enum MazeFrameType
{
    PushStart,
    Explore,
    VisitNeighbor,
    PopCurrent,
    DeadEnd,
    Found,
    FinalPathStep,
    NoPath
}

public sealed record MazeFrame(
    MazeFrameType Type,
    MazePoint Point,
    IReadOnlyList<MazePoint> Stack,
    string Message);

public sealed class MazeGrid
{
    public MazeGrid(char[,] cells, MazePoint start, MazePoint goal)
    {
        Cells = cells;
        Start = start;
        Goal = goal;
        Width = cells.GetLength(0);
        Height = cells.GetLength(1);
    }

    public char[,] Cells { get; }

    public int Width { get; }

    public int Height { get; }

    public MazePoint Start { get; }

    public MazePoint Goal { get; }

    public bool InBounds(in MazePoint point) =>
        point.X >= 0 && point.Y >= 0 && point.X < Width && point.Y < Height;

    public bool IsOpen(in MazePoint point) =>
        InBounds(point) && Cells[point.X, point.Y] != '#';

    public bool IsWall(in MazePoint point) =>
        InBounds(point) && Cells[point.X, point.Y] == '#';
}
