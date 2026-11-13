using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataStructuresVisualizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            var navigation = this.FindControl<ListBox>("NavigationList");

            if (navigation is not null && navigation.SelectedIndex < 0)
            {
                navigation.SelectedIndex = 0;
            }

            SelectPage(0);
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NavigationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var navigation = this.FindControl<ListBox>("NavigationList");

        if (navigation is not null)
        {
            SelectPage(navigation.SelectedIndex);
        }
    }

    private void SelectPage(int index)
    {
        var maze = this.FindControl<MazeView>("MazePage");
        var queue = this.FindControl<QueueView>("QueuePage");
        var tree = this.FindControl<ExpressionTreeView>("TreePage");

        if (maze is null || queue is null || tree is null)
        {
            return;
        }

        maze.IsVisible = index == 0;
        queue.IsVisible = index == 1;
        tree.IsVisible = index == 2;
    }
}
