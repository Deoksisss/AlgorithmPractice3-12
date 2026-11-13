using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataStructuresVisualizer.Views;

public partial class ExamplesView : UserControl
{
    public ExamplesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
