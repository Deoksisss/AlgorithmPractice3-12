using DataStructuresVisualizer.Controls;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DataStructuresVisualizer.Models;
using DataStructuresVisualizer.Services;

namespace DataStructuresVisualizer.Views;

public partial class MazeView : UserControl
{
    private const double CellGap = 3;

    private readonly HashSet<MazePoint> visited = [];
    private readonly HashSet<MazePoint> deadEnds = [];
    private readonly HashSet<MazePoint> pathCells = [];
    private readonly Dictionary<MazePoint, Border> visuals = [];

    private DispatcherTimer? timer;
    private MazeGrid? grid;
    private IReadOnlyList<MazeFrame>? frames;
    private int frameIndex;
    private MazePoint? currentPoint;
    private HashSet<MazePoint> stackSnapshot = [];

    public MazeView()
    {
        InitializeComponent();
        ResponsiveColumns.Attach(this);
        DetachedFromVisualTree += (_, _) => StopAnimation();


        Loaded += (_, _) =>
        {
            GenerateRandom();
            RenderGrid();
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private Canvas MazeSurface => Require<Canvas>("MazeCanvas");

    private TextBox MazeTextBox => Require<TextBox>("MazeInput");
    private ListBox StackBox => Require<ListBox>("StackList");
    private TextBlock StatusLabel => Require<TextBlock>("StatusText");
    private TextBlock StepLabel => Require<TextBlock>("StepText");
    private Slider SpeedControl => Require<Slider>("SpeedSlider");

    private void GenerateButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        GenerateRandom();
        RenderGrid();
    }

    private void ManualLoadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();

        if (!MazeParser.TryParse(MazeTextBox.Text ?? string.Empty, out var parsed, out var error))
        {
            UpdateStatus($"Ошибка: {error}");
            return;
        }

        grid = parsed;
        RenderGrid();
        ResetVisualization();
        UpdateStatus("Лабиринт загружен. Можно запускать DFS.");
    }

    private void StartButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();

        if (grid is null)
        {
            UpdateStatus("Сначала загрузите или сгенерируйте лабиринт.");
            return;
        }

