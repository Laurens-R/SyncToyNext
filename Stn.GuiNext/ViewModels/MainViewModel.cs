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
