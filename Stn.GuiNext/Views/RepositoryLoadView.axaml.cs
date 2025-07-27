using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Stn.GuiNext.ViewModels;

namespace Stn.GuiNext;

public partial class RepositoryLoadView : UserControl
{
    public static readonly StyledProperty<MainViewModel> ViewModelProperty =
    AvaloniaProperty.Register<FileBrowserControl, MainViewModel>(nameof(ViewModel));

    public MainViewModel ViewModel
    {
        get
        {
            return GetValue(ViewModelProperty);
        }
        set
        {
            SetValue(ViewModelProperty, value);
        }
    }


    public RepositoryLoadView()
    {
        InitializeComponent();
    }
}