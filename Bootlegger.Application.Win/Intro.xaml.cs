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

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for Intro.xaml
    /// </summary>
    public partial class Intro
    {
        public Intro()
        {
            InitializeComponent();

            continuebtn.Click += Continuebtn_Click;
        }

        private void Continuebtn_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)._mainFrame.Content = new Install();
        }
    }
}
