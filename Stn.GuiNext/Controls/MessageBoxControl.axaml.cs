using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Stn.GuiNext;

public enum MessageBoxOptions
{
    YesNo,
    OK,
    OKCancel
}

public partial class MessageBoxControl : UserControl
{

    private EventHandler<PopupControlResult>? _currentDialogHandler;

    public static readonly StyledProperty<bool> ShowMessageBoxProperty =
    AvaloniaProperty.Register<MessageBoxControl, bool>(nameof(ShowMessageBox), defaultValue: false);

    public bool ShowMessageBox
    {
        get => GetValue(ShowMessageBoxProperty);
        
        set
        {
            SetValue(ShowMessageBoxProperty, value);
            OnPropertyChanged(nameof(ShowMessageBox));
        }
    }

    public event EventHandler<PopupControlResult>? DialogChoice;

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MessageBoxControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    public async Task<PopupControlResult> ShowDialogAsync(string message, string title, MessageBoxOptions options)
    {
        var tcs = new TaskCompletionSource<PopupControlResult>();

        // Unsubscribe previous handler if it exists
        if (_currentDialogHandler != null)
        {
            popupDialog.DialogChoice -= _currentDialogHandler;
            _currentDialogHandler = null;
        }

        switch (options)
        {
            case MessageBoxOptions.YesNo:
                popupDialog.HasYes = true;
                popupDialog.HasNo = true;
                popupDialog.HasOK = false;
                popupDialog.HasCancel = false ;
                break;
            case MessageBoxOptions.OK:
                popupDialog.HasYes = false;
                popupDialog.HasNo = false;
                popupDialog.HasOK = true;
                popupDialog.HasCancel = false;
                break;
            case MessageBoxOptions.OKCancel:
                popupDialog.HasYes = false;
                popupDialog.HasNo = false;
                popupDialog.HasOK = true;
                popupDialog.HasCancel = true;
                break;
        }

        popupDialog.Title = title;
        textMessage.Text = message;

        // Store the handler reference so we can unsubscribe later
        _currentDialogHandler = (sender, result) =>
        {
            ShowMessageBox = false;
            OnPropertyChanged(nameof(ShowMessageBox));

            // Unsubscribe after handling to prevent leaks
            popupDialog.DialogChoice -= _currentDialogHandler;
            _currentDialogHandler = null;

            tcs.SetResult(result);
        };

        popupDialog.DialogChoice += _currentDialogHandler;

        ShowMessageBox = true;
        OnPropertyChanged(nameof(ShowMessageBox));

        return await tcs.Task;
    }
}