        frames = MazeSolver.Solve(grid);
        frameIndex = 0;
        ResetVisualization();

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(25, SpeedControl.Value))
        };
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void StopButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        UpdateStatus("Анимация остановлена.");
    }

    private void ResetButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        ResetVisualization();
        UpdateStatus("Визуализация сброшена.");
    }

    private void GenerateRandom()
    {
        StopAnimation();
        frames = null;
        ResetVisualization();
        grid = MazeGenerator.GenerateRandom();
        MazeTextBox.Text = ToText(grid);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!IsEffectivelyVisible) { StopAnimation(); return; }
        if (frames is null || timer is null || frameIndex >= frames.Count)
        {
            timer?.Stop();
            return;
        }

        ApplyFrame(frames[frameIndex]);
        frameIndex++;
        StepLabel.Text = $"{Math.Min(frameIndex, frames.Count)} / {frames.Count}";
        timer.Interval = TimeSpan.FromMilliseconds(Math.Max(25, SpeedControl.Value));

        if (frameIndex >= frames.Count)
        {
            timer.Stop();
        }
    }

    private void ApplyFrame(MazeFrame frame)
    {
        stackSnapshot = frame.Stack.ToHashSet();

        switch (frame.Type)
        {
            case MazeFrameType.PushStart:
            case MazeFrameType.VisitNeighbor:
                visited.Add(frame.Point);
                currentPoint = frame.Point;
                break;

            case MazeFrameType.Explore:
                currentPoint = frame.Point;
                break;

            case MazeFrameType.PopCurrent:
                currentPoint = frame.Stack.Count > 0 ? frame.Stack[^1] : null;
                break;

            case MazeFrameType.DeadEnd:
                deadEnds.Add(frame.Point);
                currentPoint = frame.Stack.Count > 0 ? frame.Stack[^1] : null;
                break;

            case MazeFrameType.Found:
                currentPoint = frame.Point;
                break;

            case MazeFrameType.FinalPathStep:
                pathCells.Add(frame.Point);
                break;

            case MazeFrameType.NoPath:
                currentPoint = null;
                break;
        }

        UpdateStatus(frame.Message);
        RenderStates();

        StackBox.ItemsSource = frame.Stack
            .Reverse()
            .Select((point, index) => index == 0 ? $"↑ ({point.X}, {point.Y})" : $"({point.X}, {point.Y})")
            .ToList();
    }

    private void RenderGrid()
    {
        var canvas = MazeSurface;


        canvas.Children.Clear();
        visuals.Clear();

        if (grid is null)
        {
            canvas.Width = 320;
            canvas.Height = 240;
            return;
        }
        const double cellSize = 32;
        canvas.Width = 16 + grid.Width * (cellSize + CellGap);
        canvas.Height = 16 + grid.Height * (cellSize + CellGap);
        const double startX = 8;
        const double startY = 8;

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var point = new MazePoint(x, y);
                var border = new Border
                {
                    Width = cellSize,
                    Height = cellSize
                };

                border.Classes.Add("cell");

                var cell = grid.Cells[x, y];

                switch (cell)
                {
                    case '#':
                        border.Classes.Add("wall");
                        break;
                    case 'S':
                        border.Classes.Add("start");
                        break;
                    case 'E':
                        border.Classes.Add("end");
                        break;
                    default:
                        border.Classes.Add("floor");
                        break;
                }

                if (cell is 'S' or 'E') border.Child = new TextBlock { Text = cell.ToString(), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                ToolTip.SetTip(border, point.ToString());
                Canvas.SetLeft(border, startX + x * (cellSize + CellGap));
                Canvas.SetTop(border, startY + y * (cellSize + CellGap));
                canvas.Children.Add(border);
                visuals[point] = border;
            }
        }
        RenderStates();
    }

    private void RenderStates()
    {
        if (grid is null)
        {
            return;
        }

        foreach (var (point, border) in visuals)
        {
            border.Classes.Remove("visited");
            border.Classes.Remove("stackCell");
            border.Classes.Remove("current");
            border.Classes.Remove("dead");
            border.Classes.Remove("pathCell");

            if (grid.IsWall(point))
            {
                continue;
            }

            if (pathCells.Contains(point))
            {
                border.Classes.Add("pathCell");
                continue;
            }

            if (currentPoint == point)
            {
                border.Classes.Add("current");
            }
            else if (stackSnapshot.Contains(point))
            {
                border.Classes.Add("stackCell");
            }
            else if (deadEnds.Contains(point))
            {
                border.Classes.Add("dead");
            }
            else if (visited.Contains(point))
            {
                border.Classes.Add("visited");
            }
        }
    }

    private void ResetVisualization()
    {
        visited.Clear();
        deadEnds.Clear();
        pathCells.Clear();
        stackSnapshot.Clear();
        currentPoint = null;
        frameIndex = 0;
        StepLabel.Text = "0 / 0";
        StackBox.ItemsSource = null;
        RenderStates();
    }

    private void StopAnimation()
    {
        timer?.Stop();
        timer = null;
    }

    private void UpdateStatus(string message)
    {
        StatusLabel.Text = message;
    }

    private T Require<T>(string name)
        where T : Control
    {
        var control = this.FindControl<T>(name);

        if (control is null)
        {
            throw new InvalidOperationException($"Контрол «{name}» не найден.");
        }

        return control;
    }

    private static string ToText(MazeGrid maze)
    {
        var lines = new List<string>(maze.Height);

        for (var y = 0; y < maze.Height; y++)
        {
            var chars = new char[maze.Width];

            for (var x = 0; x < maze.Width; x++)
            {
                chars[x] = maze.Cells[x, y];
            }

            lines.Add(new string(chars));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
