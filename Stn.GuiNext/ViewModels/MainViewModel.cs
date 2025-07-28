using Avalonia.Controls;
using ReactiveUI;
using Stn.Core.SyncPoints;
using Stn.GuiNext.Views;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace Stn.GuiNext.ViewModels;

public class MainViewModel : ViewModelBase
{
    public const int RepositoryLoadViewIndex = 0;
    public const int RepositoryViewIndex = 1;

    public ObservableCollection<UserControl> Views { get; } = new()
    {
        new RepositoryLoadView(),
        new RepositoryView()
    };

    public RepositoryLoadView? RepositoryLoadView
    {
        get
        {
            return Views[RepositoryLoadViewIndex] as RepositoryLoadView;
        }
    }

    public RepositoryView? RepositoryView
    {
        get
        {
            return Views[RepositoryViewIndex] as RepositoryView;
        }
    }

    public Repository? Repository
    {
        get; set;
    }

    private UserControl _currentView;
    public UserControl CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public void SwitchTo(int index)
    {
        if (index >= 0 && index < Views.Count)
            CurrentView = Views[index];
    }

    public void SwitchTo(UserControl? control)
    {
        if (control == null) return;
        if(Views.Contains(control))
        {
            CurrentView = control;
        }
    }

    public MainViewModel()
    {
        _currentView = Views[RepositoryLoadViewIndex];

        if (RepositoryView != null)
        {
            RepositoryView.ViewModel = this;
        }

        if(RepositoryLoadView != null)
        {
            RepositoryLoadView.ViewModel = this;
        }

        CurrentView = Views[RepositoryLoadViewIndex];
    }

}
