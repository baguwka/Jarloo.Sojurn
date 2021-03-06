﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using Jarloo.Sojurn.InformationProviders;
using Jarloo.Sojurn.Models;
using Jarloo.Sojurn.Views;

namespace Jarloo.Sojurn.ViewModels
{
    [Export]
    public class AddShowViewModel : Screen
    {
        #region Properties

        private bool isSearchCompleted;
        private string showName;

        public BindableCollection<Show> Shows { get; set; }

        private bool isWorking;
        private Show selectedShow;
        private Show show;
        private string error;
        private List<Show> currentShows;
         
        public string Error
        {
            get { return error; }
            set
            {
                error = value;
                NotifyOfPropertyChange(()=>Error);
            }
        }
        
        public bool CanAddShow
        {
            get { return SelectedShow != null && isWorking == false; }
        }

        public Show Show
        {
            get { return show; }
            set
            {
                show = value;
                NotifyOfPropertyChange(() => Show);
            }
        }

        public Show SelectedShow
        {
            get { return selectedShow; }
            set
            {
                selectedShow = value;
                NotifyOfPropertyChange(() => SelectedShow);
                NotifyOfPropertyChange(() => CanAddShow);
            }
        }


        public bool IsWorking
        {
            get { return isWorking; }
            set
            {
                isWorking = value;
                NotifyOfPropertyChange(() => IsWorking);
                NotifyOfPropertyChange(() => CanAddShow);
            }
        }

        public bool IsSearchCompleted
        {
            get { return isSearchCompleted; }
            set
            {
                isSearchCompleted = value;
                NotifyOfPropertyChange(() => IsSearchCompleted);
            }
        }


        public string ShowName
        {
            get { return showName; }
            set
            {
                showName = value;
                NotifyOfPropertyChange(() => ShowName);
            }
        }

        #endregion

        private readonly IInformationProvider informationProvider;

        [ImportingConstructor]
        public AddShowViewModel(IInformationProvider infoProvider, List<Show> currentShows)
        {
            Shows = new BindableCollection<Show>();
            informationProvider = infoProvider;
            DisplayName = "";
            this.currentShows = currentShows;
        }

        public void Cancel()
        {
            TryClose(false);
        }
        
        public async void SearchShow()
        {
            IsSearchCompleted = false;
            IsWorking = true;
            
            string query = ShowName;
            List<Show> shows = await Task<List<Show>>.Factory.StartNew(() =>
                {
                    try
                    {
                        return informationProvider.GetShows(query);
                    }
                    catch
                    {
                        return null;
                    }
                });

            IsWorking = false;
            
            Execute.BeginOnUIThread(() =>
                {
                    Shows.Clear();
                    IsSearchCompleted = true;

                    if (shows == null)
                    {
                        Error = "Provider failed to return information.";
                        return;
                    }
                    
                    Error = null;
                    
                    foreach (Show show in shows)
                    {
                        Shows.Add(show);
                    }
                });
        }

        public async void AddShow()
        {
            if (SelectedShow == null) return;

            if (currentShows.Any(w => w.ShowId == SelectedShow.ShowId))
            {
                Error = "Show is already in your collection.";
                ShowName = string.Empty;
                IsSearchCompleted = false;
                SelectedShow = null;
                return;
            }

            Execute.BeginOnUIThread(() =>
                {
                    IsWorking = true;
                    IsSearchCompleted = false;
                });
            
            Show newShow = await Task<Show>.Factory.StartNew(() =>
                {
                    try
                    {
                        return informationProvider.GetFullDetails(SelectedShow.ShowId);
                    }
                    catch
                    {
                        return null;
                    }
                });
            
            Show = newShow;

            if (newShow != null)
            {
                Error = null;
                TryClose(true);
            }
            else
            {
                Error = "Provider failed to return information.";
            }
        }

        public void TextModified(KeyEventArgs e)
        {
            if (e.Key == Key.Return) SearchShow();
        }
    }
}