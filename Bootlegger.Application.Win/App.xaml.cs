using Bootlegger.App.Lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static BootleggerApplication BootleggerApp { get; private set; }
        static App()
        {
            BootleggerApp = new BootleggerApplication();
        }
    }
}
