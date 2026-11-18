using DataStructuresVisualizer.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DataStructuresVisualizer.Services;
using StackQueueLab.Core.Structures;

namespace DataStructuresVisualizer.Views;

public partial class StackDemoView : UserControl
{
    private const double CardHeight = 54;
    private const double CardGap = 10;
    private const double CardLeft = 30;
    private const double TopStart = 32;

    private LinkedStack<string> stack = new();
    private readonly List<Border> cards = [];
    private readonly List<string> log = [];
    private readonly List<(int code, string? value)> pendingOperations = [];

    private DispatcherTimer? sequenceTimer;
    private int operationIndex;
    private double totalOperationMilliseconds;
    private readonly SceneMotion motion = new();
    private IReadOnlyList<ImportedOperationFile> stackFiles = [];
    private ImportedOperationFile? selectedStackFile;

    public StackDemoView()
    {
        InitializeComponent();
        ResponsiveColumns.Attach(this);
        DetachedFromVisualTree += (_, _) => StopSequence();
        Loaded += (_, _) =>
        {
            LoadStackFiles();
            LayoutCards();
            UpdateState();
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox ValueBox => Require<TextBox>("StackValueInput");
    private TextBox SequenceBox => Require<TextBox>("StackSequenceInput");
    private Canvas Surface => Require<Canvas>("StackCanvas");
    private TextBlock StatusText => Require<TextBlock>("StackStatus");
    private TextBlock CountText => Require<TextBlock>("StackCountText");
    private TextBlock TopText => Require<TextBlock>("StackTopText");
    private TextBlock ReadTimeText => Require<TextBlock>("StackReadTimeText");
    private TextBlock OperationTimeText => Require<TextBlock>("StackOperationTimeText");
    private TextBlock TotalTimeText => Require<TextBlock>("StackTotalTimeText");
    private ListBox LogBox => Require<ListBox>("StackLogList");
    private ComboBox StackFileSelector => Require<ComboBox>("StackFileCombo");
    private ComboBox StackSetSelector => Require<ComboBox>("StackSetCombo");

    private void Push_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopSequence();
        Push(ValueBox.Text ?? "42");
    }

    private void Pop_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopSequence();
        Pop();
    }

    private void Top_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (stack.IsEmpty)
        {
            AppendLog("Top → стек пуст");
            StatusText.Text = "Стек пуст.";
            return;
        }

        var watch = Stopwatch.StartNew();
        var top = stack.Peek();
        watch.Stop();
        RecordOperation("Top", watch);

