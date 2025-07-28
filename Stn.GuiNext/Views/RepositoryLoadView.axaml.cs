using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Stn.Core.SyncPoints;
using Stn.Core.UX;
using Stn.GuiNext.ViewModels;
using System;

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

                        ViewModel.Repository = Repository.Initialize(localPath, dialogResult.RemotePath, dialogResult.IsCompressed);

                        if (ViewModel == null) return;
                    }
                    else
                    {
                        //User chose to not further configure stuff 
                        return;
                    }
                } 
            
                ViewModel.SwitchTo(ViewModel.RepositoryView);
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }
    }
}