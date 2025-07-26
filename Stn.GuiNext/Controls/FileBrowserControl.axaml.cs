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

namespace Stn.GuiNext;

public enum FileBrowserControlType
{
    FileSystem,
    CompressedArchive,
    Repository
}

public partial class FileBrowserControl : UserControl
{
    private FileBrowser? _browser = null;
    private string _browserPath = "C:\\";

    public static readonly StyledProperty<FileBrowserControlType> BrowserTypeProperty =
    AvaloniaProperty.Register<FileBrowserControl, FileBrowserControlType>(nameof(BrowserType), FileBrowserControlType.FileSystem);

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
        browserGrid.PointerPressed += OnBrowserGridPointerPressed;
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
    }

    private void OnBrowserGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            var row = (e.Source as Visual)?.GetVisualParent<DataGridRow>();
            if (row?.DataContext is FileBrowserEntry entry && _browser != null)
            {
                if(!entry.IsFile && entry.Name == "..")
                {
                    _browser.NavigateUp();
                    return;
                }

                if (!entry.IsFile)
                {
                    _browser.NavigateTo(entry);
                    return;
                }

                //at this point the entry is a file... so we can use the default file handler.
                //TODO:implement file handler.
                
            }
        }
    }

}