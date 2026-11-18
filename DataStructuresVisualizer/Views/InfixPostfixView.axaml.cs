using DataStructuresVisualizer.Controls;
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
    private string builtExpression = string.Empty;
    private readonly SceneMotion motion = new();
    private readonly List<Border> tokens = [];
    private Canvas Surface => Require<Canvas>("TokenCanvas");

    public InfixPostfixView()
    {
        InitializeComponent();
        ResponsiveColumns.Attach(this);
        DetachedFromVisualTree += (_, _) => StopAnimation();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TextBox InputBox => Require<TextBox>("InfixExpressionInput");
    private TextBlock StatusLabel => Require<TextBlock>("ConversionStatus");
    private TextBlock FinalPostfix => Require<TextBlock>("FinalPostfixText");
    private TextBlock FinalValue => Require<TextBlock>("FinalValueText");
    private TextBlock StepLabel => Require<TextBlock>("StepExplanationText");
    private TextBlock CounterLabel => Require<TextBlock>("ConversionCounterText");
    private Slider SpeedSlider => Require<Slider>("ConversionSpeedSlider");

    private void RandomExpression_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StopAnimation();
        InputBox.Text = InfixConversionService.GenerateRandomExpression(Random.Shared);
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
        if ((conversion is null || builtExpression != InputBox.Text) && !BuildConversion())
        {
            return;
        }

        StopAnimation();
        stepIndex = 0;
        RenderEmptyState(conversion!.Tokens);
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
        StopAnimation();
        try
        {
            builtExpression = InputBox.Text ?? string.Empty;
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
            FinalPostfix.Text = "—";
            FinalValue.Text = "—";
            CounterLabel.Text = "Шаг: 0 / 0";
            StatusLabel.Text = $"Ошибка: {exception.Message}";
            ClearVisuals();
            return false;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!IsEffectivelyVisible) { StopAnimation(); return; }
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
        motion.Complete();
        var stack = step.StackIndices ?? [];
        var output = step.OutputIndices ?? [];
        var targets = new List<SceneMotion.Target>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var stackIndex = Array.IndexOf(stack.ToArray(), i);
            var outputIndex = Array.IndexOf(output.ToArray(), i);
            var x = 20 + i % 4 * 62;
            var y = 60 + i / 4 * 50;
            var opacity = i < step.CurrentToken ? 0.25 : 1.0;
            if (stackIndex >= 0) { x = 290; y = 60 + stackIndex * 50; opacity = 1; }
            if (outputIndex >= 0) { x = 400 + outputIndex % 4 * 62; y = 60 + outputIndex / 4 * 50; opacity = 1; }
            tokens[i].Classes.Set("current", i == step.CurrentToken);
            tokens[i].Classes.Set("output", outputIndex >= 0);
            targets.Add(new(tokens[i], x, y, opacity));
        }
        Surface.Height = Math.Max(220, 100 + Math.Max((tokens.Count + 3) / 4, stack.Count) * 50);
        motion.Animate(targets, milliseconds: Math.Min(350, SpeedSlider.Value * 0.8));
        StepLabel.Text = step.Message;
    }

    private void RenderEmptyState(IReadOnlyList<string> values)
    {
        ClearVisuals();
        foreach (var (text, x) in new[] { ("Входные токены", 20), ("Стек · TOP ↓", 280), ("Выход →", 400) })
        {
            var label = new TextBlock { Text = text, FontSize = 18 };
            Canvas.SetLeft(label, x); Canvas.SetTop(label, 12); Surface.Children.Add(label);
        }
        for (var i = 0; i < values.Count; i++)
        {
            var token = CreateToken(values[i]);
            token.Width = 56; token.Height = 38;
            ToolTip.SetTip(token, $"Токен {i + 1}: {values[i]}");
            Canvas.SetLeft(token, 20 + i % 4 * 62); Canvas.SetTop(token, 60 + i / 4 * 50);
            Surface.Children.Add(token); tokens.Add(token);
        }
        Surface.Height = Math.Max(220, 100 + (tokens.Count + 5) / 4 * 50);
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
            FontFamily = new Avalonia.Media.FontFamily("Consolas,DejaVu Sans Mono"),
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            FontSize = 14
        };

        return border;
    }

    private void ClearVisuals()
    {
        motion.Complete();
        Surface.Children.Clear();
        tokens.Clear();
    }

    private void StopAnimation()
    {
        motion.Complete();
        timer?.Stop();
        timer = null;
    }

    private static double Evaluate(string postfix) =>
        InfixConversionService.EvaluatePostfix(postfix);

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
