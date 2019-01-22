using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using MahApps.Metro.Controls.Dialogs;
using System.Threading;

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for InstallDocker.xaml
    /// </summary>
    public partial class Install
    {
        public Install()
        {
            InitializeComponent();
            Loaded += InstallDocker_Loaded;
        }

        
        CancellationTokenSource cancel = new CancellationTokenSource();

        private void InstallDocker_Loaded(object sender, RoutedEventArgs e)
        {
            cancel = new CancellationTokenSource();
            
            needswifi.Visibility = (!App.BootleggerApp.HasCachedContent) ? Visibility.Visible : Visibility.Hidden;
        }

        private void continuebtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            //back
            cancel.Cancel();
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Intro();
        }

        private async void continuebtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                continuebtn.IsEnabled = false;
                progress.Visibility = Visibility.Visible;
                status.Visibility = Visibility.Visible;
                progress.IsIndeterminate = true;

                App.BootleggerApp.OnFileDownloadProgress += BootleggerApp_OnFileDownloadProgress;
                App.BootleggerApp.OnDownloadProgress += BootleggerApp_OnDownloadProgress;
                App.BootleggerApp.OnNextDownload += BootleggerApp_OnNextDownload;

                if (!App.BootleggerApp.IsDockerInstalled)
                {
                    status.Text = "Downloading Docker Installer...";
                    await App.BootleggerApp.DownloadInstaller(cancel.Token);

                    status.Text = "Installing Docker...";
                    await App.BootleggerApp.RunInstaller(cancel.Token);
                }

                status.Text = "Starting Docker...";

                progress.IsIndeterminate = true;

                await App.BootleggerApp.StartDocker(cancel.Token);

                status.Text = "Getting Our Story Components...";

                progress.IsIndeterminate = true;

                await App.BootleggerApp.DownloadImages(false, cancel.Token);

                App.BootleggerApp.IsInstalled = true;

                (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new WiFiCheck();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void BootleggerApp_OnNextDownload(int arg1, int arg2, double arg3)
        {
            progress.IsIndeterminate = false;
            status.Text = $"Installing {arg1} of {arg2}...";
            progress.Value = arg3;
        }

        private void BootleggerApp_OnDownloadProgress(string arg1, int arg2, int arg3, Dictionary<string, double> arg4, double arg5)
        {
            progress.IsIndeterminate = false;
            progress.Value = arg5;
        }

        private void BootleggerApp_OnFileDownloadProgress(System.Net.DownloadProgressChangedEventArgs obj)
        {
            progress.IsIndeterminate = false;

            status.Text = $"Downloading Docker Installer {Math.Round((obj?.BytesReceived/(1024.0*1024.0)).Value,2)}MB of {Math.Round((double)(obj?.TotalBytesToReceive/(1024.0*1024.0)).Value,2)}MB...";
            progress.Value = obj.ProgressPercentage;
        }
    }
}
