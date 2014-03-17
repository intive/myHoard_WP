﻿using Caliburn.Micro;
using MyHoard.Services;
using MyHoard.Views;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MyHoard.ViewModels
{
    public class LoginViewModel : ViewModelBase, IHandle<IRestResponse>, IHandle<ServiceErrorMessage>
    {

        private readonly IEventAggregator eventAggregator;
        private Dictionary<string, string> backends;
        private bool canLogin;
        private bool isFormAccessible;
        private Visibility isProgressBarVisible;
        private string userName;
        private string selectedBackend;
        private PasswordBox passwordBox;
        private RestRequestAsyncHandle asyncHandle;

        public LoginViewModel(INavigationService navigationService, CollectionService collectionService, IEventAggregator eventAggregator)
            : base(navigationService, collectionService)
        {
            Backends = ConfigurationService.Backends;
            SelectedBackend = Backends.Keys.First();
            this.eventAggregator = eventAggregator;
            eventAggregator.Subscribe(this);
            IsFormAccessible = true;
        }

        public void OnGoBack(CancelEventArgs eventArgs)
        {
            if (!IsFormAccessible)
            {
                MessageBoxResult messageResult = MessageBox.Show(Resources.AppResources.CancelConfirm, "", MessageBoxButton.OKCancel);
                if (messageResult == MessageBoxResult.Cancel)
                {
                    eventArgs.Cancel = true;
                }
                else
                {
                    asyncHandle.Abort();
                }
            }
        }

        public void Handle(IRestResponse response)
        {
            IsFormAccessible = true;
            CanLogin = true;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show(Resources.AppResources.LoginSuccess);
                //TODO: Parse tokens
                NavigationService.UriFor<CollectionListViewModel>().Navigate();
                while (NavigationService.BackStack.Any())
                {
                    this.NavigationService.RemoveBackEntry();
                }

            }
            else
            {
                string message = Resources.AppResources.ErrorOccurred;
                if (!String.IsNullOrEmpty(response.Content))
                {
                    try
                    {
                        JObject parsedResponse = JObject.Parse(response.Content);
                        message += ": " + parsedResponse["error_message"];
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
                MessageBox.Show(message);
            }
        }

        public void Handle(ServiceErrorMessage message)
        {
            IsFormAccessible = true; ;
            CanLogin = true;
            MessageBox.Show(message.Content);
        }


        protected override void OnDeactivate(bool close)
        {
            eventAggregator.Unsubscribe(this);
            base.OnDeactivate(close);
        }


        protected override void OnActivate()
        {
            eventAggregator.Subscribe(this);
            base.OnActivate();
        }


        protected override void OnViewLoaded(object view)
        {
            passwordBox = ((LoginView)view).Password;
            passwordBox.PasswordChanged += new RoutedEventHandler(PasswordChanged);
            base.OnViewLoaded(view);
        }

        public void PasswordChanged(object sender, RoutedEventArgs e)
        {
            DataChanged();
        }

        public void DataChanged()
        {
            CanLogin = (!String.IsNullOrWhiteSpace(UserName) && !String.IsNullOrWhiteSpace(passwordBox.Password));
        }

        public async void Login()
        {
            IsFormAccessible = false;
            RegistrationService registrationService = new RegistrationService(Backends[SelectedBackend]);
            asyncHandle = registrationService.Login(UserName, passwordBox.Password);
        }

        public string UserName
        {
            get { return userName; }
            set
            {
                userName = value;
                NotifyOfPropertyChange(() => UserName);
            }
        }


        public string SelectedBackend
        {
            get { return selectedBackend; }
            set
            {
                selectedBackend = value;
                NotifyOfPropertyChange(() => SelectedBackend);
            }
        }

        public bool CanLogin
        {
            get { return canLogin; }
            set
            {
                canLogin = value;
                NotifyOfPropertyChange(() => CanLogin);
            }
        }

        public bool IsFormAccessible
        {
            get { return isFormAccessible; }
            set
            {
                isFormAccessible = value;
                NotifyOfPropertyChange(() => IsFormAccessible);
                if (!value)
                    CanLogin = false;
                IsProgressBarVisible = (IsFormAccessible ? Visibility.Collapsed : Visibility.Visible);
            }
        }

        public Visibility IsProgressBarVisible
        {
            get { return isProgressBarVisible; }
            set
            {
                isProgressBarVisible = value;
                NotifyOfPropertyChange(() => IsProgressBarVisible);
            }
        }


        public Dictionary<string, string> Backends
        {
            get { return backends; }
            set
            {
                backends = value;
                NotifyOfPropertyChange(() => Backends);
            }
        }

    }
}