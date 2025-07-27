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
        DataContext = new MainViewModel();

        var vm = ViewModel;

        if(vm != null)
        {
            
        }

        buttonRepository.Tapped += ButtonRepository_Tapped;
    }

    private void ButtonRepository_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        
    }
}
