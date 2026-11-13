using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace DataStructuresVisualizer.Views;

public partial class LinkedListAlgorithmsView : UserControl
{
    private sealed class VisualNode
    {
        public int Id { get; init; }
        public int Value { get; init; }
        public required Border View { get; init; }
    }

    private const double NodeWidth = 66;
    private const double NodeHeight = 54;
    private const double NodeGap = 30;
    private const double NodeTop = 52;

    private readonly List<VisualNode> nodes = [];
    private readonly List<string> log = [];
    private DispatcherTimer? positionTimer;
    private int nextId;

    public LinkedListAlgorithmsView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            TaskSelector.SelectedIndex = 0;
            UpdateTaskUi();
            ApplyValuesFromInput();
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private ComboBox TaskSelector => Require<ComboBox>("TaskCombo");
    private TextBox ValuesBox => Require<TextBox>("LinkedListValuesInput");
    private TextBox Param1Box => Require<TextBox>("Param1Input");
    private TextBox Param2Box => Require<TextBox>("Param2Input");
    private StackPanel Param1PanelControl => Require<StackPanel>("Param1Panel");
    private StackPanel Param2PanelControl => Require<StackPanel>("Param2Panel");
    private TextBlock Param1LabelControl => Require<TextBlock>("Param1Label");
    private TextBlock Param2LabelControl => Require<TextBlock>("Param2Label");
    private TextBlock StatusText => Require<TextBlock>("LinkedListStatus");
    private TextBlock DescriptionText => Require<TextBlock>("TaskDescriptionText");
    private TextBlock CountText => Require<TextBlock>("LinkedListCountText");
    private Canvas Surface => Require<Canvas>("ListCanvas");
    private ListBox LogBox => Require<ListBox>("LinkedListLogList");

