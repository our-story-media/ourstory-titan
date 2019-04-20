using Bootlegger.App.Lib;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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
        }

        private void Running_Unloaded(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        CancellationTokenSource cts = new CancellationTokenSource();

        async void Start()
        {
            progress.Content = "Starting application...";

            try
            {
                if (await App.BootleggerApp.RunServer(cts.Token))
                {
                    progress.Content = "Running";
                    sharewarning.Visibility = Visibility.Collapsed;
                    progressring.Visibility = Visibility.Collapsed;
                }
                else
                {
                    progress.Content = "Problem starting application!";
                    sharewarning.Visibility = Visibility.Collapsed;
                    progressring.Visibility = Visibility.Collapsed;
                }
            }
            catch (FileLoadException)
            {
                //show video panel
                sharewarning.Visibility = Visibility.Visible;
                video.Play();

                // await check on if its enabled again:
                //  await App.BootleggerApp.CheckSharedDrives();

                await Task.Delay(10000);

                Start();

                //sharewarning.Visibility = Visibility.Collapsed;
            }


        }

        private async void Running_Loaded(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OnLog += BootleggerApp_OnLog;
            
            Start();
        }

        private void BootleggerApp_OnLog(string obj)
        {
            //Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    obj = Regex.Replace(obj, @"[^\u0020-\u007F]+", string.Empty);
            //    if (obj.Length > 0)
            //    {
            //        log.AppendText($"{DateTime.Now.ToShortTimeString()} - {obj.Trim('\n', '\r')}\r");
            //        log.ScrollToEnd();
            //    }
            //}));
        }

        private void continuebtn_Click(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenAdminPanel();
        }

        private void continuebtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.OpenFolder();
        }

        private void continuebtn_Copy1_Click(object sender, RoutedEventArgs e)
        {
            //update
            cts.Cancel();
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new DownloadImages(true);
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
    }
}
