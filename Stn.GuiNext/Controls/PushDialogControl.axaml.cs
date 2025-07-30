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

public enum PushDialogOutcome
{
    OK,
    Canceled
}

public class PushDialogResult {
    public PushDialogOutcome Outcome { get; set; } = PushDialogOutcome.Canceled;
    public string ChangeDescription { get; set; } = string.Empty;
}

public partial class PushDialogControl : UserControl
{
    private EventHandler<PopupControlResult>? _currentDialogHandler;

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static readonly StyledProperty<bool> ShowDialogBoxProperty =
    AvaloniaProperty.Register<PushDialogControl, bool>(nameof(ShowDialog), defaultValue: false);

    public bool ShowDialog
    {
        get => GetValue(ShowDialogBoxProperty);

        set
        {
            SetValue(ShowDialogBoxProperty, value);
            OnPropertyChanged(nameof(ShowDialog));
        }
    }

    public PushDialogControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    public static async Task<PushDialogResult> ShowDialogAsync(Panel parent)
    {
        var dialog = new PushDialogControl();

        var tcs = new TaskCompletionSource<PushDialogResult>();

        // Unsubscribe previous handler if it exists
        if (dialog._currentDialogHandler != null)
        {
            dialog.popupDialog.DialogChoice -= dialog._currentDialogHandler;
            dialog._currentDialogHandler = null;
        }

        // Store the handler reference so we can unsubscribe later
        dialog._currentDialogHandler = (sender, result) =>
        {
            dialog.ShowDialog = false;
            dialog.OnPropertyChanged(nameof(ShowDialog));

            // Unsubscribe after handling to prevent leaks
            dialog.popupDialog.DialogChoice -= dialog._currentDialogHandler;
            dialog._currentDialogHandler = null;

            var pushResult = new PushDialogResult();

            if (result == PopupControlResult.OK && !String.IsNullOrEmpty(dialog.textboxChangeDescription.Text))
            {
                pushResult.ChangeDescription = dialog.textboxChangeDescription.Text;
                pushResult.Outcome = PushDialogOutcome.OK;
            }

            parent.Children.Remove(dialog);
            tcs.SetResult(pushResult);
        };

        dialog.popupDialog.DialogChoice += dialog._currentDialogHandler;

        dialog.ShowDialog = true;
        dialog.OnPropertyChanged(nameof(ShowDialog));

        parent.Children.Add(dialog);

        return await tcs.Task;
    }
}