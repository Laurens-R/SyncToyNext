using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Stn.Core;
using Stn.Core.SyncPoints;
using Stn.GuiNext.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Stn.GuiNext.Views;

public partial class RepositoryView : UserControl
{
    public static readonly StyledProperty<MainViewModel> ViewModelProperty =
    AvaloniaProperty.Register<FileBrowserControl, MainViewModel>(nameof(ViewModel));

    public MainViewModel ViewModel
    {
        get
        {
            return GetValue(ViewModelProperty);
        }
        set
        {
            SetValue(ViewModelProperty, value);
        }
    }

    public RepositoryView()
    {
        InitializeComponent();
        menuCloseRepo.Tapped += MenuCloseRepo_Tapped;
        buttonPush.Tapped += ButtonPush_Tapped;
        buttonRestore.Tapped += ButtonRestore_Tapped;
        buttonRestoreSingle.Tapped += ButtonRestoreSingle_Tapped;
        comboRemoteSyncpoints.SelectionChanged += ComboRemoteSyncpoints_SelectionChanged;
    }

    private async void MenuCloseRepo_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, "Are you sure you want to close this repository?", "Are you sure?", MessageBoxOptions.YesNo);
        if (ViewModel != null && result == PopupControlResult.Yes)
        {
            ViewModel.SwitchTo(ViewModel.RepositoryLoadView);
        }
    }

    private void ComboRemoteSyncpoints_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedSyncPoint = comboRemoteSyncpoints.SelectedItem as SyncPoint;

        if(selectedSyncPoint != null && ViewModel != null && ViewModel.Repository != null)
        {
            remoteFileBrowser.CurrentSyncPoint = selectedSyncPoint;
            remoteFileBrowser.Refresh();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (ViewModel != null && ViewModel.Repository != null)
        {
            Repository.UpdateProgressHandler = (int current, int max, string message) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    progressDialog.MaxValue = max;
                    progressDialog.CurrentValue = current;
                    progressDialog.Status = message;
                });
            };

            localFileBrowser.BrowserPath = ViewModel.Repository.LocalPath;
            localFileBrowser.WatcherEnabled = true;
            RefreshLocalSyncPointLabel();

            remoteFileBrowser.BrowserType = FileBrowserControlType.Repository;
            remoteFileBrowser.AssociatedRepository = ViewModel.Repository;
            remoteFileBrowser.BrowserPath = ViewModel.Repository.RemotePath;
            RefreshRemoteSyncPoints();
        }
    }

    private void RefreshLocalSyncPointLabel()
    {
        if (ViewModel != null && ViewModel.Repository != null)
        {
            localSyncPointLabel.Content = ViewModel.Repository.SyncPoints.Single(sp => sp.SyncPointId == ViewModel.Repository.LocalSyncPointID);
        }
    }

    private void RefreshRemoteSyncPoints()
    {
        if (ViewModel != null && ViewModel.Repository != null)
        {
            comboRemoteSyncpoints.ItemsSource = ViewModel.Repository.SyncPoints;
            if (comboRemoteSyncpoints.Items.Count > 0)
            {
                comboRemoteSyncpoints.SelectedIndex = 0;
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }

    private async void ButtonRestoreSingle_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, "Are you sure you want to restore the selected files from this syncpoint? It will undo all your local changes since that syncpoint to the files that you have selected.", "Are you sure?", MessageBoxOptions.YesNo);

        if (result == PopupControlResult.Yes)
        {
            var repository = ViewModel.Repository;
            var selectedSyncPoint = comboRemoteSyncpoints.SelectedItem as SyncPoint;
            var selectedItems = remoteFileBrowser.SelectedItems;

            if (repository != null && selectedSyncPoint != null && selectedItems != null)
            {
                progressDialog.Title = "Restoring files to local";
                progressDialog.IsVisible = true;

                var task = Task.Run(() =>
                {
                    foreach (var item in selectedItems)
                    {
                        repository.RestoreSingleFile(selectedSyncPoint.SyncPointId, item.RelativePath);
                    }
                
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        RefreshLocalSyncPointLabel();
                        progressDialog.IsVisible = false;
                    });
                });
            }
        }
    }

    private async void ButtonRestore_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, "Are you sure you want to restore this syncpoint? It will undo all your local changes since that syncpoint.", "Are you sure?", MessageBoxOptions.YesNo);
        
        if (result == PopupControlResult.Yes)
        {
            var repository = ViewModel.Repository;
            var selectedSyncPoint = comboRemoteSyncpoints.SelectedItem as SyncPoint;

            if (repository != null && selectedSyncPoint != null)
            {
                progressDialog.Title = "Restoring to local";
                progressDialog.IsVisible = true;

                var task = Task.Run(() =>
                {
                    repository.Restore(selectedSyncPoint.SyncPointId);

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        RefreshLocalSyncPointLabel();
                        progressDialog.IsVisible = false;
                    });
                });
            }
        }
    }

    private async void ButtonPush_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await PushDialogControl.ShowDialogAsync(mainRepositoryViewGrid);

        if (result.Outcome == PushDialogOutcome.OK && ViewModel != null && ViewModel.Repository != null) {
            progressDialog.Title = "Pusing to remote";
            progressDialog.IsVisible = true;

            var repository = ViewModel.Repository; //needed because of threading.

            var task = Task.Run(() =>
            {
                repository.Push(string.Empty, result.ChangeDescription);

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    RefreshLocalSyncPointLabel();
                    RefreshRemoteSyncPoints();

                    progressDialog.IsVisible = false;
                });
            });
        }
    }
}
