using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Bootlegger.App.FixNetwork
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Resetting System network settings...");
            try
            {
                string networkName = "";
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in interfaces)
                {
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && adapter.OperationalStatus == OperationalStatus.Up)
                    {
                        networkName = adapter.Name;
                        var process = new Process
                        {
                            StartInfo = {
                                FileName = "netsh",
                                Arguments = $"interface ip set address \"{networkName}\" dhcp",
                                CreateNoWindow = false,
                                WindowStyle = ProcessWindowStyle.Normal,
                                Verb = "runas"
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    }

                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Environment.Exit(1);
            }
            Console.WriteLine("Complete!");
        }
    }
}