        AppendLog($"Top → {top}");
        StatusText.Text = $"Вершина стека: {top}";
    }

    private void IsEmpty_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var watch = Stopwatch.StartNew();
        var empty = stack.IsEmpty;
        watch.Stop();
        RecordOperation("isEmpty", watch);

        AppendLog($"isEmpty → {(empty ? "true" : "false")}");
        StatusText.Text = empty ? "Стек пуст." : "Стек содержит элементы.";
    }

    private void Print_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var watch = Stopwatch.StartNew();
        var snapshot = stack.Snapshot();
        var text = snapshot.Count == 0 ? "[ ]" : $"[ {string.Join(", ", snapshot)} ]";
        watch.Stop();
        RecordOperation("Print", watch);

        if (snapshot.Count == 0)
        {
            AppendLog("Print → [ ]");
            return;
        }

        AppendLog($"Print → {text}");
        StatusText.Text = "Содержимое стека выведено в журнал.";
    }

    private void RandomSequence_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var random = Random.Shared;
        var counter = 0;
        var operations = new List<string>();

        for (var i = 0; i < 12; i++)
        {
            var code = random.Next(1, 6);

            if (code == 1)
            {
                operations.Add($"1,{++counter}");
            }
            else
            {
                operations.Add(code.ToString());
            }
        }

        SequenceBox.Text = string.Join(" ", operations);
        StartSequence();
    }

    private void RunSequence_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StartSequence();
    }

    private void StopSequence_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopSequence();
        StatusText.Text = "Выполнение набора остановлено.";
    }

    private void Reset_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopSequence();
        motion.Complete();
        stack = new LinkedStack<string>();
        totalOperationMilliseconds = 0;
        cards.Clear();
        log.Clear();
        ReadTimeText.Text = "Чтение: —";
        OperationTimeText.Text = "Операция: —";
        UpdateTotalTimeLabel();
        foreach (var card in Surface.Children.OfType<Border>().ToArray()) Surface.Children.Remove(card);
        LayoutCards();
        LogBox.ItemsSource = null;
        UpdateState();
        StatusText.Text = "Стек очищен.";
    }

    private void StackFile_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var index = StackFileSelector.SelectedIndex;

        if (index < 0 || index >= stackFiles.Count)
        {
            return;
        }

        selectedStackFile = stackFiles[index];
        ReadTimeText.Text = $"Чтение: {FormatMilliseconds(selectedStackFile.ReadMilliseconds)} мс";

        if (selectedStackFile.Error is not null)
        {
            StackSetSelector.ItemsSource = new[] { "Файл содержит ошибку" };
            StackSetSelector.SelectedIndex = 0;
            StatusText.Text = $"Ошибка файла: {selectedStackFile.Error}";
            return;
        }

        StackSetSelector.ItemsSource = selectedStackFile.Sets
            .Select(set => $"Набор {set.Number} · {set.Operations.Count} операций")
            .ToArray();
        StackSetSelector.SelectedIndex = 0;
    }

    private void ImportStackFile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (selectedStackFile is null)
        {
            StatusText.Text = "Сначала выберите файл.";
            return;
        }

        if (selectedStackFile.Error is not null)
        {
            StatusText.Text = $"Файл не может быть импортирован: {selectedStackFile.Error}";
            return;
        }

        var setIndex = StackSetSelector.SelectedIndex;

        if (setIndex < 0 || setIndex >= selectedStackFile.Sets.Count)
        {
            StatusText.Text = "Выберите набор.";
            return;
        }

        var set = selectedStackFile.Sets[setIndex];
        SequenceBox.Text = string.Join(" ", set.Operations.Select(ToToken));
        StatusText.Text = $"Импортирован {selectedStackFile.Name}, набор {set.Number}.";
        StartSequence();
    }

    private void LoadStackFiles()
    {
        stackFiles = OperationFileService.LoadFiles("StackInput");

        if (stackFiles.Count == 0)
        {
            StackFileSelector.ItemsSource = new[] { "Файлы не найдены" };
            StackFileSelector.SelectedIndex = 0;
            StackSetSelector.ItemsSource = Array.Empty<string>();
            return;
        }

        StackFileSelector.ItemsSource = stackFiles
            .Select(file => file.Error is null ? file.Name : $"{file.Name} ⚠")
            .ToArray();
        StackFileSelector.SelectedIndex = 0;
    }

    private static string ToToken(ImportedOperation operation) =>
        operation.Code == 1 ? $"1,{operation.Value}" : operation.Code.ToString();

    private void StartSequence()
    {        StopSequence();
        pendingOperations.Clear();
        operationIndex = 0;

        var tokens = (SequenceBox.Text ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (token.StartsWith("1,", StringComparison.Ordinal) && token.Length > 2)
            {
                pendingOperations.Add((1, token[2..]));
            }
            else if (int.TryParse(token, out var code) && code is >= 2 and <= 5)
            {
                pendingOperations.Add((code, null));
            }
            else
            {
                StatusText.Text = $"Некорректная операция: {token}";
                return;
            }
        }

        if (pendingOperations.Count == 0)
        {
            StatusText.Text = "Набор операций пуст.";
            return;
        }

        sequenceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(520)
        };
        sequenceTimer.Tick += SequenceTick;
        sequenceTimer.Start();
        StatusText.Text = "Выполняется набор операций.";
    }

    private void SequenceTick(object? sender, EventArgs e)
    {
        if (!IsEffectivelyVisible) { StopSequence(); return; }
        if (operationIndex >= pendingOperations.Count)
        {
            StopSequence();
            StatusText.Text = "Набор операций выполнен.";
            return;
        }

        var operation = pendingOperations[operationIndex++];
        ExecuteOperation(operation.code, operation.value);
    }

    private void ExecuteOperation(int code, string? value)
    {
        switch (code)
        {
            case 1:
                Push(value ?? "?");
                break;
            case 2:
                Pop();
                break;
            case 3:
                Top_Click(null, null!);
                break;
            case 4:
                IsEmpty_Click(null, null!);
                break;
            case 5:
                Print_Click(null, null!);
                break;
        }
    }

    private void Push(string value)
    {
        motion.Complete();
        var watch = Stopwatch.StartNew();
        stack.Push(value);
        watch.Stop();
        RecordOperation("Push", watch);

        var card = CreateCard(value);
        cards.Insert(0, card);
        Surface.Children.Add(card);
        Canvas.SetTop(card, TopStart);
        Canvas.SetLeft(card, 260);
        card.Opacity = 0;

        LayoutCards(incoming: card);
        AppendLog($"Push({value})");
        UpdateState();
    }

    private void Pop()
    {
        motion.Complete();
        if (stack.IsEmpty)
        {
            AppendLog("Pop → пусто");
            StatusText.Text = "Pop невозможен: стек пуст.";
            return;
        }

        var watch = Stopwatch.StartNew();
        var value = stack.Pop();
        watch.Stop();
        RecordOperation("Pop", watch);

        var card = cards[0];
        cards.RemoveAt(0);
        LayoutCards(card);
        AppendLog($"Pop → {value}");
        UpdateState();
    }

    private Border CreateCard(string value)
    {
        var border = new Border();
        border.Classes.Add("stackCard");
        border.Child = new TextBlock
        {
            Text = value,
            MaxWidth = 170,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        };

        ToolTip.SetTip(border, value);
        return border;
    }

    private void LayoutCards(Border? removed = null, Border? incoming = null)
    {
        Surface.Width = 520;
        var settledHeight = Math.Max(160, TopStart + stack.Count * (CardHeight + CardGap) + 24);
        Surface.Height = removed is null ? settledHeight : Math.Max(Surface.Height, settledHeight);
        var targets = new List<SceneMotion.Target>();
        for (var i = 0; i < cards.Count; i++)
        {
            cards[i].Classes.Set("topCard", i == 0);
            targets.Add(new(cards[i], CardLeft, TopStart + i * (CardHeight + CardGap)));
        }
        if (removed is not null)
        {
            // Move the outgoing top into a separate column before compacting the stack.
            motion.Animate([new(removed, 270, TopStart, 0)], completed: () =>
            {
                Surface.Children.Remove(removed);
                motion.Animate(targets, completed: () => Surface.Height = settledHeight, milliseconds: 300);
            }, milliseconds: 180);
        }
        else if (incoming is not null)
        {
            motion.Animate(targets.Where(t => t.View != incoming).ToArray(), completed: () =>
                motion.Animate(targets.Where(t => t.View == incoming).ToArray(), milliseconds: 180), milliseconds: 180);
        }
        else motion.Animate(targets, milliseconds: 300);
    }

    private void UpdateState()
    {
        CountText.Text = $"Элементов: {stack.Count}";
        TopText.Text = stack.IsEmpty ? "Вершина: —" : $"Вершина: {stack.Peek()}";
        StatusText.Text = stack.IsEmpty ? "Стек пуст." : "Операция выполнена.";
    }

    private void RecordOperation(string name, Stopwatch watch)
    {
        totalOperationMilliseconds += watch.Elapsed.TotalMilliseconds;
        OperationTimeText.Text = $"{name}: {FormatMilliseconds(watch.Elapsed.TotalMilliseconds)} мс";
        UpdateTotalTimeLabel();
    }

    private void UpdateTotalTimeLabel()
    {
        TotalTimeText.Text = $"Всего: {FormatMilliseconds(totalOperationMilliseconds)} мс";
    }

    private static string FormatMilliseconds(double milliseconds) =>
        milliseconds.ToString("0.######", CultureInfo.InvariantCulture);

    private void AppendLog(string message)
    {
        log.Insert(0, message);

        if (log.Count > 80)
        {
            log.RemoveAt(log.Count - 1);
        }

        LogBox.ItemsSource = log.ToArray();
    }

    private void StopSequence()
    {
        motion.Complete();
        sequenceTimer?.Stop();
        sequenceTimer = null;
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
