using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DataStructuresVisualizer.Models;
using DataStructuresVisualizer.Services;

namespace DataStructuresVisualizer.Views;

public partial class ExpressionTreeView : UserControl
{
    private const double NodeWidth = 60;
    private const double NodeHeight = 44;

    private readonly Dictionary<ExpressionNode, Border> visuals = [];
    private readonly LruExpressionCache _lruCache = new(96);

    private DispatcherTimer? timer;
    private ExpressionNode? root;
    private string postfix = string.Empty;
    private IReadOnlyList<EvaluationStep>? steps;
    private int stepIndex;
    private string builtExpression = string.Empty;
    private ListBox CacheList => Require<ListBox>("CacheEntries");

    public ExpressionTreeView()
    {
        InitializeComponent();
        DetachedFromVisualTree += (_, _) => StopAnimation();

        Loaded += (_, _) => BuildCurrentTree();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox InputBox => Require<TextBox>("ExpressionInput");
    private Canvas TreeSurface => Require<Canvas>("TreeCanvas");
    private TextBlock PostfixLabel => Require<TextBlock>("PostfixText");
    private TextBlock ResultLabel => Require<TextBlock>("ResultText");
    private TextBlock LruCacheLabel => Require<TextBlock>("LruCacheText");
    private TextBlock StatusLabel => Require<TextBlock>("TreeStatus");

    private void RandomExpression_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        InputBox.Text = ExpressionService.GenerateRandomExpression(Random.Shared);
        ResultLabel.Text = "—";
        StatusLabel.Text = "Случайное выражение готово. Нажмите «Построить дерево».";
    }

    private void BuildTree_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        BuildCurrentTree();
    }

    private void StartTree_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();

        if (root is null || builtExpression != InputBox.Text)
        {
            BuildCurrentTree();
        }

        if (root is null)
        {
            return;
        }

        var evaluationSteps = new List<EvaluationStep>();

        try
        {
            ClearNodeStates();
            var initialCache = _lruCache.Describe();
            var initialEntries = _lruCache.Snapshot();
            var playbackCache = new LruExpressionCache(_lruCache.Capacity);
            playbackCache.Restore(_lruCache.Capture());
            ExpressionService.Evaluate(root, evaluationSteps, playbackCache);
            steps = evaluationSteps;
            stepIndex = 0;
            ResultLabel.Text = "—";
            LruCacheLabel.Text = initialCache;
            CacheList.ItemsSource = initialEntries;
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(650)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            StatusLabel.Text = "Пост-порядковый обход дерева начался.";
        }
        catch (Exception exception)
        {
            steps = null;
            StatusLabel.Text = $"Ошибка вычисления: {exception.Message}";
        }
    }

    private void StopTree_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        StatusLabel.Text = "Анимация остановлена.";
    }

    private void ResetTree_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        ClearNodeStates();
        _lruCache.Clear();
        LruCacheLabel.Text = _lruCache.Describe();
        CacheList.ItemsSource = null;
        ResultLabel.Text = "—";
        StatusLabel.Text = "Анимация и LRU-кэш сброшены.";
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!IsEffectivelyVisible) { StopAnimation(); return; }
        if (steps is null || timer is null)
        {
            return;
        }

        if (stepIndex >= steps.Count)
        {
            timer.Stop();
            StatusLabel.Text = "Вычисление завершено.";
            return;
        }

        ApplyStep(stepIndex);
        stepIndex++;

        if (stepIndex >= steps.Count)
        {
            timer.Stop();
            StatusLabel.Text = "Вычисление завершено.";
        }
    }

    private void BuildCurrentTree()
    {
        try
        {
            builtExpression = InputBox.Text ?? string.Empty;
            postfix = ExpressionService.ToPostfix(InputBox.Text ?? string.Empty);
            root = ExpressionService.BuildTree(postfix);
            PostfixLabel.Text = postfix;
            ResultLabel.Text = "—";
            steps = null;
            stepIndex = 0;
            RenderTree();
            StatusLabel.Text = "Дерево построено. Можно запускать вычисление.";
        }
        catch (Exception exception)
        {
            root = null;
            steps = null;
            ResultLabel.Text = "—";
            PostfixLabel.Text = "—";
            StatusLabel.Text = exception.Message;
            RenderTree();
        }
    }

    private void RenderTree()
    {
        var canvas = TreeSurface;
        canvas.Children.Clear();
        visuals.Clear();

        if (root is null)
        {
            return;
        }

        var layoutWidth = ExpressionService.Layout(root);
        const double offsetX = 50;
        const double offsetY = 50;
        canvas.Width = Math.Max(160, layoutWidth + 100);
        canvas.Height = Math.Max(140, MaximumY(root) + 100);

        DrawTree(root, offsetX, offsetY);
    }

    private static double MaximumY(ExpressionNode node) => Math.Max(node.Y,
        Math.Max(node.Left is null ? 0 : MaximumY(node.Left), node.Right is null ? 0 : MaximumY(node.Right)));

    private void DrawTree(ExpressionNode node, double offsetX, double offsetY)
    {
        var canvas = TreeSurface;

        if (node.Left is not null)
        {
            canvas.Children.Add(CreateEdge(node, node.Left, offsetX, offsetY));
            DrawTree(node.Left, offsetX, offsetY);
        }

        if (node.Right is not null)
        {
            canvas.Children.Add(CreateEdge(node, node.Right, offsetX, offsetY));
            DrawTree(node.Right, offsetX, offsetY);
        }

        var border = new Border();
        border.Classes.Add("exprNode");

        if (node.IsOperator)
        {
            border.Classes.Add("operator");
        }

        border.Child = new TextBlock
        {
            Text = node.Value,
            MaxWidth = NodeWidth - 8,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };

        Canvas.SetLeft(border, node.X + offsetX - NodeWidth / 2.0);
        Canvas.SetTop(border, node.Y + offsetY - NodeHeight / 2.0);
        canvas.Children.Add(border);
        visuals[node] = border;
        ToolTip.SetTip(border, node.Value);
    }

    private static Avalonia.Controls.Shapes.Line CreateEdge(
        ExpressionNode parent,
        ExpressionNode child,
        double offsetX,
        double offsetY)
    {
        var line = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(
                parent.X + offsetX,
                parent.Y + offsetY + NodeHeight / 2),
            EndPoint = new Avalonia.Point(
                child.X + offsetX,
                child.Y + offsetY - NodeHeight / 2)
        };

        line.Classes.Add("edge");
        return line;
    }

    private void ApplyStep(int index)
    {
        if (steps is null)
        {
            return;
        }

        ClearNodeStates();

        for (var i = 0; i < index; i++)
        {
            if (steps[i].HasValue) visuals[steps[i].Node].Classes.Add(steps[i].FromCache ? "cached" : "done");
        }

        var current = steps[index];
        visuals[current.Node].Classes.Add(current.FromCache ? "cached" : "current");
        StatusLabel.Text = current.Description;
        ResultLabel.Text = current.HasValue ? Format(current.Value) : "—";
        LruCacheLabel.Text = current.CacheState;
        CacheList.ItemsSource = current.CacheEntries;
        if (current.Cache is not null) _lruCache.Restore(current.Cache);
    }

    private void ClearNodeStates()
    {
        foreach (var border in visuals.Values)
        {
            border.Classes.Remove("current");
            border.Classes.Remove("done");
            border.Classes.Remove("cached");
        }
    }

    private void StopAnimation()
    {
        timer?.Stop();
        timer = null;
        stepIndex = 0;
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

    private static string Format(double value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);
}
