using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DataStructuresVisualizer.Services;

namespace DataStructuresVisualizer.Views;

public partial class InfixPostfixView : UserControl
{
    private InfixConversionResult? conversion;
    private DispatcherTimer? timer;
    private int stepIndex;

    public InfixPostfixView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox InputBox => Require<TextBox>("InfixExpressionInput");
    private WrapPanel InputTokens => Require<WrapPanel>("InfixTokensPanel");
    private StackPanel OperatorStack => Require<StackPanel>("OperatorStackPanel");
    private WrapPanel OutputTokens => Require<WrapPanel>("PostfixOutputPanel");
    private TextBlock StatusLabel => Require<TextBlock>("ConversionStatus");
    private TextBlock FinalPostfix => Require<TextBlock>("FinalPostfixText");
    private TextBlock FinalValue => Require<TextBlock>("FinalValueText");
    private TextBlock StepLabel => Require<TextBlock>("StepExplanationText");
    private TextBlock CounterLabel => Require<TextBlock>("ConversionCounterText");
    private Slider SpeedSlider => Require<Slider>("ConversionSpeedSlider");

    private void RandomExpression_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        InputBox.Text = ExpressionService.GenerateRandomExpression(Random.Shared);
        conversion = null;
        FinalPostfix.Text = "—";
        FinalValue.Text = "—";
        StatusLabel.Text = "Случайное выражение готово.";
        ClearVisuals();
    }

    private void Build_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BuildConversion();
    }

    private void Start_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (conversion is null && !BuildConversion())
        {
            return;
        }

        StopAnimation();
        stepIndex = 0;
        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(SpeedSlider.Value)
        };
        timer.Tick += Timer_Tick;
        timer.Start();
        StatusLabel.Text = "Пошаговое преобразование началось.";
    }

    private void Stop_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        StatusLabel.Text = "Анимация остановлена.";
    }

    private void Reset_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        conversion = null;
        stepIndex = 0;
        FinalPostfix.Text = "—";
        FinalValue.Text = "—";
        CounterLabel.Text = "Шаг: 0 / 0";
        StatusLabel.Text = "Визуализация сброшена.";
        StepLabel.Text = "Шаги появятся после запуска.";
        ClearVisuals();
    }

    private bool BuildConversion()
    {
        try
        {
            conversion = InfixConversionService.ConvertWithSteps(InputBox.Text ?? string.Empty);
            stepIndex = 0;
            FinalPostfix.Text = conversion.Postfix;
            FinalValue.Text = Format(Evaluate(conversion.Postfix));
            StatusLabel.Text = "Постфиксная запись построена. Запустите анимацию.";
            CounterLabel.Text = $"Шаг: 0 / {conversion.Steps.Count}";
            RenderEmptyState(conversion.Tokens);
            return true;
        }
        catch (Exception exception)
        {
            conversion = null;
            StatusLabel.Text = $"Ошибка: {exception.Message}";
            ClearVisuals();
            return false;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (conversion is null || timer is null)
        {
            return;
        }

        if (stepIndex >= conversion.Steps.Count)
        {
            StopAnimation();
            StatusLabel.Text = "Преобразование завершено.";
            return;
        }

        ApplyStep(conversion.Steps[stepIndex]);
        stepIndex++;
        CounterLabel.Text = $"Шаг: {stepIndex} / {conversion.Steps.Count}";
        timer.Interval = TimeSpan.FromMilliseconds(SpeedSlider.Value);

        if (stepIndex >= conversion.Steps.Count)
        {
            StopAnimation();
            StatusLabel.Text = "Преобразование завершено.";
        }
    }

    private void ApplyStep(InfixConversionStep step)
    {
        RenderTokens(step.CurrentToken, step.Stack, step.Output);
        StepLabel.Text = step.Message;
    }

    private void RenderEmptyState(IReadOnlyList<string> tokens)
    {
        InputTokens.Children.Clear();
        OperatorStack.Children.Clear();
        OutputTokens.Children.Clear();

        foreach (var token in tokens)
        {
            InputTokens.Children.Add(CreateToken(token));
        }
    }

    private void RenderTokens(
        int currentToken,
        IReadOnlyList<string> stack,
        IReadOnlyList<string> output)
    {
        if (conversion is null)
        {
            return;
        }

        InputTokens.Children.Clear();

        for (var i = 0; i < conversion.Tokens.Count; i++)
        {
            InputTokens.Children.Add(CreateToken(conversion.Tokens[i], i == currentToken));
        }

        OperatorStack.Children.Clear();

        foreach (var token in stack)
        {
            OperatorStack.Children.Add(CreateToken(token));
        }

        OutputTokens.Children.Clear();

        foreach (var token in output)
        {
            OutputTokens.Children.Add(CreateToken(token, outputToken: true));
        }
    }

    private static Border CreateToken(string text, bool current = false, bool outputToken = false)
    {
        var border = new Border();
        border.Classes.Add("token");

        if (current)
        {
            border.Classes.Add("current");
        }

        if (outputToken)
        {
            border.Classes.Add("output");
        }

        border.Child = new TextBlock
        {
            Text = text,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            FontSize = 14
        };

        return border;
    }

    private void ClearVisuals()
    {
        InputTokens.Children.Clear();
        OperatorStack.Children.Clear();
        OutputTokens.Children.Clear();
    }

    private void StopAnimation()
    {
        timer?.Stop();
        timer = null;
    }

    private static double Evaluate(string postfix)
    {
        var root = ExpressionService.BuildTree(postfix);
        return ExpressionService.Evaluate(root, []);
    }

    private static string Format(double value) =>
        value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

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
