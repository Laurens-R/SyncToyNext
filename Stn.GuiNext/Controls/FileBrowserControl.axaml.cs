using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Splat.ModeDetection;
using Stn.Core;
using Stn.Core.IO;
using Stn.Core.SyncPoints;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    private string _browserPath = "C:\\"; //TODO: we need to do something with this before creating the MacOS and Linux release.

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

    public static readonly StyledProperty<FileBrowserMode> BrowserModeProperty =
    AvaloniaProperty.Register<FileBrowserControl, FileBrowserMode>(nameof(BrowserMode), FileBrowserMode.FoldersAndFiles);

    public FileBrowserMode BrowserMode
    {
        get
        {
            return GetValue(BrowserModeProperty);
        }
        set
        {
            SetValue(BrowserModeProperty, value);
            if (_browser != null)
            {
                _browser.BrowserMode = value;
                _browser.Refresh();
            }
            
            OnPropertyChanged(nameof(BrowserMode));
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

    public Repository? AssociatedRepository
    {
        get; set;
    }

    public SyncPoint? CurrentSyncPoint
    {
        get; set;
    }

    public event EventHandler<string>? CurrentPathChanged;

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

    public bool WatcherEnabled
    {
        get
        {
            var fsBrowser = _browser as FileSystemBrowser;
            return fsBrowser?.WatcherEnabled ?? false;
        }

        set
        {
            var fsBrowser = _browser as FileSystemBrowser;
            if (fsBrowser == null) return;
            fsBrowser.WatcherEnabled = value;
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

    public void Refresh()
    {
        if(_browser != null && CurrentSyncPoint != null)
        {
            if (BrowserType == FileBrowserControlType.Repository)
            {
                var _repBrowser = (RepositoryBrowser)_browser;
                _repBrowser.CurrentSyncPoint = CurrentSyncPoint;

                //we return because setting the CurrentSyncPoint on the repository browser triggers an internal refresh already.
                //doesn't make sense to do it twice.
                return;
            }
            else
            {
                _browser.Refresh();
            }
        }
    }

    private void BrowserGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        var row = browserGrid.SelectedItem;
        if (row is FileBrowserEntry entry && entry != null  && _browser != null)
        {
            if (!entry.IsFile && entry.Name == "..")
            {
                _browser.NavigateUp();
                OnPropertyChanged(nameof(Entries));
                OnPropertyChanged(nameof(CurrentPath));
                CurrentPathChanged?.Invoke(this, CurrentPath);
                return;
            }

            if (!entry.IsFile)
            {
                _browser.NavigateTo(entry);
                OnPropertyChanged(nameof(Entries));
                OnPropertyChanged(nameof(CurrentPath));
                CurrentPathChanged?.Invoke(this, CurrentPath);
                return;
            }

            string previewPath = string.Empty;

            if (BrowserType == FileBrowserControlType.FileSystem)
            {
                previewPath = entry.Path;
            } else if (BrowserType == FileBrowserControlType.Repository)
            {
                if(AssociatedRepository != null && CurrentSyncPoint != null && entry.Tag != null)
                {
                    previewPath = AssociatedRepository.GetTempCopyOfFile((SyncPointEntry)entry.Tag, CurrentSyncPoint);
                }
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = previewPath,
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
            var fsBrowser = (FileSystemBrowser)_browser;
            fsBrowser.OnFileCreatedHandler += FsBrowser_OnFileCreatedHandler;
            fsBrowser.OnFileRemovedHandler += FsBrowser_OnFileRemovedHandler;
            fsBrowser.OnFileRenamedHandler += FsBrowser_OnFileRenamedHandler;
        }
        else if (BrowserType == FileBrowserControlType.CompressedArchive)
        {
            _browser = new ZipArchiveBrowser(BrowserPath);
        }
        else if (BrowserType == FileBrowserControlType.Repository && AssociatedRepository != null)
        {
            _browser = new RepositoryBrowser(AssociatedRepository);
            CurrentSyncPoint = AssociatedRepository.SyncPoints.First(sp => sp.SyncPointId == AssociatedRepository.LocalSyncPointID);

            if (CurrentSyncPoint == null) throw new InvalidOperationException("Syncpoint cannot be null here.");
        }

        if (_browser == null) throw new InvalidOperationException("Browser should be set at this point.");

        _browser.BrowserMode = BrowserMode;

        OnPropertyChanged(nameof(Entries));
    }

    private void FsBrowser_OnFileRenamedHandler(object? sender, System.IO.RenamedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _browser?.Refresh();
        });
    }

    private void FsBrowser_OnFileRemovedHandler(object? sender, System.IO.FileSystemEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _browser?.Refresh();
        });
    }

    private void FsBrowser_OnFileCreatedHandler(object? sender, System.IO.FileSystemEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _browser?.Refresh();
        });
    }
}