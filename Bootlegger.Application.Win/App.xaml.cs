using Bootlegger.App.Lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Principal;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Globalization;
using CefSharp;
using CefSharp.Wpf;
using System.Windows.Media;

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    
    public partial class App : Application
    {
        public static BootleggerApplication BootleggerApp { get; private set; }

        class OurStorySchemeHandlerFactory : ISchemeHandlerFactory
        {
            public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
            {
                if (request.Url == "ourstory://videos/")
                {
                    App.BootleggerApp.OpenFolder();
                    browser.StopLoad();
                    return null;
                }
                else
                {
                    return null;
                }
            }
        }

        public static void ChangeCulture(CultureInfo lang)
        {

            App.BootleggerApp.Log.Info($"Changing Locale to {lang}");

            Thread.CurrentThread.CurrentUICulture = lang;
            Thread.CurrentThread.CurrentCulture = lang;

            var oldWindow = Application.Current.MainWindow as MainWindow;
            oldWindow.ChangingCultures = true;

            Application.Current.MainWindow = new MainWindow();
            Application.Current.MainWindow.Show();

            oldWindow.Close();

            App.BootleggerApp.Log.Info($"Changed Locale to {lang}");

        }

        static App()
        {

            //FOR DEBUGGING:
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ar");

            //Monitor parent process exit and close subprocesses if parent process exits first
            //This will at some point in the future becomes the default

            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            Cef.EnableHighDPISupport();
            
            var settings = new CefSettings()
            {



            };

            settings.RegisterScheme(new CefCustomScheme()
            {
                IsStandard = true,
                SchemeName = "ourstory",
                SchemeHandlerFactory = new OurStorySchemeHandlerFactory()
            });
            //settings.CefCommandLineArgs.Add("disable-gpu", "1");
            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            BootleggerApp = new BootleggerApplication();
           
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                BootleggerApp?.Log.Error(eventArgs.ExceptionObject);
            };
        }

        
        //public static bool IsUserAdministrator()
        //{
        //    bool isAdmin;
        //    try
        //    {
        //        WindowsIdentity user = WindowsIdentity.GetCurrent();
        //        WindowsPrincipal principal = new WindowsPrincipal(user);
        //        isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        //    }
        //    catch (UnauthorizedAccessException ex)
        //    {
        //        isAdmin = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        isAdmin = false;
        //    }
        //    return isAdmin;
        //}

        //public static List<string> ListLocales()
        //{
        //    // Retrieve available languages
        //    List<string> languages = new List<string>();
        //    // Retrieve all files in Lang/ folder
        //    String[] files = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        //        foreach (String filePath in files)
        //        {
        //            // For each file

        //            // Create regex
        //            Regex regex = new Regex("Strings.[a-zA-Z-]*.resx");
        //            // Save file name that matches with regex
        //            if (regex.IsMatch(filePath))
        //            {
        //                // If the file is a language file

        //                String fileName =
        //                    Path.GetFileNameWithoutExtension(filePath);
        //                // Extract language from the file name
        //                String language = fileName.Replace("Strings.",
        //                    String.Empty);
        //                // Add language to the list
        //                languages.Add(language);
        //            }
        //        }
        //        return languages;
        //}
    }
}
