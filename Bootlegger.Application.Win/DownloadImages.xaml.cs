using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using MahApps.Metro.Controls.Dialogs;

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for DownloadImages.xaml
    /// </summary>
    public partial class DownloadImages
    {
        bool doneOnce = false;


        public DownloadImages()
        {
            InitializeComponent();

            cancel = new CancellationTokenSource();
            Loaded += DownloadImages_Loaded;
        }

        bool force;

        public DownloadImages(bool force)
        {
            this.force = force;
            InitializeComponent();

            cancel = new CancellationTokenSource();
            Loaded += DownloadImages_Loaded;
        }

        private void BootleggerApp_OnNextDownload(int arg1, int arg2, double arg3)
        {
            Dispatcher.Invoke(() =>
            {
                if (arg1 == arg2)
                {
                    //do next steps...
                    (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
                }
                else
                {
                    progresses.Clear();
                    layersstack.Children.Clear();
                    progress.Value = arg3;
                    progresslabel.Content = "Downloading " + arg1 + " of " + arg2;
                }
            });
        }

        CancellationTokenSource cancel;

        private async void DownloadImages_Loaded(object sender, RoutedEventArgs e)
        {
            if (!doneOnce)
            {
                doneOnce = true;
                App.BootleggerApp.OnDownloadProgress += BootleggerApp_OnDownloadProgress;
                App.BootleggerApp.OnNextDownload += BootleggerApp_OnNextDownload;

                progresslabel.Content = "Stopping Applicaton...";

                await App.BootleggerApp.StopServer();

                progresslabel.Content = "Initiating Download...";


                try
                {
                    await App.BootleggerApp.DownloadImages(force, cancel.Token);
                }
                catch (TaskCanceledException ex)
                {

                }
                catch (Exception ef)
                {
                    await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Error", ef.Message);
                }
            }
        }

        private Dictionary<string, ProgressBar> progresses = new Dictionary<string, ProgressBar>();

        int lastimage = -1;

        private void BootleggerApp_OnDownloadProgress(string arg1, int arg2, int arg3, Dictionary<string,double> layers, double arg5)
        {
            Dispatcher.Invoke(() =>
            {
                progress.Value = arg5;
                //Debug.WriteLine(arg5);

                if (lastimage != arg2)
                {
                    progresses.Clear();
                    layersstack.Children.Clear();
                }

                foreach(var layer in layers)
                {
                    if (progresses.ContainsKey(layer.Key))
                        progresses[layer.Key].Value = layer.Value;
                    else
                    {
                        var prog = new MetroProgressBar() { Value = layer.Value, Maximum = 1 };
                        progresses.Add(layer.Key, prog);
                        layersstack.Children.Add(prog);
                    }
                }

                progresslabel.Content = arg1 + " " + arg2 + " of " + arg3;
            });
        }

        private void continuebtn_Copy_Click(object sender, RoutedEventArgs e)
        {
            cancel.Cancel();
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new WiFiCheck();
        }
    }
}
