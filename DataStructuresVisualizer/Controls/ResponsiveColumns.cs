using Avalonia;
using Avalonia.Controls;

namespace DataStructuresVisualizer.Controls;

/// <summary>On narrow pages move auxiliary information below the scene, keeping controls readable.</summary>
public static class ResponsiveColumns
{
    public static void Attach(UserControl view)
    {
        if (view.Content is not Grid grid || grid.ColumnDefinitions.Count != 3) return;
        var side = grid.Children.FirstOrDefault(c => Grid.GetColumn(c) == 2);
        if (side is null) return;
        var originalWidth = grid.ColumnDefinitions[2].Width;
        var originalMargin = side.Margin;
        bool? compact = null;
        view.SizeChanged += (_, _) =>
        {
            var next = view.Bounds.Width < 1000;
            if (compact == next) return;
            compact = next;
            grid.RowDefinitions = new RowDefinitions(next ? "*,150" : "*");
            grid.ColumnDefinitions[2].Width = next ? new GridLength(0) : originalWidth;
            Grid.SetRow(side, next ? 1 : 0);
            Grid.SetColumn(side, next ? 0 : 2);
            Grid.SetColumnSpan(side, next ? 2 : 1);
            side.Margin = next ? new Thickness(0, 8, 0, 0) : originalMargin;
        };
    }
}
