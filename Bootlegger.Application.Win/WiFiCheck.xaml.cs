
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
using Bootlegger.App.Win.locale;

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
        }

        private async void Continuebtn_Click_1(object sender, RoutedEventArgs e)
        {
            App.BootleggerApp.ConfigureNetwork("10.10.10.1", "255.255.255.0");

            await Task.Delay(1000);

            //check wifi config:
            if (App.BootleggerApp.WiFiSettingsOk)
                (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
            else
                if (await(App.Current.MainWindow as MetroWindow).ShowMessageAsync(Strings.InvalidNetwork, Strings.CheckNetworkSettingsAndTryAgain, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = Strings.ContinueAnyway, NegativeButtonText = Strings.ManuallyChangeSettings }) == MessageDialogResult.Affirmative)
                    (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Running();
        }
    }
}
