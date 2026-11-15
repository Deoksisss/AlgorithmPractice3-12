using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace DataStructuresVisualizer.Controls;

/// <summary>Navigation transforms the whole scene, never the algorithm's coordinates.</summary>
public sealed class SceneViewport : Decorator
{
    private readonly ScrollViewer scroller;
    private readonly Canvas transform;
    private readonly TextBlock scaleLabel;
    private Control? scene;
    private double scale = 1;
    private bool fit = true;
    private Point? dragStart;
    private Vector dragOffset;

    public Control? Scene
    {
        get => scene;
        set
        {
            if (scene is not null) scene.SizeChanged -= SceneSizeChanged;
            scene = value;
            transform.Children.Clear();
            if (value is not null)
            {
                value.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
                transform.Children.Add(value);
            }
            if (scene is not null) scene.SizeChanged += SceneSizeChanged;
            ScheduleFit();
        }
    }

    public SceneViewport()
    {
        ClipToBounds = true;
        transform = new Canvas { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        scroller = new ScrollViewer
        {
            Content = transform,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        scaleLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center, MinWidth = 48 };
        var toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
        AddButton(toolbar, "−", () => Zoom(scale / 1.25));
        AddButton(toolbar, "+", () => Zoom(scale * 1.25));
        AddButton(toolbar, "Вписать", () => { fit = true; Fit(); });
        AddButton(toolbar, "1:1", () => Zoom(1));
        toolbar.Children.Add(scaleLabel);
        ToolTip.SetTip(toolbar, "Ctrl + колесо — масштаб. Перетаскивание — перемещение.");
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        grid.Children.Add(toolbar);
        Grid.SetRow(scroller, 1);
        grid.Children.Add(scroller);
        Child = grid;
        scroller.SizeChanged += (_, _) => ScheduleFit();
        scroller.PointerWheelChanged += (_, e) =>
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
            Zoom(scale * Math.Pow(1.15, e.Delta.Y));
            e.Handled = true;
        };
        scroller.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(scroller).Properties.IsLeftButtonPressed) return;
            if (e.Source is Visual source && (source is ScrollBar || source.GetVisualAncestors().OfType<ScrollBar>().Any())) return;
            dragStart = e.GetPosition(scroller);
            dragOffset = scroller.Offset;
            e.Pointer.Capture(scroller);
        };
        scroller.PointerMoved += (_, e) =>
        {
            if (dragStart is not { } start) return;
            scroller.Offset = dragOffset - (e.GetPosition(scroller) - start);
        };
        scroller.PointerReleased += (_, e) => { dragStart = null; e.Pointer.Capture(null); };
        scroller.PointerCaptureLost += (_, _) => dragStart = null;
    }

    private static void AddButton(Panel panel, string text, Action action)
    {
        var button = new Button { Content = text, Padding = new Thickness(8, 4), Margin = new Thickness(0, 0, 4, 0) };
        button.Click += (_, _) => action();
        panel.Children.Add(button);
    }

    private void SceneSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        SetScale(scale);
        ScheduleFit();
    }
    private void ScheduleFit() => Dispatcher.UIThread.Post(() => { if (fit) Fit(); }, DispatcherPriority.Loaded);

    private void Fit()
    {
        if (scene is null || scene.Bounds.Width <= 0 || scene.Bounds.Height <= 0) return;
        var width = Math.Max(1, scroller.Bounds.Width - 24);
        var height = Math.Max(1, scroller.Bounds.Height - 24);
        SetScale(Math.Min(1, Math.Min(width / scene.Bounds.Width, height / scene.Bounds.Height)));
        scroller.Offset = default;
    }

    private void Zoom(double value)
    {
        fit = false;
        var center = (scroller.Offset + new Vector(scroller.Viewport.Width / 2, scroller.Viewport.Height / 2)) / scale;
        SetScale(Math.Clamp(value, 0.001, 4));
        Dispatcher.UIThread.Post(() => scroller.Offset = center * scale - new Vector(scroller.Viewport.Width / 2, scroller.Viewport.Height / 2));
    }

    private void SetScale(double value)
    {
        scale = Math.Max(0.000001, value);
        if (scene is not null)
        {
            scene.RenderTransform = new ScaleTransform(scale, scale);
            transform.Width = scene.Bounds.Width * scale;
            transform.Height = scene.Bounds.Height * scale;
        }
        scaleLabel.Text = $"{scale:P0}";
    }
}
