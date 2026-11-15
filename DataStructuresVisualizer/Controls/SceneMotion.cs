using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace DataStructuresVisualizer.Controls;

/// <summary>One clock per scene. Interrupted batches settle before the next operation begins.</summary>
public sealed class SceneMotion
{
    public sealed record Target(Control View, double X, double Y, double Opacity = 1, double Lane = 0);
    private DispatcherTimer? timer;
    private Action? finish;
    public bool IsRunning => timer is not null;

    public void Complete(bool includeFollowups = true)
    {
        timer?.Stop();
        timer = null;
        var action = finish;
        finish = null;
        action?.Invoke();
        if (includeFollowups && timer is not null) Complete();
    }

    public void Animate(IReadOnlyList<Target> targets, Action? frame = null, Action? completed = null, double milliseconds = 420)
    {
        Complete();
        var starts = targets.Select(t => new Point(Finite(Canvas.GetLeft(t.View)), Finite(Canvas.GetTop(t.View)))).ToArray();
        var opacity = targets.Select(t => t.View.Opacity).ToArray();
        void Draw(double progress)
        {
            var eased = progress * progress * (3 - 2 * progress);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var start = starts[i];
                double x, y;
                if (target.Lane > 0)
                {
                    // Lift, translate on a separate lane, then lower; swaps never cross cards.
                    var lift = Math.Clamp(progress * 3, 0, 1);
                    var travel = Math.Clamp(progress * 3 - 1, 0, 1);
                    var lower = Math.Clamp(progress * 3 - 2, 0, 1);
                    x = start.X + (target.X - start.X) * travel;
                    y = start.Y + (target.Lane - start.Y) * lift + (target.Y - target.Lane) * lower;
                }
                else
                {
                    x = start.X + (target.X - start.X) * eased;
                    y = start.Y + (target.Y - start.Y) * eased;
                }
                Canvas.SetLeft(target.View, x);
                Canvas.SetTop(target.View, y);
                target.View.Opacity = opacity[i] + (target.Opacity - opacity[i]) * eased;
            }
            frame?.Invoke();
        }
        finish = () => { Draw(1); completed?.Invoke(); };
        var watch = Stopwatch.StartNew();
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var progress = Math.Clamp(watch.Elapsed.TotalMilliseconds / milliseconds, 0, 1);
            Draw(progress);
            if (progress >= 1) Complete(false);
        };
        timer.Start();
    }

    private static double Finite(double value) => double.IsFinite(value) ? value : 0;
}
