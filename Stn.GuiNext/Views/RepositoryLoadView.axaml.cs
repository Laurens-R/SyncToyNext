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
using Avalonia.Threading;
using Stn.Core.SyncPoints;
using Stn.Core.UX;
using Stn.GuiNext.ViewModels;
using System;
using System.Threading.Tasks;

namespace Stn.GuiNext;

public partial class RepositoryLoadView : UserControl
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


    public RepositoryLoadView()
    {
        InitializeComponent();
        buttonBrowseLocal.Tapped += ButtonBrowseLocal_Tapped;
        
        Repository.UpdateProgressHandler = async (int current, int max, string message) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                progressDialog.MaxValue = max;
                progressDialog.CurrentValue = current;
                progressDialog.Status = message;
            });
        };
    }

    private async void ButtonBrowseLocal_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var folderResult = await FolderBrowserDialog.ShowDialogAsync(mainRepoLoadGrid, "Select a local repository...");
        
        if(folderResult.Outcome == FolderBrowserDialogOutcome.OK)
        {
            try
            {
                var localPath = folderResult.SelectedFolder;

                try
                {
                    ViewModel.Repository = new Repository(localPath);
                    ViewModel.SwitchTo(ViewModel.RepositoryView);
                }
                catch
                {
                    if (await MessageBoxControl.ShowDialogAsync(mainRepoLoadGrid, $"Repository has not been initialized in {localPath}. Do you want to proceed to initialize it?", "Init repo?", MessageBoxOptions.YesNo) == PopupControlResult.Yes)
                    {
                        var dialogResult = await RemoteConfigurationDialogControl.ShowDialogAsync(mainRepoLoadGrid, "Configure remote...");    

                        if (dialogResult == null)
                        {
                            throw new InvalidOperationException("Remote must be specified");
                        }
                        if (dialogResult.Outcome == RemoteDialogOutcome.Ok && !String.IsNullOrWhiteSpace(dialogResult.RemotePath))
                        {
                            progressDialog.Title = "Initializing repository";
                            progressDialog.IsVisible = true;
                            
                            var task = Task.Run(async () =>
                            {
                                var newRepository = Repository.Initialize(localPath, dialogResult.RemotePath, dialogResult.IsCompressed);

                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    ViewModel.Repository = newRepository;
                                    progressDialog.IsVisible = false;
                                    ViewModel.SwitchTo(ViewModel.RepositoryView);
                                });
                            });
                        }
                    }
                    else
                    {
                        //User chose to not further configure stuff 
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }
    }
}