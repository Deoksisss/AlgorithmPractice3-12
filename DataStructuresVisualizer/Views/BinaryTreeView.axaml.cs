using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DataStructuresVisualizer.Models;
using DataStructuresVisualizer.Services;

namespace DataStructuresVisualizer.Views;

public partial class BinaryTreeView : UserControl
{
    private const double NodeSize = 112;
    private const double Half = NodeSize / 2;
    private const double CellHalf = NodeSize / 4;

    private BinaryTreeNode? root;

    public BinaryTreeView()
    {
        InitializeComponent();
        Loaded += (_, _) => BuildCurrent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox InputBox => Require<TextBox>("BinaryTreeInput");
    private Canvas Surface => Require<Canvas>("BinaryTreeCanvas");
    private TextBlock StatusText => Require<TextBlock>("BinaryTreeStatus");
    private TextBlock NodeCountLabel => Require<TextBlock>("NodeCountText");
    private TextBlock HeightLabel => Require<TextBlock>("HeightText");
    private TextBlock NullCountLabel => Require<TextBlock>("NullCountText");

    private void Build_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BuildCurrent();
    }

    private void Random_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        InputBox.Text = BinaryTreeService.GenerateRandom(Random.Shared);
        BuildCurrent();
    }

    private void Reset_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        root = null;
        Surface.Children.Clear();
        Surface.Width = 900;
        Surface.Height = 600;
        NodeCountLabel.Text = "Узлов: 0";
        HeightLabel.Text = "Высота: 0";
        NullCountLabel.Text = "NULL-ссылок: 0";
        StatusText.Text = "Визуализация сброшена.";
    }

    private void BuildCurrent()
    {
        var result = BinaryTreeService.Build(InputBox.Text ?? string.Empty);

        if (result.Error is not null || result.Root is null)
        {
            root = null;
            Surface.Children.Clear();
            StatusText.Text = $"Ошибка: {result.Error}";
            return;
        }

        root = result.Root;
        var layout = BinaryTreeService.Layout(root);
        Surface.Width = layout.Width;
        Surface.Height = layout.Height;
        Surface.Children.Clear();

        DrawEdges(root);
        DrawNodes(root);

        NodeCountLabel.Text = $"Узлов: {BinaryTreeService.CountNodes(root)}";
        HeightLabel.Text = $"Высота: {BinaryTreeService.Height(root)}";
        NullCountLabel.Text = $"NULL-ссылок: {BinaryTreeService.CountNulls(root)}";
        StatusText.Text = "Дерево построено.";
    }

    private void DrawEdges(BinaryTreeNode node)
    {
        if (node.Left is not null)
        {
            DrawArrow(
                new Point(node.X - CellHalf, node.Y + NodeSize * 0.75),
                new Point(node.Left.X, node.Left.Y));
            DrawEdges(node.Left);
        }

        if (node.Right is not null)
        {
            DrawArrow(
                new Point(node.X + CellHalf, node.Y + NodeSize * 0.75),
                new Point(node.Right.X, node.Right.Y));
            DrawEdges(node.Right);
        }
    }

    private void DrawArrow(Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1)
        {
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        const double headLength = 14;
        const double headWidth = 9;

        var shaftEnd = new Point(
            end.X - ux * headLength,
            end.Y - uy * headLength);

        var line = new Line
        {
            StartPoint = start,
            EndPoint = shaftEnd,
            Stroke = new SolidColorBrush(Color.Parse("#6E9BBE")),
            StrokeThickness = 2
        };

        var px = -uy;
        var py = ux;

        var polygon = new Polygon
        {
            Points =
            [
                end,
                new Point(shaftEnd.X + px * headWidth / 2, shaftEnd.Y + py * headWidth / 2),
                new Point(shaftEnd.X - px * headWidth / 2, shaftEnd.Y - py * headWidth / 2)
            ],
            Fill = new SolidColorBrush(Color.Parse("#6E9BBE"))
        };

        Surface.Children.Add(line);
        Surface.Children.Add(polygon);
    }

    private void DrawNodes(BinaryTreeNode node)
    {
        var outer = new Border
        {
            Width = NodeSize,
            Height = NodeSize,
            Background = new SolidColorBrush(Color.Parse("#17324B")),
            BorderBrush = new SolidColorBrush(Color.Parse("#69A8C6")),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(4)
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,*")
        };

        var top = new TextBlock
        {
            Text = node.Value.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 34,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#EAF8FF"))
        };

        var bottom = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*")
        };

        var leftCell = CreateCell(node.Left is not null, isLeft: true);
        var rightCell = CreateCell(node.Right is not null, isLeft: false);

        bottom.Children.Add(leftCell);
        Grid.SetColumn(rightCell, 1);
        bottom.Children.Add(rightCell);

        grid.Children.Add(top);
        Grid.SetRow(bottom, 1);
        grid.Children.Add(bottom);

        outer.Child = grid;
        Canvas.SetLeft(outer, node.X - Half);
        Canvas.SetTop(outer, node.Y);
        Surface.Children.Add(outer);

        if (node.Left is not null)
        {
            DrawNodes(node.Left);
        }

        if (node.Right is not null)
        {
            DrawNodes(node.Right);
        }
    }

    private static Border CreateCell(bool hasChild, bool isLeft)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse(hasChild ? "#123F4C" : "#1B293C")),
            BorderBrush = new SolidColorBrush(Color.Parse("#3A617A")),
            BorderThickness = new Thickness(0, 1, isLeft ? 1 : 0, 0)
        };

        border.Child = new TextBlock
        {
            Text = hasChild ? string.Empty : "NULL",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse(hasChild ? "#7FE3D0" : "#F08AA0"))
        };

        return border;
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
}