    private void TaskSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateTaskUi();
    }

    private void RandomValues_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var random = Random.Shared;
        var count = random.Next(7, 26);
        var values = Enumerable.Range(0, count)
            .Select(_ => random.Next(0, 1000))
            .ToArray();

        ValuesBox.Text = string.Join(" ", values);
        ApplyValuesFromInput();
    }

    private void ApplyValues_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyValuesFromInput();
    }

    private void Execute_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!ApplyValuesFromInput())
        {
            return;
        }

        ClearChanged();

        var task = TaskSelector.SelectedIndex;
        var note = string.Empty;

        switch (task)
        {
            case 0:
                nodes.Reverse();
                MarkAllChanged();
                note = "Ссылки перестроены в обратном порядке.";
                break;

            case 1:
                if (nodes.Count > 1)
                {
                    var last = nodes[^1];
                    nodes.RemoveAt(nodes.Count - 1);
                    nodes.Insert(0, last);
                    last.View.Classes.Add("changed");
                    note = "Последний элемент перенесён в начало.";
                }
                else
                {
                    note = "Перенос не требуется.";
                }

                break;

            case 2:
                if (nodes.Count > 1)
                {
                    var first = nodes[0];
                    nodes.RemoveAt(0);
                    nodes.Add(first);
                    first.View.Classes.Add("changed");
                    note = "Первый элемент перенесён в конец.";
                }
                else
                {
                    note = "Перенос не требуется.";
                }

                break;

            case 3:
                note = $"Количество различных элементов: {nodes.Select(node => node.Value).Distinct().Count()}.";
                break;

            case 4:
                var counts = nodes.GroupBy(node => node.Value).ToDictionary(group => group.Key, group => group.Count());
                var toRemove = nodes.Where(node => counts[node.Value] > 1).ToArray();

                foreach (var node in toRemove)
                {
                    RemoveNode(node);
                }

                note = toRemove.Length == 0
                    ? "Неуникальных элементов нет."
                    : $"Удалено элементов: {toRemove.Length}.";
                break;

            case 5:
                var x = ReadParameter(Param1Box, "x");

                if (x is null)
                {
                    return;
                }

                var index = nodes.FindIndex(node => node.Value == x.Value);

                if (index < 0)
                {
                    StatusText.Text = $"Значение {x} не найдено.";
                    return;
                }

                var copies = nodes.Select(node => CloneNode(node.Value)).ToArray();

                for (var i = 0; i < copies.Length; i++)
                {
                    nodes.Insert(index + 1 + i, copies[i]);
                    copies[i].View.Classes.Add("changed");
                }

                note = "Копия списка вставлена после первого вхождения x.";
                break;

            case 6:
                var insertValue = ReadParameter(Param1Box, "E");

                if (insertValue is null)
                {
                    return;
                }

                var insertionIndex = nodes.FindIndex(node => node.Value >= insertValue.Value);

                if (insertionIndex < 0)
                {
                    insertionIndex = nodes.Count;
                }

                var inserted = CreateNode(insertValue.Value);
                nodes.Insert(insertionIndex, inserted);
                inserted.View.Classes.Add("changed");
                note = "Элемент E вставлен с сохранением порядка.";
                break;

            case 7:
                var removeValue = ReadParameter(Param1Box, "e");

                if (removeValue is null)
                {
                    return;
                }

                var removed = nodes.Where(node => node.Value == removeValue.Value).ToArray();

                foreach (var node in removed)
                {
                    RemoveNode(node);
                }

                note = $"Удалено элементов: {removed.Length}.";
                break;

            case 8:
                var f = ReadParameter(Param1Box, "F");
                var before = ReadParameter(Param2Box, "E");

                if (f is null || before is null)
                {
                    return;
                }

                var eIndex = nodes.FindIndex(node => node.Value == before.Value);

                if (eIndex < 0)
                {
                    StatusText.Text = $"Значение {before} не найдено.";
                    return;
                }

                var newNode = CreateNode(f.Value);
                nodes.Insert(eIndex, newNode);
                newNode.View.Classes.Add("changed");
                note = "Элемент F вставлен перед первым E.";
                break;
        }

        LayoutNodes();
        UpdateState(note);
        ValuesBox.Text = string.Join(" ", nodes.Select(node => node.Value));
    }

    private void Reset_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyValuesFromInput();
        ClearChanged();
        StatusText.Text = "Список восстановлен из поля ввода.";
    }

    private bool ApplyValuesFromInput()
    {
        var parts = (ValuesBox.Text ?? string.Empty)
            .Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var values = new List<int>();

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var value))
            {
                StatusText.Text = $"Некорректное значение: {part}";
                return false;
            }

            values.Add(value);
        }

        nodes.Clear();
        Surface.Children.Clear();
        positionTimer?.Stop();
        positionTimer = null;
        nextId = 0;

        foreach (var value in values)
        {
            var node = CreateNode(value);
            nodes.Add(node);
        }

        LayoutNodes();
        UpdateState(values.Count == 0 ? "Список пуст." : "Список загружен.");
        return true;
    }

    private VisualNode CreateNode(int value)
    {
        var border = new Border();
        border.Classes.Add("listNode");
        border.Child = new TextBlock
        {
            Text = value.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };

        var node = new VisualNode
        {
            Id = nextId++,
            Value = value,
            View = border
        };

        Surface.Children.Add(border);
        Canvas.SetLeft(border, 900);
        Canvas.SetTop(border, -80);
        border.Opacity = 0;

        return node;
    }

    private VisualNode CloneNode(int value) => CreateNode(value);

    private void RemoveNode(VisualNode node)
    {
        nodes.Remove(node);
        node.View.Opacity = 0;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(320)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();

            if (Surface.Children.Contains(node.View))
            {
                Surface.Children.Remove(node.View);
            }
        };

        timer.Start();
    }

    private void LayoutNodes()
    {
        Surface.Width = Math.Max(900, 30 + nodes.Count * (NodeWidth + NodeGap) + 30);
        Surface.Height = 150;

        var targets = new List<(VisualNode Node, double X, double Y)>(nodes.Count);

        for (var i = 0; i < nodes.Count; i++)
        {
            targets.Add((
                nodes[i],
                30 + i * (NodeWidth + NodeGap),
                NodeTop));
        }

        AnimatePositions(targets);
    }

    private void AnimatePositions(
        IReadOnlyList<(VisualNode Node, double X, double Y)> targets)
    {
        positionTimer?.Stop();

        if (targets.Count == 0)
        {
            return;
        }

        var starts = new List<(double X, double Y, double Opacity)>(targets.Count);

        foreach (var target in targets)
        {
            starts.Add((
                Canvas.GetLeft(target.Node.View),
                Canvas.GetTop(target.Node.View),
                target.Node.View.Opacity));
        }

        var stopwatch = Stopwatch.StartNew();
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        positionTimer = timer;
        timer.Tick += (_, _) =>
        {
            var progress = Math.Clamp(stopwatch.Elapsed.TotalMilliseconds / 420.0, 0, 1);
            var eased = 1 - Math.Pow(1 - progress, 3);

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var start = starts[i];

                Canvas.SetLeft(target.Node.View, start.X + (target.X - start.X) * eased);
                Canvas.SetTop(target.Node.View, start.Y + (target.Y - start.Y) * eased);
                target.Node.View.Opacity = start.Opacity + (1 - start.Opacity) * eased;
            }

            if (progress >= 1)
            {
                timer.Stop();
                positionTimer = null;

                for (var i = 0; i < targets.Count; i++)
                {
                    Canvas.SetLeft(targets[i].Node.View, targets[i].X);
                    Canvas.SetTop(targets[i].Node.View, targets[i].Y);
                    targets[i].Node.View.Opacity = 1;
                }
            }
        };

        timer.Start();
    }

    private void MarkAllChanged()
    {
        foreach (var node in nodes)
        {
            node.View.Classes.Add("changed");
        }
    }

    private void ClearChanged()
    {
        foreach (var node in nodes)
        {
            node.View.Classes.Remove("changed");
        }
    }

    private void UpdateTaskUi()
    {
        var task = TaskSelector.SelectedIndex;

        Param1PanelControl.IsVisible = task is 5 or 6 or 7 or 8;
        Param2PanelControl.IsVisible = task == 8;
        Param1LabelControl.Text = task switch
        {
            5 => "x — значение для поиска",
            6 => "E — вставляемое значение",
            7 => "e — удаляемое значение",
            8 => "F — вставляемое значение",
            _ => "Параметр"
        };
        Param2LabelControl.Text = "E — перед этим значением";

        DescriptionText.Text = task switch
        {
            0 => "Переворачивает ссылки: узлы меняют порядок на обратный.",
            1 => "Переносит последний узел в начало списка.",
            2 => "Переносит первый узел в конец списка.",
            3 => "Считает количество различных значений, не меняя список.",
            4 => "Удаляет каждый узел, значение которого встречается больше одного раза.",
            5 => "После первого x вставляет полную копию списка.",
            6 => "Вставляет E перед первым элементом, который не меньше E.",
            7 => "Удаляет все узлы со значением e.",
            8 => "Вставляет F перед первым вхождением E.",
            _ => "Список — последовательность узлов, связанных ссылками."
        };
    }

    private int? ReadParameter(TextBox box, string name)
    {
        if (!int.TryParse(box.Text, out var value))
        {
            StatusText.Text = $"Параметр {name} должен быть целым числом.";
            return null;
        }

        return value;
    }

    private void UpdateState(string note)
    {
        CountText.Text = $"Элементов: {nodes.Count}";
        StatusText.Text = note;

        if (!string.IsNullOrWhiteSpace(note))
        {
            log.Insert(0, note);

            if (log.Count > 60)
            {
                log.RemoveAt(log.Count - 1);
            }

            LogBox.ItemsSource = log.ToArray();
        }
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
