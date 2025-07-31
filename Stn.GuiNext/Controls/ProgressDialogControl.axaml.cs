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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stn.GuiNext;

public partial class ProgressDialogControl : UserControl
{
    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static readonly StyledProperty<int> MaxValueProperty =
    AvaloniaProperty.Register<ProgressDialogControl, int>(nameof(MaxValue), defaultValue: 100);

    public int MaxValue
    {
        get => GetValue(MaxValueProperty);

        set
        {
            SetValue(MaxValueProperty, value);
            OnPropertyChanged(nameof(MaxValue));
        }
    }

    public static readonly StyledProperty<int> CurrentValueProperty =
    AvaloniaProperty.Register<ProgressDialogControl, int>(nameof(CurrentValue), defaultValue: 0);

    public int CurrentValue
    {
        get => GetValue(CurrentValueProperty);

        set
        {
            SetValue(CurrentValueProperty, value);
            OnPropertyChanged(nameof(CurrentValue));
        }
    }

    public static readonly StyledProperty<string> StatusProperty =
    AvaloniaProperty.Register<ProgressDialogControl, string>(nameof(Status), defaultValue: "Please wait while the operation is completed...");

    public string Status
    {
        get => GetValue(StatusProperty);

        set
        {
            SetValue(StatusProperty, value);
            labelStatus.Content = value;
            OnPropertyChanged(nameof(Status));
        }
    }

    public static readonly StyledProperty<string> TitleProperty =
    AvaloniaProperty.Register<ProgressDialogControl, string>(nameof(Title), defaultValue: "Working on it!");

    public string Title
    {
        get => GetValue(TitleProperty);

        set
        {
            SetValue(TitleProperty, value);
            OnPropertyChanged(nameof(Title));
        }
    }

    public ProgressDialogControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}