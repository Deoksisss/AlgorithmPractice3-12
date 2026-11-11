using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DataStructuresVisualizer.Models;
using DataStructuresVisualizer.Services;

namespace DataStructuresVisualizer.Views;

public partial class QueueView : UserControl
{
    private const int WorkdayMinutes = 480;
    private const double QueueStartX = 260;
    private const double QueueY = 250;
    private const double SlotWidth = 92;

    private readonly List<BankCustomer> plan = [];
    private readonly List<BankCustomer> waiting = [];
    private readonly List<Border> customerVisuals = [];
    private readonly List<DispatcherTimer> removalTimers = [];

    private DispatcherTimer? timer;
    private int pointer;
    private int minute;
    private int busyMinutes;
    private int idleMinutes;
    private int servedCount;
    private int totalWaitMinutes;
    private int waitCount;
    private BankCustomer? active;

    public QueueView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            UpdateCanvasLayout();
            GenerateClients();
        };
        BankScrollerControl.SizeChanged += (_, _) => UpdateCanvasLayout();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox ClientTextBox => Require<TextBox>("ClientInput");
    private Canvas QueueCanvas => Require<Canvas>("BankCanvas");
    private ScrollViewer BankScrollerControl => Require<ScrollViewer>("BankScroller");
    private Avalonia.Controls.Shapes.Ellipse TellerShape => Require<Avalonia.Controls.Shapes.Ellipse>("TellerEllipse");
    private TextBlock StatusLabel => Require<TextBlock>("BankStatus");
    private TextBlock ClockLabel => Require<TextBlock>("ClockText");
    private Slider SpeedControl => Require<Slider>("SpeedSlider");
    private TextBlock TotalLabel => Require<TextBlock>("TotalText");
    private TextBlock ServedLabel => Require<TextBlock>("ServedText");
    private TextBlock WaitingLabel => Require<TextBlock>("WaitingText");
    private TextBlock AverageWaitLabel => Require<TextBlock>("AverageWaitText");
    private TextBlock UtilizationLabel => Require<TextBlock>("UtilizationText");
    private TextBlock IdleLabel => Require<TextBlock>("IdleText");

    private void GenerateClients_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        GenerateClients();
    }

    private void StartShift_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopSimulation();
        ClearVisualization();
        ResetCounters();

        if (string.IsNullOrWhiteSpace(ClientTextBox.Text))
        {
            plan.Clear();
            plan.AddRange(BankService.GenerateRandom(Random.Shared, WorkdayMinutes));
            ClientTextBox.Text = BankService.ToInput(plan);
        }

        if (!BankService.TryParse(ClientTextBox.Text ?? string.Empty, out var parsed, out var error))
        {
            StatusLabel.Text = $"Ошибка: {error}";
            return;
        }

        plan.Clear();
        plan.AddRange(parsed);
        TotalLabel.Text = plan.Count.ToString();

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(GetInterval())
        };
        timer.Tick += Timer_Tick;
        timer.Start();
        StatusLabel.Text = "Симуляция рабочего дня началась.";
    }

    private void StopShift_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopSimulation();
        StatusLabel.Text = "Симуляция остановлена.";
    }

    private void GenerateClients()
    {
        StopSimulation();
        ClearVisualization();
        ResetCounters();

        plan.Clear();
        plan.AddRange(BankService.GenerateRandom(Random.Shared, WorkdayMinutes));
        ClientTextBox.Text = BankService.ToInput(plan);
        TotalLabel.Text = plan.Count.ToString();
        UpdateCanvasLayout();
        StatusLabel.Text = $"Сгенерировано клиентов: {plan.Count}.";
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (timer is null)
        {
            return;
        }

        timer.Interval = TimeSpan.FromMilliseconds(GetInterval());
        AdvanceOneMinute();
        UpdateStats();
        UpdateCanvasLayout();

        if (minute >= WorkdayMinutes)
        {
            StopSimulation();
            StatusLabel.Text = "Рабочий день завершён.";
        }
    }

    private void AdvanceOneMinute()
    {
        while (pointer < plan.Count && plan[pointer].ArrivalMinute <= minute)
        {
            Enqueue(plan[pointer++]);
        }

        if (active is null && waiting.Count > 0)
        {
            StartNextService();
        }

        if (active is not null)
        {
            busyMinutes++;
            active.RemainingServiceMinutes--;

            if (active.RemainingServiceMinutes <= 0)
            {
                CompleteService();
            }
        }
        else
        {
            idleMinutes++;
            TellerShape.Classes.Remove("busy");
            TellerShape.Classes.Add("idle");
        }

        minute++;
        ClockLabel.Text = FormatClock(minute);
    }

    private void Enqueue(BankCustomer customer)
    {
        UpdateCanvasLayout();
        var visual = CreateCustomerVisual(customer);
        customer.Visual = visual;

        waiting.Add(customer);
        customerVisuals.Add(visual);

        PositionWaiting();
    }

    private Border CreateCustomerVisual(BankCustomer customer)
    {
        var canvas = QueueCanvas;
        var border = new Border
        {
            Width = 82,
            Height = 48
        };

        border.Classes.Add("customer");
        border.Child = new TextBlock
        {
            Text = $"{customer.Name}\n{customer.ServiceMinutes} мин",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 10
        };

        Canvas.SetLeft(border, Math.Max(0, canvas.Width - 92));
        Canvas.SetTop(border, QueueY);
        canvas.Children.Add(border);

        return border;
    }

    private void StartNextService()
    {
        var customer = waiting[0];
        waiting.RemoveAt(0);

        active = customer;
        customer.WaitedMinutes = minute - customer.ArrivalMinute;
        customer.RemainingServiceMinutes = customer.ServiceMinutes;
        totalWaitMinutes += customer.WaitedMinutes.Value;
        waitCount++;

        if (customer.Visual is Border visual)
        {
            visual.Classes.Add("active");
            Canvas.SetLeft(visual, 16);
            Canvas.SetTop(visual, 54);
        }

        TellerShape.Classes.Remove("idle");
        TellerShape.Classes.Add("busy");
        PositionWaiting();
    }

    private void CompleteService()
    {
        if (active is null)
        {
            return;
        }

        servedCount++;
        active.IsServed = true;

        if (active.Visual is Border visual)
        {
            visual.Classes.Remove("active");
            visual.Classes.Add("served");
            Canvas.SetLeft(visual, 16);
            Canvas.SetTop(visual, 345);

            StartRemovalTimer(visual);
        }

        active = null;
        ServedLabel.Text = servedCount.ToString();
        WaitingLabel.Text = waiting.Count.ToString();

        if (waiting.Count > 0)
        {
            StartNextService();
        }
    }

    private void StartRemovalTimer(Border visual)
    {
        var removalTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(950)
        };

        removalTimer.Tick += (_, _) =>
        {
            removalTimer.Stop();
            visual.Opacity = 0;

            var cleanupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(650)
            };

            cleanupTimer.Tick += (_, _) =>
            {
                cleanupTimer.Stop();

                if (QueueCanvas.Children.Contains(visual))
                {
                    QueueCanvas.Children.Remove(visual);
                }

                customerVisuals.Remove(visual);
                removalTimers.Remove(removalTimer);
            };

            cleanupTimer.Start();
        };

        removalTimers.Add(removalTimer);
        removalTimer.Start();
    }

    private void PositionWaiting()
    {
        UpdateCanvasLayout();

        for (var i = 0; i < waiting.Count; i++)
        {
            if (waiting[i].Visual is Border visual)
            {
                Canvas.SetLeft(visual, QueueStartX + i * SlotWidth);
                Canvas.SetTop(visual, QueueY);
            }
        }
    }

    private void UpdateCanvasLayout()
    {
        var viewportWidth = Math.Max(760, BankScrollerControl.Bounds.Width - 24);
        var viewportHeight = Math.Max(430, BankScrollerControl.Bounds.Height - 12);
        var neededWidth = QueueStartX + (waiting.Count + 2) * SlotWidth + 220;

        QueueCanvas.Width = Math.Max(viewportWidth, neededWidth);
        QueueCanvas.Height = viewportHeight;
    }

    private void UpdateStats()
    {
        WaitingLabel.Text = waiting.Count.ToString();
        AverageWaitLabel.Text = waitCount == 0
            ? "0 мин"
            : $"{(double)totalWaitMinutes / waitCount:0.0} мин";

        var totalBusy = busyMinutes + idleMinutes;

        UtilizationLabel.Text = totalBusy == 0
            ? "0%"
            : $"{(double)busyMinutes / totalBusy * 100:0}%";

        IdleLabel.Text = totalBusy == 0
            ? "0%"
            : $"{(double)idleMinutes / totalBusy * 100:0}%";
    }

    private void ClearVisualization()
    {
        foreach (var removalTimer in removalTimers)
        {
            removalTimer.Stop();
        }

        removalTimers.Clear();

        var canvas = QueueCanvas;

        foreach (var visual in customerVisuals)
        {
            canvas.Children.Remove(visual);
        }

        customerVisuals.Clear();
        waiting.Clear();
        active = null;
        TellerShape.Classes.Remove("busy");
        TellerShape.Classes.Add("idle");
        UpdateCanvasLayout();
    }

    private void ResetCounters()
    {
        minute = 0;
        pointer = 0;
        busyMinutes = 0;
        idleMinutes = 0;
        servedCount = 0;
        totalWaitMinutes = 0;
        waitCount = 0;
        ClockLabel.Text = FormatClock(0);
        ServedLabel.Text = "0";
        WaitingLabel.Text = "0";
        AverageWaitLabel.Text = "0 мин";
        UtilizationLabel.Text = "0%";
        IdleLabel.Text = "0%";
    }

    private void StopSimulation()
    {
        timer?.Stop();
        timer = null;
    }

    private int GetInterval()
    {
        return Math.Clamp((int)(620 - SpeedControl.Value * 10), 30, 600);
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

    private static string FormatClock(int minutes)
    {
        var time = new TimeSpan(9, 0, 0).Add(TimeSpan.FromMinutes(minutes));

        if (time.TotalHours >= 24)
        {
            time = time.Subtract(TimeSpan.FromHours(24));
        }

        return $"{time.Hours:00}:{time.Minutes:00}";
    }
}
