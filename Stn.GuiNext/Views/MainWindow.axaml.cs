using Avalonia.Controls;
using Stn.GuiNext.ViewModels;

namespace Stn.GuiNext.Views;

public partial class MainWindow : Window
{
    public MainViewModel? ViewModel
    {
        get
        {
            return DataContext as MainViewModel;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
    }
}
