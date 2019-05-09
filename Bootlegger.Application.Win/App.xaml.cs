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

namespace Bootlegger.App.Win
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    
    public partial class App : Application
    {
        public static BootleggerApplication BootleggerApp { get; private set; }
        static App()
        {

            //FOR DEBUGGING:
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ar");


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
