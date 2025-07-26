using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Splat.ModeDetection;
using Stn.Core.IO;
using Stn.Core.SyncPoints;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stn.GuiNext;

public enum FileBrowserControlType
{
    FileSystem,
    CompressedArchive,
    Repository
}

public partial class FileBrowserControl : UserControl, INotifyPropertyChanged
{
    private FileBrowser? _browser = null;
    private string _browserPath = "C:\\";

    public static readonly StyledProperty<FileBrowserControlType> BrowserTypeProperty =
    AvaloniaProperty.Register<FileBrowserControl, FileBrowserControlType>(nameof(BrowserType), FileBrowserControlType.FileSystem);

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FileBrowserControlType BrowserType
    {
        get
        {
            return GetValue(BrowserTypeProperty);
        }
        set
        {
            SetValue(BrowserTypeProperty, value);
        }
    }

    public string BrowserPath 
    { 
        get
        {
            return _browserPath;
        }

        set
        {
            _browserPath = value;
            InitializeBrowser();
            OnPropertyChanged(nameof(Entries));
        }
    }

    public string CurrentPath
    {
        get
        {
            if (_browser == null) return String.Empty;
            return _browser.CurrentPath;
        }
    }

    public ObservableCollection<FileBrowserEntry>? Entries
    {
        get
        {
            if (_browser != null) return _browser.AllEntries;
            return null;
        }
    }

    public FileBrowserControl()
    {
        InitializeBrowser();
        InitializeComponent();
        DataContext = this;
        browserGrid.DoubleTapped += BrowserGrid_DoubleTapped;
        
        //just temp: we can use this to do file dialogs etc.
        var toplevel = TopLevel.GetTopLevel(this);
        

    }

    private void BrowserGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        var row = browserGrid.SelectedItem;
        if (row is FileBrowserEntry entry && _browser != null)
        {
            if (!entry.IsFile && entry.Name == "..")
            {
                _browser.NavigateUp();
                OnPropertyChanged(nameof(Entries));
                OnPropertyChanged(nameof(CurrentPath));
                return;
            }

            if (!entry.IsFile)
            {
                _browser.NavigateTo(entry);
                OnPropertyChanged(nameof(Entries));
                OnPropertyChanged(nameof(CurrentPath));
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = entry.Path,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }

        }
    }

    private void InitializeBrowser()
    {
        if (BrowserType == FileBrowserControlType.FileSystem)
        {
            _browser = new FileSystemBrowser(BrowserPath);
        }
        else if (BrowserType == FileBrowserControlType.CompressedArchive)
        {
            _browser = new ZipArchiveBrowser(BrowserPath);
        }
        else if (BrowserType == FileBrowserControlType.Repository)
        {
            var repository = new Repository(BrowserPath);
            _browser = new RepositoryBrowser(repository);
        }
        OnPropertyChanged(nameof(Entries));
    }
}