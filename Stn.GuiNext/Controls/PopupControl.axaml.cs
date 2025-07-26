using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Stn.GuiNext;

public partial class PopupControl : UserControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<PopupControl, bool>(nameof(IsOpen), true);

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public PopupControl()
    {
        InitializeComponent();

        this.GetObservable(IsOpenProperty).Subscribe(new IsOpenObserver(this));
    }

    private class IsOpenObserver : IObserver<bool>
    {
        private readonly PopupControl _parent;
        public IsOpenObserver(PopupControl parent) => _parent = parent;

        public void OnNext(bool value)
        {
            _parent.IsVisible = value ? true : false;
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}