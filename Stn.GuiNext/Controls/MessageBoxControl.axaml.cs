/*
    STN - A file synchronization and source control solution
    Copyright (C) 2025  Laurens Ruijtenberg

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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

    public static async Task<PopupControlResult> ShowDialogAsync(Panel parent, string message, string title, MessageBoxOptions options)
    {
        var dialog = new MessageBoxControl();
        var tcs = new TaskCompletionSource<PopupControlResult>();

        // Unsubscribe previous handler if it exists
        if (dialog._currentDialogHandler != null)
        {
            dialog.popupDialog.DialogChoice -= dialog._currentDialogHandler;
            dialog._currentDialogHandler = null;
        }

        switch (options)
        {
            case MessageBoxOptions.YesNo:
                dialog.popupDialog.HasYes = true;
                dialog.popupDialog.HasNo = true;
                dialog.popupDialog.HasOK = false;
                dialog.popupDialog.HasCancel = false ;
                break;
            case MessageBoxOptions.OK:
                dialog.popupDialog.HasYes = false;
                dialog.popupDialog.HasNo = false;
                dialog.popupDialog.HasOK = true;
                dialog.popupDialog.HasCancel = false;
                break;
            case MessageBoxOptions.OKCancel:
                dialog.popupDialog.HasYes = false;
                dialog.popupDialog.HasNo = false;
                dialog.popupDialog.HasOK = true;
                dialog.popupDialog.HasCancel = true;
                break;
        }

        dialog.popupDialog.Title = title;
        dialog.textMessage.Text = message;

        // Store the handler reference so we can unsubscribe later
        dialog._currentDialogHandler = (sender, result) =>
        {
            dialog.ShowMessageBox = false;
            dialog.OnPropertyChanged(nameof(ShowMessageBox));

            // Unsubscribe after handling to prevent leaks
            dialog.popupDialog.DialogChoice -= dialog._currentDialogHandler;
            dialog._currentDialogHandler = null;

            parent.Children.Remove(dialog);
            tcs.SetResult(result);
        };

        dialog.popupDialog.DialogChoice += dialog._currentDialogHandler;

        dialog.ShowMessageBox = true;
        dialog.OnPropertyChanged(nameof(ShowMessageBox));

        parent.Children.Add(dialog);

        return await tcs.Task;
    }
}