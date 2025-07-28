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

    public ProgressDialogControl()
    {
        InitializeComponent();
        DataContext = this;
    }
}