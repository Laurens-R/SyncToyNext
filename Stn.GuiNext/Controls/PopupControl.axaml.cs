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
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Stn.GuiNext;

public enum PopupControlResult
{
    OK,
    Cancel,
    Yes,
    No
}

public partial class PopupControl : ContentControl
{
    private Button? _buttonCancel;
    private Button? _buttonOK;
    private Button? _buttonYes;
    private Button? _buttonNo;

    public event EventHandler<PopupControlResult>? DialogChoice;

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static readonly StyledProperty<string> TitleProperty =
    AvaloniaProperty.Register<PopupControl, string>(nameof(Title), "Modal Dialog");

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<bool> ShowPopupProperty =
    AvaloniaProperty.Register<PopupControl, bool>(nameof(ShowPopup), true);

    public bool ShowPopup {
        get => GetValue(ShowPopupProperty);
        set => SetValue(ShowPopupProperty, value);
    }

    public static readonly StyledProperty<bool> HasYesProperty =
    AvaloniaProperty.Register<PopupControl, bool>(nameof(HasYes), false);

    public bool HasYes
    {
        get => GetValue(HasYesProperty);
        set => SetValue(HasYesProperty, value);
    }

    public static readonly StyledProperty<bool> HasNoProperty =
    AvaloniaProperty.Register<PopupControl, bool>(nameof(HasNo), false);

    public bool HasNo
    {
        get => GetValue(HasNoProperty);
        set => SetValue(HasNoProperty, value);
    }

    public static readonly StyledProperty<bool> HasOKProperty =
    AvaloniaProperty.Register<PopupControl, bool>(nameof(HasOK), false);

    public bool HasOK
    {
        get => GetValue(HasOKProperty);
        set => SetValue(HasOKProperty, value);
    }

    public static readonly StyledProperty<bool> HasCancelProperty =
    AvaloniaProperty.Register<PopupControl, bool>(nameof(HasCancel), false);

    public bool HasCancel
    {
        get => GetValue(HasCancelProperty);
        set => SetValue(HasCancelProperty, value);
    }


    public static readonly StyledProperty<int> PopupWidthProperty =
    AvaloniaProperty.Register<PopupControl, int>(nameof(PopupWidth), 800);

    public int PopupWidth
    {
        get => GetValue(PopupWidthProperty);
        set => SetValue(PopupWidthProperty, value);
    }

    public static readonly StyledProperty<int> PopupHeightProperty =
    AvaloniaProperty.Register<PopupControl, int>(nameof(PopupHeight), 400);

    public int PopupHeight
    {
        get => GetValue(PopupHeightProperty);
        set => SetValue(PopupHeightProperty, value);
    }

    public async Task<PopupControlResult> ShowDialogAsync()
    {
        var tcs = new TaskCompletionSource<PopupControlResult>();

        ShowPopup = true;
        OnPropertyChanged(nameof(ShowPopup));

        DialogChoice = (sender, result) =>
        {
            ShowPopup = false;
            OnPropertyChanged(nameof(ShowPopup));
            tcs.SetResult(result);
        };

        return await tcs.Task;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Find and store the button reference
        _buttonOK = e?.NameScope.Find<Button>("buttonOK");
        _buttonCancel = e?.NameScope.Find<Button>("buttonCancel");
        _buttonYes = e?.NameScope.Find<Button>("buttonYes");
        _buttonNo = e?.NameScope.Find<Button>("buttonNo");

        if (_buttonOK != null)
        {
            _buttonOK.Click += _buttonOK_Click;
        }

        if (_buttonCancel != null)
        {
            _buttonCancel.Click += _buttonCancel_Click;
        }

        if (_buttonYes != null)
        {
            _buttonYes.Click += _buttonYes_Click;
        }

        if (_buttonNo != null)
        {
            _buttonNo.Click += _buttonNo_Click;
        }
    }

    private void _buttonNo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogChoice?.Invoke(this, PopupControlResult.No);
    }

    private void _buttonYes_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogChoice?.Invoke(this, PopupControlResult.Yes);
    }

    private void _buttonCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogChoice?.Invoke(this, PopupControlResult.Cancel);
    }

    private void _buttonOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogChoice?.Invoke(this, PopupControlResult.OK);
    }

    public PopupControl()
    {
        InitializeComponent();
    }
}