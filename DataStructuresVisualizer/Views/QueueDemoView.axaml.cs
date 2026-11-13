using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DataStructuresVisualizer.Services;

namespace DataStructuresVisualizer.Views;

public partial class QueueDemoView : UserControl
{
    private const double CardWidth = 92;
    private const double CardGap = 10;
    private const double CardLeft = 20;
    private const double CardTop = 20;

    private readonly LinkedList<string> linkedQueue = new();
    private readonly Queue<string> standardQueue = new();
    private readonly List<Border> linkedCards = [];
    private readonly List<Border> standardCards = [];
    private readonly List<string> log = [];
    private readonly List<(int code, string? value)> pendingOperations = [];

    private DispatcherTimer? sequenceTimer;
    private int operationIndex;
    private IReadOnlyList<ImportedOperationFile> queueFiles = [];
    private ImportedOperationFile? selectedQueueFile;

    public QueueDemoView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadQueueFiles();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox ValueBox => Require<TextBox>("QueueValueInput");
    private TextBox SequenceBox => Require<TextBox>("QueueSequenceInput");
    private Canvas LinkedCanvas => Require<Canvas>("LinkedQueueCanvas");
    private Canvas StandardCanvas => Require<Canvas>("StandardQueueCanvas");
    private TextBlock StatusText => Require<TextBlock>("QueueStatus");
    private TextBlock CountText => Require<TextBlock>("QueueCountText");
    private TextBlock HeadText => Require<TextBlock>("QueueHeadText");
    private TextBlock LinkedTimeLabel => Require<TextBlock>("LinkedTimeText");
    private TextBlock StandardTimeLabel => Require<TextBlock>("StandardTimeText");
    private ListBox LogBox => Require<ListBox>("QueueLogList");
    private ComboBox QueueFileSelector => Require<ComboBox>("QueueFileCombo");
    private ComboBox QueueSetSelector => Require<ComboBox>("QueueSetCombo");

