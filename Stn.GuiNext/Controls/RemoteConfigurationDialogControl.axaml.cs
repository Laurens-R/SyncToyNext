using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Stn.GuiNext;

public enum RemoteDialogOutcome
{
    Ok,
    Cancelled
}

public class RemoteDialogResult
{
    public string? RemotePath { get; set; } = string.Empty;
    public bool IsCompressed { get; set; } = false;
    public RemoteDialogOutcome Outcome { get; set; } = RemoteDialogOutcome.Cancelled;
}

public partial class RemoteConfigurationDialogControl : UserControl
{

    private bool _targetExists = false;

    private EventHandler<PopupControlResult>? _currentDialogHandler;

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static readonly StyledProperty<bool> ShowDialogBoxProperty =
    AvaloniaProperty.Register<RemoteConfigurationDialogControl, bool>(nameof(ShowDialog), defaultValue: false);

    public bool ShowDialog
    {
        get => GetValue(ShowDialogBoxProperty);

        set
        {
            SetValue(ShowDialogBoxProperty, value);
            OnPropertyChanged(nameof(ShowDialog));
        }
    }

    public RemoteConfigurationDialogControl()
    {
        InitializeComponent();
        DataContext = this;
        buttonBrowseRemote.Tapped += ButtonBrowseRemote_Tapped;
        textboxRemotePath.KeyUp += TextboxRemotePath_KeyUp;
    }

    private void TextboxRemotePath_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if(e.Key == Avalonia.Input.Key.Enter)
        {
            _targetExists = Directory.Exists(textboxRemotePath.Text);

            if (!_targetExists)
            {
                textboxRemotePath.Foreground = Brushes.Red;
            } else
            {
                textboxRemotePath.Foreground = Brushes.Black;
            }   
        }
    }

    private async void ButtonBrowseRemote_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var remoteBrowserResult = await FolderBrowserDialog.ShowDialogAsync(mainRemoteConfigGrid, "Select a remote repository...");

        if(remoteBrowserResult.Outcome == FolderBrowserDialogOutcome.OK)
        {
            textboxRemotePath.Text = remoteBrowserResult.SelectedFolder;
        }

        _targetExists = Directory.Exists(textboxRemotePath.Text);

        if (!_targetExists)
        {
            textboxRemotePath.Foreground = Brushes.Red;
        }
        else
        {
            textboxRemotePath.Foreground = Brushes.Black;
        }
    }

    public static async Task<RemoteDialogResult> ShowDialogAsync(Panel parent, string title)
    {
        var dialog = new RemoteConfigurationDialogControl();

        var tcs = new TaskCompletionSource<RemoteDialogResult>();

        // Unsubscribe previous handler if it exists
        if (dialog._currentDialogHandler != null)
        {
            dialog.popupDialog.DialogChoice -= dialog._currentDialogHandler;
            dialog._currentDialogHandler = null;
        }

        dialog.popupDialog.Title = title;

        // Store the handler reference so we can unsubscribe later
        dialog._currentDialogHandler = (sender, result) =>
        {
            dialog.ShowDialog = false;
            dialog.OnPropertyChanged(nameof(ShowDialog));

            // Unsubscribe after handling to prevent leaks
            dialog.popupDialog.DialogChoice -= dialog._currentDialogHandler;
            dialog._currentDialogHandler = null;

            var remoteResult = new RemoteDialogResult();

            if (result == PopupControlResult.OK)
            {
                if (dialog._targetExists)
                {
                    remoteResult.RemotePath = dialog.textboxRemotePath.Text;
                    remoteResult.IsCompressed = dialog.checkboxIsCompressed.IsChecked.HasValue ? dialog.checkboxIsCompressed.IsChecked.Value : false;
                    remoteResult.Outcome = RemoteDialogOutcome.Ok;
                }
            }

            parent.Children.Remove(dialog);
            tcs.SetResult(remoteResult);
        };

        dialog.popupDialog.DialogChoice += dialog._currentDialogHandler;

        dialog.ShowDialog = true;
        dialog.OnPropertyChanged(nameof(ShowDialog));

        parent.Children.Add(dialog);

        return await tcs.Task;
    }
}