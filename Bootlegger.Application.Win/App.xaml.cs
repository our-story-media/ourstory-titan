using Bootlegger.App.Lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Principal;

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

            //if (!IsUserAdministrator())
            //{
            //    MessageBox.Show("Please restart application as Administrator.");
            //    Environment.Exit(1);
            //}


            BootleggerApp = new BootleggerApplication();

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                BootleggerApp?.Log.Error(eventArgs.ExceptionObject);
            };
        }

        
        public static bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }
    }
}