    private void Enqueue_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Enqueue(ValueBox.Text ?? "клиент");
    }

    private void Dequeue_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Dequeue();
    }

    private void Peek_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (linkedQueue.Count == 0)
        {
            AppendLog("Peek → очередь пуста");
            StatusText.Text = "Очередь пуста.";
            return;
        }

        var linkedHead = linkedQueue.First!.Value;
        var standardHead = standardQueue.Peek();
        AppendLog($"Peek → связанная: {linkedHead}; Queue<T>: {standardHead}");
        StatusText.Text = $"Голова очереди: {linkedHead}.";
        UpdateState();
    }

    private void IsEmpty_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var empty = linkedQueue.Count == 0;
        AppendLog($"isEmpty → связанная: {empty}; Queue<T>: {(standardQueue.Count == 0)}");
        StatusText.Text = empty ? "Очереди пусты." : "Очереди содержат элементы.";
    }

    private void Print_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var linked = linkedQueue.Count == 0 ? "[ ]" : $"[ {string.Join(", ", linkedQueue)} ]";
        var standard = standardQueue.Count == 0 ? "[ ]" : $"[ {string.Join(", ", standardQueue)} ]";
        AppendLog($"Print → связанная: {linked}; Queue<T>: {standard}");
        StatusText.Text = "Содержимое обеих очередей выведено в журнал.";
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
        linkedQueue.Clear();
        standardQueue.Clear();
        linkedCards.Clear();
        standardCards.Clear();
        LinkedCanvas.Children.Clear();
        StandardCanvas.Children.Clear();
        log.Clear();
        LogBox.ItemsSource = null;
        UpdateState();
        StatusText.Text = "Обе очереди очищены.";
    }

    private void QueueFile_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var index = QueueFileSelector.SelectedIndex;

        if (index < 0 || index >= queueFiles.Count)
        {
            return;
        }

        selectedQueueFile = queueFiles[index];

        if (selectedQueueFile.Error is not null)
        {
            QueueSetSelector.ItemsSource = new[] { "Файл содержит ошибку" };
            QueueSetSelector.SelectedIndex = 0;
            StatusText.Text = $"Ошибка файла: {selectedQueueFile.Error}";
            return;
        }

        QueueSetSelector.ItemsSource = selectedQueueFile.Sets
            .Select(set => $"Набор {set.Number} · {set.Operations.Count} операций")
            .ToArray();
        QueueSetSelector.SelectedIndex = 0;
    }

    private void ImportQueueFile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (selectedQueueFile is null)
        {
            StatusText.Text = "Сначала выберите файл.";
            return;
        }

        if (selectedQueueFile.Error is not null)
        {
            StatusText.Text = $"Файл не может быть импортирован: {selectedQueueFile.Error}";
            return;
        }

        var setIndex = QueueSetSelector.SelectedIndex;

        if (setIndex < 0 || setIndex >= selectedQueueFile.Sets.Count)
        {
            StatusText.Text = "Выберите набор.";
            return;
        }

        var set = selectedQueueFile.Sets[setIndex];
        SequenceBox.Text = string.Join(" ", set.Operations.Select(ToToken));
        StatusText.Text = $"Импортирован {selectedQueueFile.Name}, набор {set.Number}.";
        StartSequence();
    }

    private void LoadQueueFiles()
    {
        queueFiles = OperationFileService.LoadFiles("QueueInput");

        if (queueFiles.Count == 0)
        {
            QueueFileSelector.ItemsSource = new[] { "Файлы не найдены" };
            QueueFileSelector.SelectedIndex = 0;
            QueueSetSelector.ItemsSource = Array.Empty<string>();
            return;
        }

        QueueFileSelector.ItemsSource = queueFiles
            .Select(file => file.Error is null ? file.Name : $"{file.Name} ⚠")
            .ToArray();
        QueueFileSelector.SelectedIndex = 0;
    }

    private static string ToToken(ImportedOperation operation) =>
        operation.Code == 1 ? $"1,{operation.Value}" : operation.Code.ToString();

    private void Enqueue(string value)
    {
        var linkedWatch = Stopwatch.StartNew();
        linkedQueue.AddLast(value);
        linkedWatch.Stop();

        var standardWatch = Stopwatch.StartNew();
        standardQueue.Enqueue(value);
        standardWatch.Stop();

        AddCard(LinkedCanvas, linkedCards, value);
        AddCard(StandardCanvas, standardCards, value);
        ScheduleLayout(LinkedCanvas, linkedCards);
        ScheduleLayout(StandardCanvas, standardCards);

        LinkedTimeLabel.Text = $"Enqueue: {linkedWatch.Elapsed.TotalMilliseconds:0.######} мс";
        StandardTimeLabel.Text = $"Enqueue: {standardWatch.Elapsed.TotalMilliseconds:0.######} мс";
        AppendLog($"Enqueue({value})");
        UpdateState();
    }

    private void Dequeue()
    {
        if (linkedQueue.Count == 0)
        {
            AppendLog("Dequeue → пусто");
            StatusText.Text = "Dequeue невозможен: очередь пуста.";
            return;
        }

        var linkedWatch = Stopwatch.StartNew();
        var linkedValue = linkedQueue.First!.Value;
        linkedQueue.RemoveFirst();
        linkedWatch.Stop();

        var standardWatch = Stopwatch.StartNew();
        var standardValue = standardQueue.Dequeue();
        standardWatch.Stop();

        RemoveFirstCard(LinkedCanvas, linkedCards);
        RemoveFirstCard(StandardCanvas, standardCards);
        LayoutLane(LinkedCanvas, linkedCards);
        LayoutLane(StandardCanvas, standardCards);
        LinkedTimeLabel.Text = $"Dequeue: {linkedWatch.Elapsed.TotalMilliseconds:0.######} мс";
        StandardTimeLabel.Text = $"Dequeue: {standardWatch.Elapsed.TotalMilliseconds:0.######} мс";
        AppendLog($"Dequeue → связанная: {linkedValue}; Queue<T>: {standardValue}");
        UpdateState();
    }

    private void AddCard(Canvas canvas, List<Border> cardList, string value)
    {
        var border = new Border();
        border.Classes.Add("queueCard");
        border.Child = new TextBlock
        {
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 12
        };

        var index = cardList.Count;
        var targetX = CardLeft + index * (CardWidth + CardGap);
        var startX = targetX + 160;

        canvas.Width = Math.Max(canvas.Width, startX + CardWidth + 24);
        Canvas.SetLeft(border, startX);
        Canvas.SetTop(border, CardTop);
        canvas.Children.Add(border);
        cardList.Add(border);
    }

    private static void RemoveFirstCard(Canvas canvas, List<Border> cardList)
    {
        if (cardList.Count == 0)
        {
            return;
        }

        var border = cardList[0];
        cardList.RemoveAt(0);
        border.Opacity = 0;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();

            if (canvas.Children.Contains(border))
            {
                canvas.Children.Remove(border);
            }
        };

        timer.Start();
    }

    private static void LayoutLane(Canvas canvas, List<Border> cardList)
    {
        var required = CardLeft + cardList.Count * (CardWidth + CardGap) + CardLeft;
        canvas.Width = Math.Max(720, required + 180);
        canvas.Height = 96;

        for (var i = 0; i < cardList.Count; i++)
        {
            Canvas.SetLeft(cardList[i], CardLeft + i * (CardWidth + CardGap));
            Canvas.SetTop(cardList[i], CardTop);
            cardList[i].Classes.Remove("headCard");

            if (i == 0)
            {
                cardList[i].Classes.Add("headCard");
            }
        }
    }

    private static void ScheduleLayout(Canvas canvas, List<Border> cardList)
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(25)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            LayoutLane(canvas, cardList);
        };

        timer.Start();
    }

    private void UpdateState()
    {
        CountText.Text = $"Элементов: {linkedQueue.Count}";
        HeadText.Text = linkedQueue.Count == 0
            ? "Голова: —"
            : $"Голова: {linkedQueue.First!.Value}";
        StatusText.Text = "Операция выполнена.";
    }

    private void StartSequence()
    {
        StopSequence();
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
            Interval = TimeSpan.FromMilliseconds(560)
        };
        sequenceTimer.Tick += SequenceTick;
        sequenceTimer.Start();
        StatusText.Text = "Выполняется набор операций.";
    }

    private void SequenceTick(object? sender, EventArgs e)
    {
        if (operationIndex >= pendingOperations.Count)
        {
            StopSequence();
            StatusText.Text = "Набор операций выполнен.";
            return;
        }

        var operation = pendingOperations[operationIndex++];

        switch (operation.code)
        {
            case 1:
                Enqueue(operation.value ?? "?");
                break;
            case 2:
                Dequeue();
                break;
            case 3:
                Peek_Click(null, null!);
                break;
            case 4:
                IsEmpty_Click(null, null!);
                break;
            case 5:
                Print_Click(null, null!);
                break;
        }
    }

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
