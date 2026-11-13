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
        var stack = this.FindControl<StackDemoView>("StackDemoPage");
        var infix = this.FindControl<InfixPostfixView>("InfixPage");
        var queue = this.FindControl<QueueDemoView>("QueueDemoPage");
        var examples = this.FindControl<ExamplesView>("ExamplesPage");
        var linkedList = this.FindControl<LinkedListAlgorithmsView>("LinkedListPage");

        if (stack is null || infix is null || queue is null || examples is null || linkedList is null)
        {
            return;
        }

        stack.IsVisible = index == 0;
        infix.IsVisible = index == 1;
        queue.IsVisible = index == 2;
        examples.IsVisible = index == 3;
        linkedList.IsVisible = index == 4;
    }
}
