using Bootlegger.App.Lib;
using Bootlegger.App.Win.locale;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

            //Browser.LoadingStateChanged += Browser_LoadingStateChanged;
            //Browser.LoadError += Browser_LoadError;
            Browser.TitleChanged += Browser_TitleChanged;
        }

     
        //private void Browser_LoadError(object sender, CefSharp.LoadErrorEventArgs e)
        //{
        //    Dispatcher.Invoke(() =>
        //    {
        //        if (e.FailedUrl.Equals("ourstory://videos"))
        //        {
        //            App.BootleggerApp.OpenFolder();
                    
                    
        //        }
        //        Console.WriteLine(Browser.Address);
        //    });
        //}

        private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                pagetitle.Text = Browser.Title;
            });
        }

        private void Running_Unloaded(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            Browser.Address = "about:blank";
            App.BootleggerApp.StopMonitor();
        }

        CancellationTokenSource cts = new CancellationTokenSource();

        bool started = false;

        async void Start()
        {
            errorwrapper.Visibility = Visibility.Collapsed; 
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
                    started = true;

                    //ADDED WIFI CHECK TO RUNNING SCREEN:
                    if (!App.BootleggerApp.WiFiSettingsOk)
                    {
                        //(Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
                        //else
                        if (await (App.Current.MainWindow as MetroWindow).ShowMessageAsync(Strings.InvalidNetwork, Strings.CheckNetworkSettingsAndTryAgain, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = Strings.ContinueAnyway, NegativeButtonText = Strings.ManuallyChangeSettings }) == MessageDialogResult.Affirmative)
                        {
                            App.BootleggerApp.ConfigureNetwork("10.10.10.1", "255.255.255.0");
                        }
                    }
                        //(Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();

                }
                else
                {
                    started = true;
                    if (!cts.IsCancellationRequested)
                    {
                        progress.Content = Strings.ProblemStartingApplication;
                        sharewarning.Visibility = Visibility.Collapsed;
                        //progressring.Visibility = Visibility.Collapsed;
                        statusled.Background = FindResource("red") as Brush;
                        ledshadow.Color = Colors.Red;

                        progresswrapper.Visibility = Visibility.Collapsed;
                        //SHOW MASSIVE WARNING
                        var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync(locale.Strings.Error, string.Format(locale.Strings.ErrorDialog, ""), MessageDialogStyle.AffirmativeAndNegative);
                        if (tt == MessageDialogResult.Affirmative)
                        {
                            (App.Current.MainWindow as MetroWindow).Close();
                        }
                    }
                    else
                    {
                        //was cancelled:
                    }

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
                                err.HasAction = true;
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

                            case WiFiPolicyException c:
                                err.Name = locale.Strings.WiFiPolicyErrorName;
                                err.Description = locale.Strings.WiFiPolicyErrorDesc;
                                err.HasAction = true;
                                break;
                        }
                    }


                    errors.ItemsSource = null;
                    errors.ItemsSource = CurrentErrors;

                    if (CurrentErrors.Count > 0)
                    {
                        if (started)
                            errorwrapper.Visibility = Visibility.Visible;
                        statusled.Background = FindResource("red") as Brush;
                        ledshadow.Color = Colors.Red;
                    }
                    else
                    {
                        errorwrapper.Visibility = Visibility.Collapsed;
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
            try
            {
                await App.BootleggerApp.BackupDatabase();
                var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync(locale.Strings.Backup, locale.Strings.BackupComplete, MessageDialogStyle.Affirmative);
            }
            catch (Exception ex)
            {
                App.BootleggerApp.Log.Error(ex);
                var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync(locale.Strings.Backup, locale.Strings.BackupError, MessageDialogStyle.Affirmative);
            }
            backupbtn.IsEnabled = true;
        }

        private async void restorebtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var diag = new Avalon.Windows.Dialogs.FolderBrowserDialog() { BrowseFiles = false, SelectedPath = Directory.GetCurrentDirectory() };
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
                restorebtn.IsEnabled = true;
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

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            var err = (sender as Button).DataContext;
            switch (err)
            {
                case IpException c:
                    App.BootleggerApp.ConfigureNetwork("10.10.10.1", "255.255.255.0");
                    break;

                case WiFiPolicyException c:
                    App.BootleggerApp.SetWiFiPolicy();
                    break;
            }
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            //show errors:
            if (errors.Visibility == Visibility.Collapsed)
            {
                errors.Visibility = Visibility.Visible;
            }
            else
            {
                errors.Visibility = Visibility.Collapsed;
            }
        }

        private void Hidebtn_Click(object sender, RoutedEventArgs e)
        {
            if (errors.Visibility == Visibility.Collapsed)
            {
                errors.Visibility = Visibility.Visible;
            }
            else
            {
                errors.Visibility = Visibility.Collapsed;
            }
        }
    }
}
