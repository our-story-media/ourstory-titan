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
using System.Globalization;
using System.Reflection;
using System.IO;

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
            Closed += MainWindow_Closed;

            List<CultureInfo> items = new List<CultureInfo>()
            {
                    CultureInfo.CreateSpecificCulture("en"),
                    CultureInfo.CreateSpecificCulture("fr"),
                    CultureInfo.CreateSpecificCulture("es"),
                    CultureInfo.CreateSpecificCulture("ar")
            };
            langs.ItemsSource = items;

            


            if (items.Contains(Thread.CurrentThread.CurrentUICulture))
                langs.SelectedItem = Thread.CurrentThread.CurrentUICulture;
            else
                langs.SelectedItem = items.First();

            FlowDirection = (Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(LangSwitch))
            {
                string lang = LangSwitch;

                Closed -= MainWindow_Closed;

                Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);

                var wnd = new MainWindow();
                Closed += MainWindow_Closed;
                wnd.Show();
                LangSwitch = null;
            }
            else
            {
                App.Current.Shutdown();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        bool canexit = false;
        bool closing = false;
        public string LangSwitch { get; private set; } = null;

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (LangSwitch != null)
                return;

            if (closing)
            {
                e.Cancel = true;
                return;
            }



            if (!canexit)
            {
                e.Cancel = true;

                var tt = await (App.Current.MainWindow as MetroWindow).ShowMessageAsync(locale.Strings.ContinueDialogTitle, locale.Strings.CloseWarning, MessageDialogStyle.AffirmativeAndNegative);
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

        private void MainWindow_Initialized(object sender, EventArgs e)
        {
            progress.Visibility = Visibility.Hidden;

            //HACK FOR DEGUB
            //App.BootleggerApp.IsInstalled = true;
            //_mainFrame.Content = new Running();
            //return;

            if (App.BootleggerApp.IsInstalled && App.BootleggerApp.IsDockerInstalled)
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
                _mainFrame.Content = new Install(false);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //open help:
            App.BootleggerApp.OpenDocs();
        }

        private void Langs_Selected(object sender, RoutedEventArgs e)
        {
            
        }

        private void Langs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName != (e.AddedItems[0] as CultureInfo).TwoLetterISOLanguageName)
            {
                LangSwitch = (e.AddedItems[0] as CultureInfo).Name;
                Close();
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Bootlegger.App.Win.credits.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                await(App.Current.MainWindow as MetroWindow).ShowMessageAsync(locale.Strings.CreditsTitle, result, MessageDialogStyle.Affirmative);
            }
        }
    }
}
