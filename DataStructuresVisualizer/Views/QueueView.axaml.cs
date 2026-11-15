using DataStructuresVisualizer.Controls;
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

    private readonly SceneMotion motion = new();
    private readonly Dictionary<Border, Avalonia.Point> destinations = [];
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
        ResponsiveColumns.Attach(this);
        Loaded += (_, _) =>
        {
            UpdateCanvasLayout();
            GenerateClients();
        };
        DetachedFromVisualTree += (_, _) => StopSimulation();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox ClientTextBox => Require<TextBox>("ClientInput");
    private Canvas QueueCanvas => Require<Canvas>("BankCanvas");
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
        if (!IsEffectivelyVisible) { StopSimulation(); return; }
        if (timer is null)
        {
            return;
        }

        timer.Interval = TimeSpan.FromMilliseconds(GetInterval());
        motion.Complete();
        AdvanceOneMinute();
        AnimateCustomers();
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
            MaxWidth = 76,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            FontSize = 10
        };

        var targetX = QueueStartX + waiting.Count * SlotWidth;
        Canvas.SetLeft(border, targetX);
        Canvas.SetTop(border, QueueY + 70);
        border.Opacity = 0;
        ToolTip.SetTip(border, $"{customer.Name} · обслуживание {customer.ServiceMinutes} мин");
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
            destinations[visual] = new Avalonia.Point(120, 54);
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
            var departureX = 16.0;
            while (customerVisuals.Any(other => other != visual && other.Classes.Contains("served") &&
                Math.Abs((destinations.TryGetValue(other, out var target) ? target.X : Canvas.GetLeft(other)) - departureX) < SlotWidth - 1))
                departureX += SlotWidth;
            destinations[visual] = new Avalonia.Point(departureX, 370);

            StartRemovalTimer(visual);
        }

        active = null;
        ServedLabel.Text = servedCount.ToString();
        WaitingLabel.Text = waiting.Count.ToString();
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
            motion.Complete();
            destinations.Remove(visual);
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
                destinations.Remove(visual);
                removalTimers.Remove(cleanupTimer);
                removalTimers.Remove(removalTimer);
            };

            removalTimers.Add(cleanupTimer);
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
                destinations[visual] = new Avalonia.Point(QueueStartX + i * SlotWidth, QueueY);
            }
        }
    }

    private void UpdateCanvasLayout()
    {
        var neededWidth = QueueStartX + (waiting.Count + 1) * SlotWidth + 40;
        QueueCanvas.Width = Math.Max(600, Math.Max(neededWidth,
            destinations.Count == 0 ? 0 : destinations.Values.Max(p => p.X) + SlotWidth + 24));
        QueueCanvas.Height = 450;
    }

    private void AnimateCustomers()
    {
        var targets = destinations.Select(pair => new SceneMotion.Target(pair.Key, pair.Value.X, pair.Value.Y)).ToArray();
        motion.Animate(targets, milliseconds: Math.Min(300, GetInterval() * 0.8));
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
        motion.Complete();
        destinations.Clear();
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
        motion.Complete();
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
