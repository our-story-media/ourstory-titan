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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Initialized;
            Closing += MainWindow_Closing;
            MouseDown += Window_MouseDown;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        bool canexit = false;
        bool closing = false;

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing)
            {
                e.Cancel = true;
                return;
            }



            if (!canexit)
            {
                e.Cancel = true;
                var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync("Continue?", "Closing this application will prevent access to Our Story", MessageDialogStyle.AffirmativeAndNegative);
                if (tt == MessageDialogResult.Affirmative)
                {
                    closing = true;
                    _mainFrame.Visibility = Visibility.Collapsed;
                    progress.Visibility = Visibility.Visible;
                    App.BootleggerApp.UnConfigureNetwork();
                    await App.BootleggerApp.StopServer();
                    canexit = true;
                    closing = false;
                    Close();
                }
            }
        }

        private async void MainWindow_Initialized(object sender, EventArgs e)
        {
            progress.Visibility = Visibility.Hidden;

            //HACK FOR DEGUB
            //App.BootleggerApp.IsInstalled = false; 

            if (App.BootleggerApp.IsInstalled)
            {
                if (App.BootleggerApp.WiFiSettingsOk)
                {
                    _mainFrame.Content = new Running();
                }
                else
                {
                    _mainFrame.Content = new WiFiCheck();
                }
            }
            else
            {
                _mainFrame.Content = new Install();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //open help:
            App.BootleggerApp.OpenDocs();
        }
    }
}
