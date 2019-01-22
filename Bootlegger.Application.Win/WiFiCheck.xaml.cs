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


namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for Checklist.xaml
    /// </summary>
    public partial class WiFiCheck
    {
        public WiFiCheck()
        {
            InitializeComponent();

            //continuebtn.Click += Continuebtn_Click;

            //if (App.BootleggerApp.CurrentState == Lib.BootleggerApplication.RUNNING_STATE.NOT_SUPPORTED)
            //{
            //    tick_os.Visual = null;
            //    tick_docker.Visual = null;
            //    tick_running.Visual = null;
            //    tick_wifi.Visual = null;
            //}
            //else if (App.BootleggerApp.CurrentState == Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER_RUNNING || App.BootleggerApp.CurrentState == Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER)
            //{
            //    tick_docker.Visual = null;
            //    tick_running.Visual = null;
            //    tick_wifi.Visual = null;
            //}
            //else if (App.BootleggerApp.CurrentState == Lib.BootleggerApplication.RUNNING_STATE.NO_IMAGES)
            //{
            //    tick_running.Visual = null;
            //    tick_wifi.Visual = null;
            //}
            //else if (App.BootleggerApp.CurrentState == Lib.BootleggerApplication.RUNNING_STATE.NOWIFICONFIG)
            //{
            //    tick_wifi.Visual = null;
            //}
        }

        private async void Continuebtn_Click(object sender, RoutedEventArgs e)
        {
            //switch(App.BootleggerApp.CurrentState)
            //{
            //    case Lib.BootleggerApplication.RUNNING_STATE.NOT_SUPPORTED:
            //        await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Not Supported", "This OS is not supported, please try on another system");
            //        Environment.Exit(1);
            //        break;

            //    case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER:
            //    case Lib.BootleggerApplication.RUNNING_STATE.NO_DOCKER_RUNNING:
            //        (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Install();
            //        break;
            //    case Lib.BootleggerApplication.RUNNING_STATE.NO_IMAGES:
            //        (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new DownloadImages();
            //        break;
            //    case Lib.BootleggerApplication.RUNNING_STATE.NOWIFICONFIG:
            //        //trigger change in config:
            //        App.BootleggerApp.ConfigureNetwork("10.10.10.1", "255.255.255.0");

            //        //check wifi config:
            //        if (App.BootleggerApp.WiFiSettingsOk)
            //            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
            //        else
            //            if (await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Invalid Network", "Check Network settings and try again.",MessageDialogStyle.AffirmativeAndNegative,new MetroDialogSettings() {AffirmativeButtonText="Continue Anyway",NegativeButtonText="Manually Change Settings" }) == MessageDialogResult.Affirmative)
            //                (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
            //        break;
            //    case Lib.BootleggerApplication.RUNNING_STATE.READY:
            //        (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
            //        break;
            //}



        }

        private async void Continuebtn_Click_1(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.ConfigureNetwork("10.10.10.1", "255.255.255.0");

            //check wifi config:
            if (App.BootleggerApp.WiFiSettingsOk)
                (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
            else
                if (await(App.Current.MainWindow as MetroWindow).ShowMessageAsync("Invalid Network", "Check Network settings and try again.", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Continue Anyway", NegativeButtonText = "Manually Change Settings" }) == MessageDialogResult.Affirmative)
                    (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
        }
    }
}
