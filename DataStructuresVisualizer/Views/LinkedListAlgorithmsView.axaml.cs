using DataStructuresVisualizer.Controls;
using System.Collections.Generic;
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
    private readonly List<Border> departing = [];
    private readonly SceneMotion motion = new();
    private readonly LinkLayer links = new() { NodeWidth = NodeWidth, NodeHeight = NodeHeight };
    private int nextId;

    public LinkedListAlgorithmsView()
    {
        InitializeComponent();
        ResponsiveColumns.Attach(this);
        DetachedFromVisualTree += (_, _) => motion.Complete();
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
        if (motion.IsRunning) { StatusText.Text = "Дождитесь завершения перемещения."; return; }
        if (!ApplyValuesFromInput(onlyIfChanged: true))
        {
            return;
        }

        ClearChanged();

        var task = TaskSelector.SelectedIndex;
        var note = string.Empty;

        switch (task)
        {
            case 0:
                var beforeReverse = nodes.ToArray();
                nodes.Reverse();
                for (var i = 0; i < nodes.Count; i++)
                    if (nodes[i] != beforeReverse[i]) nodes[i].View.Classes.Add("changed");
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
                if (!nodes.Select(n => n.Value).SequenceEqual(nodes.Select(n => n.Value).Order()))
                {
                    StatusText.Text = "Для этой операции нужен список по возрастанию.";
                    return;
                }
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

    private bool ApplyValuesFromInput(bool onlyIfChanged = false)
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

        if (onlyIfChanged && nodes.Select(node => node.Value).SequenceEqual(values))
        {
            return true;
        }

        motion.Complete();
        nodes.Clear();
        departing.Clear();
        Surface.Children.Clear();
        Surface.Children.Add(links);
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
            MaxWidth = NodeWidth - 8,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
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

        Canvas.SetLeft(border, 30 + Surface.Children.OfType<Border>().Count() * (NodeWidth + NodeGap));
        Surface.Children.Add(border);
        Canvas.SetTop(border, NodeTop);
        border.Opacity = 0;

        ToolTip.SetTip(border, $"Узел #{node.Id}: {value}");
        return node;
    }

    private VisualNode CloneNode(int value) => CreateNode(value);

    private void RemoveNode(VisualNode node)
    {
        nodes.Remove(node);
        departing.Add(node.View);
    }

    private void LayoutNodes()
    {
        if (departing.Count > 0)
        {
            var removed = departing.ToArray();
            departing.Clear();
            motion.Animate(removed.Select(v => new SceneMotion.Target(v, Canvas.GetLeft(v), NodeTop + NodeHeight + 20, 0)).ToArray(),
                frame: links.InvalidateVisual, completed: () =>
                {
                    foreach (var view in removed) Surface.Children.Remove(view);
                    LayoutNodes();
                }, milliseconds: 180);
            return;
        }
        Surface.Width = Math.Max(260, 60 + nodes.Count * (NodeWidth + NodeGap));
        var targets = new List<SceneMotion.Target>();
        var lane = NodeTop + NodeHeight + 30;
        for (var i = 0; i < nodes.Count; i++)
        {
            var x = 30 + i * (NodeWidth + NodeGap);
            var moves = Math.Abs(Canvas.GetLeft(nodes[i].View) - x) > 1;
            targets.Add(new(nodes[i].View, x, NodeTop, 1, moves ? lane : 0));
            if (moves) lane += NodeHeight + 30;
        }
        Surface.Height = Math.Max(180, lane + NodeHeight + 30);
        links.Nodes = nodes.Select(n => (Control)n.View).ToArray();
        links.Width = Surface.Width; links.Height = Surface.Height;
        motion.Animate(targets, links.InvalidateVisual, () =>
        {
            Surface.Height = 180;
            links.Height = Surface.Height;
        }, 650);
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
