using Avalonia;
using Avalonia.Controls;
using Stn.GuiNext.ViewModels;

namespace Stn.GuiNext.Views;

public partial class RepositoryView : UserControl
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

    public RepositoryView()
    {
        InitializeComponent();
        menuCloseRepo.PointerReleased += MenuCloseRepo_PointerReleased;
        buttonPush.Tapped += ButtonPush_Tapped;
        buttonRestore.Tapped += ButtonRestore_Tapped;
        buttonRestoreSingle.Tapped += ButtonRestoreSingle_Tapped;
    }

    private async void ButtonRestoreSingle_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await messageBox.ShowDialogAsync("Are you sure you want to restore the selected files from this syncpoint? It will undo all your local changes since that syncpoint to the files that you have selected.", "Are you sure?", MessageBoxOptions.YesNo);
    }

    private async void ButtonRestore_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await messageBox.ShowDialogAsync("Are you sure you want to restore this syncpoint? It will undo all your local changes since that syncpoint.", "Are you sure?", MessageBoxOptions.YesNo);
    }

    private async void ButtonPush_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await messageBox.ShowDialogAsync("Are you sure you want to push your changes?", "Are you sure?", MessageBoxOptions.YesNo);
    }

    private async void MenuCloseRepo_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        var result = await messageBox.ShowDialogAsync("Are you sure you want to close this repository?", "Are you sure?", MessageBoxOptions.YesNo);
    }
}
