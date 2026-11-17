using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DataStructuresVisualizer.Controls;

/// <summary>Edges read the same coordinates as the cards on every animation frame.</summary>
public sealed class LinkLayer : Control
{
    public IReadOnlyList<Control> Nodes { get; set; } = [];
    public IReadOnlyList<IReadOnlyList<Control>>? Chains { get; set; }
    public double NodeWidth { get; set; }
    public double NodeHeight { get; set; }
    public LinkLayer() { IsHitTestVisible = false; }

    public override void Render(DrawingContext context)
    {
        var pen = new Pen(Brushes.LightSkyBlue, 2);
        var chains = Chains is { Count: > 0 }
            ? Chains
            : new[] { Nodes };

        foreach (var chain in chains)
        {
            for (var i = 0; i + 1 < chain.Count; i++)
            {
                var a = chain[i];
                var b = chain[i + 1];
                var start = new Point(Canvas.GetLeft(a) + NodeWidth, Canvas.GetTop(a) + NodeHeight / 2);
                var end = new Point(Canvas.GetLeft(b), Canvas.GetTop(b) + NodeHeight / 2);
                if (!double.IsFinite(start.X + start.Y + end.X + end.Y)) continue;

                // Route below the nodes during a permutation, straight in the settled row.
                if (Math.Abs(start.Y - end.Y) > 1 || end.X < start.X)
                {
                    var route = Math.Max(start.Y, end.Y) + NodeHeight / 2 + 12;
                    var p1 = new Point(start.X + 8, start.Y);
                    var p2 = new Point(start.X + 8, route);
                    var p3 = new Point(end.X - 8, route);
                    var p4 = new Point(end.X - 8, end.Y);
                    context.DrawLine(pen, start, p1);
                    context.DrawLine(pen, p1, p2);
                    context.DrawLine(pen, p2, p3);
                    context.DrawLine(pen, p3, p4);
                    context.DrawLine(pen, p4, end);
                }
                else
                {
                    context.DrawLine(pen, start, end);
                }

                context.DrawLine(pen, end, end + new Vector(-6, -4));
                context.DrawLine(pen, end, end + new Vector(-6, 4));
            }
        }
    }
}
