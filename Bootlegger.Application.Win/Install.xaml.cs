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
using Bootlegger.App.Lib;

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
            //progress.Maximum = 100;
        }

        
        CancellationTokenSource cancel = new CancellationTokenSource();

        private void InstallDocker_Loaded(object sender, RoutedEventArgs e)
        {
            cancel = new CancellationTokenSource();

            App.BootleggerApp.Log.Info("Install started");

            imagesbtn.Visibility = Visibility.Visible;
        }

        private void continuebtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            //back
            cancel.Cancel();
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Intro();
        }
        
        enum filetype { DOCKER, TAR};
        filetype CURRENTFILE;

        private async void continuebtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                locationmsg.Visibility = Visibility.Collapsed;
                buttons.Visibility = Visibility.Collapsed;
                imagesbtn.IsEnabled = false;
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
                    CURRENTFILE = filetype.DOCKER;
                    await App.BootleggerApp.DownloadInstaller(cancel.Token);

                    status.Text = "Installing Docker...";
                    await App.BootleggerApp.RunInstaller(cancel.Token);
                }

                status.Text = "Starting Docker...";

                progress.IsIndeterminate = true;

                await App.BootleggerApp.StartDocker(cancel.Token);

                status.Text = "Loading Our Story Components...";

                progress.IsIndeterminate = true;

                if (remote_download)
                {
                    CURRENTFILE = filetype.TAR;
                    await App.BootleggerApp.DownloadImagesTar(cancel.Token);
                }

                //load images into system:
                await App.BootleggerApp.DownloadImages(false, cancel.Token);

                App.BootleggerApp.IsInstalled = true;

                (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new WiFiCheck();
            }
            catch (Exception ex)
            {
                App.BootleggerApp.Log.Error(ex);
                var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Error", $"There was a problem installing Our Story. {ex.Message}. Please restart and try again.", MessageDialogStyle.Affirmative);
                Environment.Exit(1);
                //Console.WriteLine(ex.Message);
            }
        }

        private void BootleggerApp_OnNextDownload(int arg1, int arg2, double arg3)
        {
            progress.IsIndeterminate = false;
            status.Text = $"Downloading {arg1} of {arg2}...\nThis may take some time...";
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

            status.Text = $"Downloading {((CURRENTFILE==filetype.DOCKER)? "Docker Installer" : "Our Story Components")}\n{Math.Round((obj?.BytesReceived/(1024.0*1024.0)).Value,2)}MB of {Math.Round((double)(obj?.TotalBytesToReceive/(1024.0*1024.0)).Value,2)}MB...";
            progress.Value = obj.ProgressPercentage/100;
        }

        bool remote_download = true;

        private void Imagesbtn_Click(object sender, RoutedEventArgs e)
        {
            remote_download = false;
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.Filter = "Tar File|*.tar";
            var result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                App.BootleggerApp.ImagesPath = fileDialog.FileName;
                continuebtn_Click(null, null);
            }
        }
    }
}
