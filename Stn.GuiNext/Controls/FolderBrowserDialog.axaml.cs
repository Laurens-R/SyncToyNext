using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace Stn.GuiNext;

public enum FolderBrowserDialogOutcome
{
    OK,
    Cancelled
}

public class FolderBrowserDialogResult
{
    public string SelectedFolder { get; set; } = string.Empty;
    public FolderBrowserDialogOutcome Outcome { get; set; } = FolderBrowserDialogOutcome.Cancelled;
}

public partial class FolderBrowserDialog : UserControl
{

    private EventHandler<PopupControlResult>? _currentDialogHandler;

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static readonly StyledProperty<bool> ShowMessageBoxProperty =
    AvaloniaProperty.Register<FolderBrowserDialog, bool>(nameof(ShowMessageBox), defaultValue: false);

    public bool ShowMessageBox
    {
        get => GetValue(ShowMessageBoxProperty);

        set
        {
            SetValue(ShowMessageBoxProperty, value);
            OnPropertyChanged(nameof(ShowMessageBox));
        }
    }

    public string[] Drives { 
        
        get
        {
            //todo: this is actually only needed on windows and not on Mac or Linux... so fix this later.
            if(OperatingSystem.IsWindows())
            {
                //enumerate the drives.
                return DriveInfo.GetDrives().
                    Where(drive => drive.IsReady).
                    Select(drive => drive.Name).
                    ToArray();
            }

            return [];
        } 
    }

    public FolderBrowserDialog()
    {
        InitializeComponent();
        DataContext = this;

        comboDrives.ItemsSource = Drives;
        comboDrives.SelectedIndex = 0;
        comboDrives.SelectionChanged += ComboDrives_SelectionChanged;

        folderBrowser.CurrentPathChanged += FolderBrowser_CurrentPathChanged;
        radioLocalDrive.IsCheckedChanged += RadioLocalDrive_IsCheckedChanged;

        sectionDrives.IsVisible = true;
        sectionNetworkLocation.IsVisible = false;

        btnLoadNetworkLocation.Tapped += BtnLoadNetworkLocation_Tapped;
        textboxNetworkLocation.KeyUp += TextboxNetworkLocation_KeyUp;
    }

    private void TextboxNetworkLocation_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if(e.Key == Avalonia.Input.Key.Enter)
        {
            CheckAndSwitchToNetworkLocation();
        }
    }

    private void BtnLoadNetworkLocation_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        CheckAndSwitchToNetworkLocation();
    }

    private void CheckAndSwitchToNetworkLocation()
    {
        if (Directory.Exists(textboxNetworkLocation.Text))
        {
            folderBrowser.BrowserPath = textboxNetworkLocation.Text;
            textboxNetworkLocation.Foreground = Brushes.Black;
            labelCurrentPath.Content = textboxNetworkLocation.Text;
        }
        else
        {
            textboxNetworkLocation.Foreground = Brushes.Red;
        }
    }

    private void RadioLocalDrive_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (radioLocalDrive.IsChecked == true)
        {
            sectionDrives.IsVisible = true;
            sectionNetworkLocation.IsVisible = false;
            comboDrives.SelectedIndex = 0;
            folderBrowser.BrowserPath = comboDrives.SelectedItem?.ToString() ?? string.Empty;
            labelCurrentPath.Content = folderBrowser.CurrentPath;
        } else
        {
            sectionDrives.IsVisible = false;
            sectionNetworkLocation.IsVisible = true;
            textboxNetworkLocation.Text = string.Empty;
            labelCurrentPath.Content = String.Empty;
        }
    }

    private void FolderBrowser_CurrentPathChanged(object? sender, string e)
    {
        labelCurrentPath.Content = e;
    }

    private void ComboDrives_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        folderBrowser.BrowserPath = comboDrives.SelectedItem?.ToString() ?? string.Empty;
    }

    public async Task<FolderBrowserDialogResult> ShowDialogAsync(string title)
    {
        var tcs = new TaskCompletionSource<FolderBrowserDialogResult>();

        // Unsubscribe previous handler if it exists
        if (_currentDialogHandler != null)
        {
            popupDialog.DialogChoice -= _currentDialogHandler;
            _currentDialogHandler = null;
        }

        popupDialog.Title = title;

        // Store the handler reference so we can unsubscribe later
        _currentDialogHandler = (sender, result) =>
        {
            ShowMessageBox = false;
            OnPropertyChanged(nameof(ShowMessageBox));

            // Unsubscribe after handling to prevent leaks
            popupDialog.DialogChoice -= _currentDialogHandler;
            _currentDialogHandler = null;

            var folderResult = new FolderBrowserDialogResult
            {
                Outcome = (result == PopupControlResult.OK) ? FolderBrowserDialogOutcome.OK : FolderBrowserDialogOutcome.Cancelled,
                SelectedFolder = folderBrowser.CurrentPath
            };

            tcs.SetResult(folderResult);
        };

        popupDialog.DialogChoice += _currentDialogHandler;

        ShowMessageBox = true;
        OnPropertyChanged(nameof(ShowMessageBox));

        return await tcs.Task;
    }
}