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

    private readonly List<BankCustomer> plan = [];
    private readonly List<BankCustomer> waiting = [];
    private readonly List<Border> customerVisuals = [];

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
        Loaded += (_, _) => GenerateClients();
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

    private TextBlock TotalLabel => Require<TextBlock>("TotalText");
    private TextBlock ServedLabel => Require<TextBlock>("ServedText");
    private TextBlock WaitingLabel => Require<TextBlock>("WaitingText");
    private TextBlock AverageWaitLabel => Require<TextBlock>("AverageWaitText");
    private TextBlock UtilizationLabel => Require<TextBlock>("UtilizationText");
    private TextBlock IdleLabel => Require<TextBlock>("IdleText");

    private void GenerateClients()
    {
        StopSimulation();
        ClearVisualization();
        ResetCounters();

        plan.Clear();
        plan.AddRange(BankService.GenerateRandom(Random.Shared, WorkdayMinutes));
        ClientTextBox.Text = BankService.ToInput(plan);
        TotalLabel.Text = plan.Count.ToString();
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
        waiting.Add(customer);
        var visual = CreateCustomerVisual(customer);
        customer.Visual = visual;
        customerVisuals.Add(visual);
        PositionWaiting();
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

        if (customer.Visual is not null)
        {
            customer.Visual.Classes.Add("active");
            Canvas.SetLeft(customer.Visual, 52);
            Canvas.SetTop(customer.Visual, 48);
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
            Canvas.SetLeft(visual, -220);
            Canvas.SetTop(visual, 310);
        }

        active = null;
        ServedLabel.Text = servedCount.ToString();
        WaitingLabel.Text = waiting.Count.ToString();

        if (waiting.Count > 0)
        {
            StartNextService();
        }
    }

    private Border CreateCustomerVisual(BankCustomer customer)
    {
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

        Canvas.SetLeft(border, 970);
        Canvas.SetTop(border, 250);
        QueueCanvas.Children.Add(border);

        return border;
    }

    private void PositionWaiting()
    {
        for (var i = 0; i < waiting.Count; i++)
        {
            if (waiting[i].Visual is Border visual)
            {
                Canvas.SetLeft(visual, 170 + i * 88);
                Canvas.SetTop(visual, 250);
            }
        }
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
