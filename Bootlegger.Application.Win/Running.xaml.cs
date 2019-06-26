﻿using Bootlegger.App.Lib;
using Bootlegger.App.Win.locale;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Bootlegger.App.Lib.BootleggerApplication;

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for Running.xaml
    /// </summary>
    public partial class Running
    {
        public Running()
        {
            InitializeComponent();
            Loaded += Running_Loaded;
            Unloaded += Running_Unloaded;

            //Browser.SnapsToDevicePixels = true;
            //Browser.UseLayoutRounding = true;
            Browser.LoadingStateChanged += Browser_LoadingStateChanged;
            Browser.TitleChanged += Browser_TitleChanged;
        }

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                pagetitle.Text = Browser.Title;
            });
        }

        private void Browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Browser.Address.Equals("ourstory://videos"))
                {
                    App.BootleggerApp.OpenFolder();
                }
                Console.WriteLine(Browser.Address);
            });

        }

        ObservableCollection<Docker.DotNet.Models.ContainerListResponse> ContainerStatus { get; set; }

        private void Running_Unloaded(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        CancellationTokenSource cts = new CancellationTokenSource();

        async void Start()
        {
            progress.Content = Strings.StartingApplication;
            progresswrapper.Visibility = Visibility.Visible;
            //Browser.Address = "google.com";
            try
            {
                if (await App.BootleggerApp.RunServer(cts.Token))
                {
                    progress.Content = Strings.Running;
                    sharewarning.Visibility = Visibility.Collapsed;
                    //progressring.Visibility = Visibility.Collapsed;
                    var culture = Thread.CurrentThread.CurrentUICulture;
                    Browser.Address = $"http://localhost:{BootleggerApplication.PORT}/auth/locale/{culture.TwoLetterISOLanguageName}";
                    statusled.Background = FindResource("green") as Brush;
                    ledshadow.Color = Colors.Green;
                    progresswrapper.Visibility = Visibility.Collapsed;
                }
                else
                {
                    progress.Content = Strings.ProblemStartingApplication;
                    sharewarning.Visibility = Visibility.Collapsed;
                    //progressring.Visibility = Visibility.Collapsed;
                    statusled.Background = FindResource("red") as Brush;
                    ledshadow.Color = Colors.Red;

                    progresswrapper.Visibility = Visibility.Collapsed;
                }
            }
            catch (FileLoadException)
            {
                //show video panel
                sharewarning.Visibility = Visibility.Visible;
                progresswrapper.Visibility = Visibility.Collapsed;
                video.Play();

                // await check on if its enabled again:
                //  await App.BootleggerApp.CheckSharedDrives();

                await Task.Delay(10000);

                Start();
            }


        }

        private void Running_Loaded(object sender, RoutedEventArgs e)
        {
            Start();
            //App.BootleggerApp.OnContainersChanged += BootleggerApp_OnContainersChanged;
            App.BootleggerApp.OnRunWarning += BootleggerApp_OnRunWarning;
        }

        List<NamedException> CurrentErrors;

        private void BootleggerApp_OnRunWarning(List<NamedException> obj)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    CurrentErrors = obj;

                    foreach(var err in CurrentErrors)
                    {
                        switch (err)
                        {
                            case IpException c:
                                err.Name = locale.Strings.IpErrorName;
                                err.Description = locale.Strings.IpErrorDesc;

                                break;

                            case ContainerException c:
                                err.Name = locale.Strings.AppErrorName;
                                err.Description = locale.Strings.AppErrorDesc;
                                break;
                            case ServerException c:
                                err.Name = locale.Strings.DashboardErrorName;
                                err.Description = locale.Strings.DashboardErrorDesc;
                                break;

                            case DriveException c:
                                err.Name = locale.Strings.DiskSpaceErrorName;
                                err.Description = locale.Strings.DiskSpaceErrorDesc;
                                break;

                            case WiFiException c:
                                err.Name = locale.Strings.WiFiErrorName;
                                err.Description = locale.Strings.WiFiErrorDesc;
                                break;
                        }
                    }


                    errors.ItemsSource = null;
                    errors.ItemsSource = CurrentErrors;

                    if (CurrentErrors.Count > 0)
                    {
                        statusled.Background = FindResource("red") as Brush;
                        ledshadow.Color = Colors.Red;
                    }
                    else
                    {
                        statusled.Background = FindResource("green") as Brush;
                        ledshadow.Color = Colors.Green;
                    }
                });
            }
            catch { }
        }

        private void continuebtn_Copy1_Click(object sender, RoutedEventArgs e)
        {
            //update
            cts.Cancel();
            //(Application.Current.MainWindow as MainWindow)._mainFrame.Content = new DownloadImages(true);
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Install(true);

        }

        private async void backupbtn_Click(object sender, RoutedEventArgs e)
        {
            backupbtn.IsEnabled = false;
            await App.BootleggerApp.BackupDatabase();
            backupbtn.IsEnabled = true;
        }

        private async void restorebtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var diag = new Avalon.Windows.Dialogs.FolderBrowserDialog() { BrowseFiles = false };
                var folder = diag.ShowDialog();

                if (diag.SelectedPath != null)
                {
                    restorebtn.IsEnabled = false;
                    await App.BootleggerApp.RestoreDatabase(diag.SelectedPath);
                    restorebtn.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                App.BootleggerApp.Log.Error(ex);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (App.Current.MainWindow as MetroWindow).Close();
        }

        private void Video_MediaEnded(object sender, RoutedEventArgs e)
        {
            video.Position = TimeSpan.FromSeconds(0);
            video.Play();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenFolder();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenLog();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack)
                Browser.BackCommand.Execute(null);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoForward)
                Browser.ForwardCommand.Execute(null);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            //if (Browser.canre)
            Browser.ReloadCommand.Execute(null);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenAdminPanel();
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenAppLocation();
        }
    }
}
