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
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Kernel;
using Stn.Core;
using Stn.Core.SyncPoints;
using Stn.GuiNext.ViewModels;
using System;
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
        DataContext = this;
        menuCloseRepo.Tapped += MenuCloseRepo_Tapped;
        buttonPush.Tapped += ButtonPush_Tapped;
        buttonRestore.Tapped += ButtonRestore_Tapped;
        buttonRestoreSingle.Tapped += ButtonRestoreSingle_Tapped;
        buttonRevertToSyncpoint.Tapped += ButtonRevertToSyncpoint_Tapped;
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

            iconGit.IsVisible = ViewModel.Repository.GitPresent;
        }
    }

    private async void RefreshLocalSyncPointLabel()
    {
        if (ViewModel != null && ViewModel.Repository != null)
        {
            if (!ViewModel.Repository.Manager.RefreshSyncPoints())
            {
                throw new InvalidOperationException("Failed to refresh sync points.");
            }

            if (ViewModel.Repository.SyncPoints.Count > 0)
            {
                var sp  = ViewModel.Repository.SyncPoints.SingleOrDefault(sp => sp.SyncPointId == ViewModel.Repository.LocalSyncPointID);

                if (sp != null)
                {
                    localSyncPointLabel.Content = sp;
                }
                else
                {
                    await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, "Local syncpoint is longer available on the remote. This can happen if the remote syncpoint has been deleted outside of this program or if something went wrong earlier. The program will now perform a push to ensure that no local progress will be lost and that the local and remote are aligned again.", "Oops", MessageBoxOptions.OK);
                    await PushLocalChanges();
                }
            }
        }
    }

    private async void RefreshRemoteSyncPoints()
    {
        try
        {
            //ok this is ugly, but the eventhandler handles the source collection which is used here for the datasource
            //which in turns causes the data collection not to be ready by the time we set the item source here. So...
            //as a hack we temporarily detach the event handler, change the data source and then set Selected Index.
            //this is super ugly and must be fixed in the future.
            comboRemoteSyncpoints.SelectionChanged -= ComboRemoteSyncpoints_SelectionChanged;

            if (ViewModel != null && ViewModel.Repository != null)
            {
                var syncPointToBeSelected = ViewModel.Repository.SyncPoints.SingleOrDefault(sp => sp.SyncPointId == ViewModel.Repository.LocalSyncPointID);

                var syncpoints = ViewModel.Repository.SyncPoints;
                comboRemoteSyncpoints.ItemsSource = syncpoints;
                comboRemoteSyncpoints.SelectionChanged += ComboRemoteSyncpoints_SelectionChanged;

                if (syncPointToBeSelected != null)
                {                    
                    var currentIndex = ViewModel.Repository.SyncPoints.IndexOf(syncPointToBeSelected);

                    if (comboRemoteSyncpoints.Items.Count > 0)
                    {
                        comboRemoteSyncpoints.SelectedIndex = currentIndex;
                    }
                }
            }
        } catch (Exception ex)
        {
            await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, $"Something went wrong while refreshing the syncpoints.", "Oops", MessageBoxOptions.OK);
        } finally
        {
            
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }

    private async void ButtonRestoreSingle_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var selectedItems = remoteFileBrowser.SelectedItems;

        if (selectedItems?.Count() == 0) return;

        var result = await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, "Are you sure you want to restore the selected files from this syncpoint? It will undo all your local changes since that syncpoint to the files that you have selected.", "Are you sure?", MessageBoxOptions.YesNo);

        if (result == PopupControlResult.Yes)
        {
            var repository = ViewModel.Repository;
            var selectedSyncPoint = comboRemoteSyncpoints.SelectedItem as SyncPoint;
            
            if (repository != null && selectedSyncPoint != null && selectedItems != null)
            {
                progressDialog.Title = "Restoring files to local";
                progressDialog.IsVisible = true;

                var task = Task.Run(async () =>
                {
                    try { 
                        await repository.RestoreMultipleEntriesFromSyncPoint(selectedItems.Select(item => item.RelativePath), selectedSyncPoint);
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, $"Something went wrong while restoring the selected files from the syncpoint: {ex.Message}", "Oops", MessageBoxOptions.OK);
                        });
                    }
                    
                    await Dispatcher.UIThread.InvokeAsync(() =>
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

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await repository.Restore(selectedSyncPoint.SyncPointId);
                    } catch (Exception ex) 
                    { 
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, $"Something went wrong while restoring the syncpoint: {ex.Message}", "Oops", MessageBoxOptions.OK);
                        });
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
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
        await PushLocalChanges();
    }

    private async Task PushLocalChanges()
    {
        var result = await PushDialogControl.ShowDialogAsync(mainRepositoryViewGrid);

        if (result.Outcome == PushDialogOutcome.OK && ViewModel != null && ViewModel.Repository != null)
        {
            progressDialog.Title = "Pusing to remote";
            progressDialog.IsVisible = true;

            var repository = ViewModel.Repository; //needed because of threading.

            var task = Task.Run(async () =>
            {
                try
                {
                    await repository.Push(string.Empty, result.ChangeDescription);
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, $"Something went wrong while pusing the changes to the remote: {ex.Message}", "Oops", MessageBoxOptions.OK);
                    });
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    RefreshLocalSyncPointLabel();
                    RefreshRemoteSyncPoints();

                    progressDialog.IsVisible = false;
                });
            });
        }
    }

    private async void ButtonRevertToSyncpoint_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var result = await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, "Are you sure you want to revert back to this syncpoint? This means that all syncpoints that came after it will be permanently removed.", "Are you sure?", MessageBoxOptions.YesNo);

        if (result == PopupControlResult.Yes)
        {
            var repository = ViewModel.Repository;
            var selectedSyncPoint = comboRemoteSyncpoints.SelectedItem as SyncPoint;

            if (repository != null && selectedSyncPoint != null)
            {
                progressDialog.Title = "Restoring to local";
                progressDialog.IsVisible = true;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await repository.RevertToSyncpoint(selectedSyncPoint.SyncPointId);
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            await MessageBoxControl.ShowDialogAsync(mainRepositoryViewGrid, $"Something went wrong while restoring the syncpoint: {ex.Message}", "Oops", MessageBoxOptions.OK);
                        });
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        RefreshRemoteSyncPoints();
                        progressDialog.IsVisible = false;
                    });
                });
            }
        }
    }
}
