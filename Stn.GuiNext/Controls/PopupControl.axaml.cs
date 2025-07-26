using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stn.GuiNext;

public partial class PopupControl : ContentControl
{
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

    public PopupControl()
    {
        InitializeComponent();
    }
}