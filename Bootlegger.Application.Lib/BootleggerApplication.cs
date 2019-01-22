using Docker.DotNet;
using Docker.DotNet.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using System.Management;
using System.Linq;
using Docker.DotNet.X509;
using System.Security.Cryptography.X509Certificates;
using CertificatesToDBandBack;
using System.Security.Cryptography;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Commands;
using System.Security.AccessControl;

namespace Bootlegger.App.Lib
{
    public class BootleggerApplication : IProgress<JSONMessage>
    {
        public enum INSTALL_STATE { NOT_SUPPORTED, NEED_DOWNLOAD, NEED_IMAGES, INSTALLED }

        public enum RUNNING_STATE { READY, RUNNING, NOWIFICONFIG }

        public RUNNING_STATE CurrentState { get; private set; }
        public OperatingSystem CurrentPlatform { get; private set; }

        public BootleggerApplication()
        {
            CurrentPlatform = System.Environment.OSVersion;
        }

        Docker.DotNet.DockerClient dockerclient;

        //private SsdpDevicePublisher _Publisher;

        #region Installer
        
        public bool IsInstalled
        {
            get
            {
                return Plugin.Settings.CrossSettings.Current.GetValueOrDefault("ourstory_installed", false);
                //return Xamarin.Essentials.Preferences.Get("ourstory.installed", false);
            }

            set
            {
                Plugin.Settings.CrossSettings.Current.AddOrUpdateValue("ourstory_installed", value);
            }
        }

        public bool IsDockerInstalled
        {
            get
            {
                switch (CurrentInstallerType)
                {
                    case InstallerType.NO_HYPER_V:
                        return File.Exists(@"C:\Program Files\Docker Toolbox\docker.exe");
                    case InstallerType.HYPER_V:
                    default:
                        return File.Exists(@"C:\Program Files\Docker\Docker\Docker for Windows.exe");
                }
            }
        }

        public bool HasCachedContent
        {
            get
            {
                string installer = (CurrentInstallerType == InstallerType.HYPER_V) ? HYPER_V_INSTALLER_LOCAL : INSTALLER_LOCAL;
                return
                    File.Exists(Path.Combine("downloads", installer)) && File.Exists(Path.Combine("downloads", "images.tar"));
            }
        }

        public bool IsOnlineInstaller { get {
                return Plugin.Connectivity.CrossConnectivity.Current.IsConnected && Plugin.Connectivity.CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi);
            }
        }

        InstallerType CurrentInstallerType { get {
            return (!HyperVSwitch.SafeNativeMethods.IsProcessorFeaturePresent(HyperVSwitch.ProcessorFeature.PF_VIRT_FIRMWARE_ENABLED))?InstallerType.HYPER_V : InstallerType.NO_HYPER_V;
        } }

        //download the content:
        const string HYPER_V_INSTALLER_REMOTE = "https://download.docker.com/win/stable/Docker%20for%20Windows%20Installer.exe";
        const string HYPER_V_INSTALLER_LOCAL = "DockerForWindows.exe";
        const string INSTALLER_REMOTE = "https://github.com/docker/toolbox/releases/download/v18.09.1/DockerToolbox-18.09.1.exe";
        const string INSTALLER_LOCAL = "DockerToolbox.exe";

        enum InstallerType { HYPER_V, NO_HYPER_V };

        public async Task DownloadInstaller(CancellationToken cancel)
        {
            string src = "";
            string dst = "";
            switch (CurrentInstallerType)
            {
                case InstallerType.HYPER_V:
                    src = HYPER_V_INSTALLER_REMOTE;
                    dst = $"{HYPER_V_INSTALLER_LOCAL}";
                    break;

                case InstallerType.NO_HYPER_V:
                    src = INSTALLER_REMOTE;
                    dst = $"{INSTALLER_LOCAL}";
                    break;
            }

            if (!File.Exists(Path.Combine("downloads",dst)))
            {
                await DownloadFileInBackground(src,Path.Combine("downloads",dst + ".download"),cancel);
                File.Move(Path.Combine("downloads", dst + ".download"), Path.Combine("downloads", dst));
            }
        }

        public event Action<DownloadProgressChangedEventArgs> OnFileDownloadProgress;

        public async Task DownloadFileInBackground(string src,string dst, CancellationToken cancel)
        {
            WebClient client = new WebClient();
            cancel.Register(client.CancelAsync);
            Uri uri = new Uri(src);

            // Specify that the DownloadFileCallback method gets called
            // when the download completes.
            //client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback2);
            // Specify a progress notification handler.
            client.DownloadProgressChanged += (o,e) =>
            {
                OnFileDownloadProgress?.Invoke(e);
            };
            await client.DownloadFileTaskAsync(uri, dst);
        }

        static Task<int> RunProcessAsync(string fileName, string arg)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = { FileName = fileName, Arguments = arg },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }

        public async Task RunInstaller(CancellationToken cancel)
        {
            string args = "";
            string file = "";
            switch (CurrentInstallerType)
            {
                case InstallerType.HYPER_V:
                    file = Path.Combine(Environment.CurrentDirectory, "downloads", HYPER_V_INSTALLER_LOCAL);
                    args = "install --quiet";
                    break;

                case InstallerType.NO_HYPER_V:
                    file = Path.Combine(Environment.CurrentDirectory, "downloads", INSTALLER_LOCAL);
                    args = "/SP- /SILENT /SUPPRESSMSGBOXES /NORESTART";
                    break;
            }

            await RunProcessAsync(file,args);
        }

        #endregion

        async Task StartDockerClient()
        {
            await Task.Run(async () =>
            {
                //start docker connection
                switch (CurrentInstallerType)
                {
                    case InstallerType.HYPER_V:
                        dockerclient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                        break;

                    case InstallerType.NO_HYPER_V:

                        var info = await Process.Start(new ProcessStartInfo("docker-machine", "env --shell=cmd")
                        //var info = await Process.Start(new ProcessStartInfo("cmd.exe","/C @FOR / f \"tokens=*\" % i IN('docker-machine env') DO @% i")
                        {
                            //var info = await Process.Start(new ProcessStartInfo("whoami") {
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }).StandardOutput.ReadToEndAsync();

                        foreach (var line in info.Split('\n'))
                        {
                            if (line.Contains("="))
                            {
                                var parts = line.Split('=');
                                Environment.SetEnvironmentVariable(parts[0].Remove(0, 4), parts[1]);
                            }
                        }

                        var host = Environment.GetEnvironmentVariable("DOCKER_HOST");
                        var path = Environment.GetEnvironmentVariable("DOCKER_CERT_PATH");

                        byte[] certBuffer = Helpers.GetBytesFromPEM(File.ReadAllText(Path.Combine(path,"cert.pem")), PemStringType.Certificate);
                        byte[] keyBuffer = Helpers.GetBytesFromPEM(File.ReadAllText(Path.Combine(path,"key.pem")), PemStringType.RsaPrivateKey);

                        X509Certificate2 certificate = new X509Certificate2(certBuffer);

                        RSACryptoServiceProvider prov = Crypto.DecodeRsaPrivateKey(keyBuffer);
                        certificate.PrivateKey = prov;


                        ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

                        var credentials = new CertificateCredentials(certificate);

                        dockerclient = new DockerClientConfiguration(new Uri(host),credentials).CreateClient();
                        break;
                }
            });
        }

        internal async Task RestoreDatabase(string pathtofiles)
        {
            //danger!
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("bootlegger");
            var files = Directory.GetFiles(pathtofiles);

            foreach (var file in files)
            {
                StreamReader writer = new StreamReader($"{file}");

                var colname = $"{new FileInfo(file).Name.Remove(new FileInfo(file).Name.Length - 5)}";

                var table = database.GetCollection<BsonDocument>(colname);

                //delete all entries
                await table.DeleteManyAsync(new BsonDocument());

                List <WriteModel<BsonDocument>> operations = new List<WriteModel<BsonDocument>>();
                string line = "";
                while ((line = writer.ReadLine()) != null)
                {
                    operations.Add(new InsertOneModel<BsonDocument>(BsonDocument.Parse(line)));
                }

                writer.Close();

                var result = await table.BulkWriteAsync(operations);
            }
        }
        

        public void UnConfigureNetwork()
        {
            ManagementClass objMC =
              new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    var gateways = (objMO["DefaultIPGateway"] as string[]);
                    var gateway = gateways?[0];

                    if (gateway?.StartsWith("10.10.10") ?? false)
                    {
                        try
                        {
                            //ManagementBaseObject setIP;
                            //ManagementBaseObject newIP = objMO.GetMethodParameters("EnableDHCP");

                            //newIP["IPAddress"] = new string[] { ip_address };
                            //newIP["SubnetMask"] = new string[] { subnet_mask };


                            //ManagementBaseObject objNewGate = null;
                            //objNewGate = objMO.GetMethodParameters("SetGateways");
                            //Set DefaultGateway
                            //objNewGate["DefaultIPGateway"] = gateways;
                            //objNewGate["GatewayCostMetric"] = new int[] { 1 };

                            objMO.InvokeMethod("EnableDHCP", null);
                            //setIP = objMO.InvokeMethod("SetGateways", objNewGate, null);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        public void ConfigureNetwork(string ip_address, string subnet_mask)
        {
            ManagementClass objMC =
              new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    var gateways = (objMO["DefaultIPGateway"] as string[]);
                    var gateway = gateways?[0];

                    if (gateway?.StartsWith("10.10.10") ?? false)
                    {
                        try
                        {
                            ManagementBaseObject setIP;
                            ManagementBaseObject newIP = objMO.GetMethodParameters("EnableStatic");

                            newIP["IPAddress"] = new string[] { ip_address };
                            newIP["SubnetMask"] = new string[] { subnet_mask };


                            ManagementBaseObject objNewGate = null;
                            objNewGate = objMO.GetMethodParameters("SetGateways");
                            //Set DefaultGateway
                            objNewGate["DefaultIPGateway"] = gateways;
                            objNewGate["GatewayCostMetric"] = new int[] { 1 };


                            setIP = objMO.InvokeMethod("EnableStatic", newIP, null);
                            setIP = objMO.InvokeMethod("SetGateways", objNewGate, null);

                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        internal void OpenFolder()
        {
            switch (CurrentInstallerType)
            {
                case InstallerType.HYPER_V:
                    System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "upload");
                    break;

                case InstallerType.NO_HYPER_V:
                    System.Diagnostics.Process.Start(@"C:\ourstory_content");
                    break;

            }
        }
        
        public bool WiFiSettingsOk { get
            {
                ManagementClass objMC =
              new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if ((bool)objMO["IPEnabled"])
                    {
                        var ip = (objMO["IPAddress"] as string[]);

                        if (ip?[0]?.Equals("10.10.10.1") ?? false)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public void OpenAdminPanel()
        {
            System.Diagnostics.Process.Start("http://localhost");
        }

        public void OpenDocs()
        {
            System.Diagnostics.Process.Start("https://guide.ourstory.video");
        }


        public async Task BackupDatabase()
        {
            var dirname = DateTime.Now.ToFileTime();
            Directory.CreateDirectory($"backup\\{dirname}");
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("bootlegger");
            var tables = await database.ListCollectionNamesAsync();
            foreach (var col in tables.ToList())
            {
                StreamWriter writer = new StreamWriter($"backup\\{dirname}\\{col}.json");
                var collection = database.GetCollection<BsonDocument>(col);
                await collection.Find(new BsonDocument()).ForEachAsync(d =>
                    writer.WriteLine(d.ToJson())
                );
                //Console.WriteLine("DONE " + col);
                await writer.FlushAsync();
                writer.Close();

            }
        }

        List<ImagesCreateParameters> imagestodownload;
        private int CurrentDownload = 0;

        public string DockerComposeFile
        {
            get
            {
                return (CurrentInstallerType == InstallerType.HYPER_V) ? "docker-compose.windows.yml" : "docker-compose.toolbox.yml";
            }
        }

        public async Task DownloadImages(bool forceupdate, CancellationToken cancel)
        {
            if (dockerclient == null)
            {
                await StartDockerClient();
            }

            bool doonline = true;
            if (!forceupdate)
            {
                //check if the local version of the images exists:
                if (File.Exists(Path.Combine("downloads","images.tar")))
                {
                    doonline = false;
                    await dockerclient.Images.LoadImageAsync(new ImageLoadParameters(), File.OpenRead(Path.Combine("downloads","images.tar")),this,cancel);
                    OnNextDownload?.Invoke(1, 1, 1);
                }
            }

            if (doonline)
            {
                CurrentDownload = 1;

                imagestodownload = new List<ImagesCreateParameters>();

                //load from yaml:
                var Document = File.ReadAllText(DockerComposeFile);
                var input = new StringReader(Document);

                // Load the stream
                var yaml = new YamlStream();
                yaml.Load(input);
                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                foreach (var entry in mapping.Children)
                {
                    if ((entry.Key as YamlScalarNode).Value == "services")
                    {
                        foreach (var service in (entry.Value as YamlMappingNode).Children)
                        {
                            //Console.WriteLine(service.Key);
                            var key = new YamlScalarNode("image");
                            var image = ((service.Value as YamlMappingNode).Children[key] as YamlScalarNode).Value;
                            var img = image.Split(':');
                            imagestodownload.Add(new ImagesCreateParameters()
                            {
                                FromImage = img[0],
                                Tag = (img.Length > 1) ? img[1] : "latest"
                            });
                            //Console.WriteLine(image);
                        }
                        //output.WriteLine(((YamlScalarNode)entry.Key).Value);
                    }
                }

                List<Task> tasks = new List<Task>();

                foreach (var im in imagestodownload)
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        Layers.Clear();

                        //detect if it exists:
                        try
                        {
                            if (forceupdate)
                                throw new Exception("Must update");
                            var exists = await dockerclient.Images.InspectImageAsync(im.FromImage, cancel);
                            //CurrentDownload++;
                            //OnNextDownload(CurrentDownload, imagestodownload.Count, CurrentDownload / (double)imagestodownload.Count);
                        }
                        catch (Exception e)
                        {
                            await dockerclient.Images.CreateImageAsync(im, null, this, cancel);
                        }
                        finally
                        {
                            CurrentDownload++;
                            OnNextDownload?.Invoke(CurrentDownload, imagestodownload.Count, CurrentDownload / (double)imagestodownload.Count);
                        }
                    }
                    else
                        break;
                }
            }
        }

        public event Action<string> OnLog;

        Process currentProcess;

        //start containers...
        public async Task<bool> RunServer(CancellationToken cancel)
        {
            try
            {
                //Start Docker
                if (!DockerStarted)
                    await StartDocker(cancel);

                //TODO: Start Docker Client
                await StartDockerClient();


                if (currentProcess != null && !currentProcess.HasExited)
                {
                    currentProcess.Close();
                }
                //if not running:
                currentProcess = new Process();
                currentProcess.StartInfo = new ProcessStartInfo("docker-compose");
                currentProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                currentProcess.StartInfo.Arguments = $"-f {DockerComposeFile} -p bootleggerlocal up -d";
                //currentProcess.StartInfo.Environment.Add("MYIP", GetLocalIPAddress());
                currentProcess.StartInfo.UseShellExecute = false;
                currentProcess.StartInfo.CreateNoWindow = true;
                currentProcess.StartInfo.RedirectStandardOutput = true;
                currentProcess.StartInfo.RedirectStandardError = true;

                currentProcess.Start();
                currentProcess.BeginOutputReadLine();
                currentProcess.BeginErrorReadLine();

                currentProcess.OutputDataReceived += (s, o) =>
                {
                    if (o.Data != null)
                        OnLog?.Invoke(o.Data.Trim());
                };

                currentProcess.ErrorDataReceived += (s, o) =>
                {
                    if (o.Data != null)
                        OnLog?.Invoke(o.Data.Trim());
                };

                await Task.Run(() =>
                {
                    currentProcess.WaitForExit();
                });

                WebClient client = new WebClient();

                bool connected = false;
                int count = 0;
                while (!connected && count < 12)
                {
                    try
                    {
                        var result = await client.DownloadStringTaskAsync($"http://localhost/status");
                        connected = true;
                    }
                    catch
                    {
                        await Task.Delay(5000);
                    }
                    finally
                    {
                        count++;
                    }
                }


                return connected;
            }
            catch
            {
                return false;
            }
        }

        public bool DockerStarted { get; set; }

        public static void AddDirectorySecurity(string FileName, string Account, FileSystemRights Rights, AccessControlType ControlType)
        {
            // Create a new DirectoryInfo object.
            DirectoryInfo dInfo = new DirectoryInfo(FileName);

            // Get a DirectorySecurity object that represents the 
            // current security settings.
            DirectorySecurity dSecurity = dInfo.GetAccessControl();

            // Add the FileSystemAccessRule to the security settings. 
            dSecurity.AddAccessRule(new FileSystemAccessRule(Account,
                                                            Rights,
                                                            ControlType));

            // Set the new access settings.
            dInfo.SetAccessControl(dSecurity);

        }

        public async Task StartDocker(CancellationToken cancel)
        {
            switch (CurrentInstallerType)
            {
                case InstallerType.HYPER_V:
                    Process.Start(@"C:\Program Files\Docker\Docker\Docker for Windows.exe");
                    break;

                case InstallerType.NO_HYPER_V:

                    try
                    {
                        if (!Directory.Exists(@"C:\ourstory_content"))
                            Directory.CreateDirectory(@"C:\ourstory_content");
                        //Directory.SetAccessControl(@"C:\Users\ourstory_content",
                        AddDirectorySecurity(@"C:\ourstory_content", "Everyone", FileSystemRights.FullControl, AccessControlType.Allow);
                    }
                    catch (Exception e)
                    {

                    }

                    //SET DOCKER BINARY ENV FOR WHEN INSTALLER HAS ONLY JUST RUN...
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_TOOLBOX_INSTALL_PATH")))
                    {
                        //Environment.SetEnvironmentVariable("DOCKER_TOOLBOX_INSTALL", @"C:\Program Files\Docker Toolbox");
                        const string name = "PATH";
                        string pathvar = System.Environment.GetEnvironmentVariable(name);
                        var value = pathvar + @";C:\Program Files\Docker Toolbox;C:\Program Files\Oracle\VirtualBox\";
                        Environment.SetEnvironmentVariable(name, value);
                        Environment.SetEnvironmentVariable("DOCKER_TOOLBOX_INSTALL_PATH", @"C:\Program Files\Docker Toolbox");
                        Environment.SetEnvironmentVariable("VBOX_MSI_INSTALL_PATH", @"C:\Program Files\Oracle\VirtualBox\");
                    }

                    var startinfo = new ProcessStartInfo(@"C:\Program Files\Git\bin\bash.exe")
                    {
                        Arguments = "-c \" \\\"/c/Program Files/Docker Toolbox/start.sh\\\" \\\"%*\\\"\"",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    //wait for docker to start...
                    var process = Process.Start(startinfo);
                    await Task.Factory.StartNew(() =>
                    {
                        process.WaitForExit();
                    });

                    //set port forwarding...
                    Process.Start(new ProcessStartInfo("VBoxManage.exe", "controlvm default natpf1 \"rule1, tcp,, 80,, 80\"")
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }).WaitForExit();
                    Process.Start(new ProcessStartInfo("VBoxManage.exe", "controlvm default natpf1 \"rule2, tcp,, 27107,, 27017\"")
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }).WaitForExit();
                    Process.Start(new ProcessStartInfo("VBoxManage.exe", "sharedfolder add default --name \"c/ourstory_content\" --hostpath \"C:\\ourstory_content\\\" --transient --automount")
                    {
                        //RedirectStandardOutput = true,
                        //RedirectStandardError = true,
                        //UseShellExecute = false
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }).WaitForExit();

                    DockerStarted = true;
                    break;
            }

            //wait until docker actually started...
            var task = Task.Factory.StartNew(async () =>
            {
                bool found = false;
                while (!found)
                {
                    try
                    {

                        await StartDockerClient();
                        var info = await dockerclient.System.GetSystemInfoAsync();
                        found = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Thread.Sleep(5000);
                }
            });

            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromMinutes(4), cancel)) == task)
            {
                await task;
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public async Task<bool> LiveServerCheck()
        {
            WebClient client = new WebClient();

            bool connected = false;
            int count = 0;
            while (!connected && count < 3)
            {
                try
                {
                    var result = await client.DownloadStringTaskAsync($"http://10.10.10.1/status");
                    connected = true;
                }
                catch
                {
                    await Task.Delay(10000);
                }
                finally
                {
                    count++;
                }
            }

            return connected;
        }

        //stop containers
        public async Task StopServer()
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                currentProcess.Close();
            }
            
            try
            {
                currentProcess = new Process();
                currentProcess.StartInfo = new ProcessStartInfo("docker-compose");
                currentProcess.StartInfo.Arguments = "-p bootleggerlocal stop";
                currentProcess.StartInfo.UseShellExecute = false;
                currentProcess.StartInfo.CreateNoWindow = true;
                currentProcess.Start();
                await Task.Run(() =>
                {
                    currentProcess.WaitForExit();
                });
            }
            catch
            {
                //cant stop?
            }

            //stop docker??

        }

        //message, current, total, sub, overall
        public event Action<string, int, int, Dictionary<string, double>, double> OnDownloadProgress;
        public event Action<int, int, double> OnNextDownload;

        Dictionary<string, double> Layers = new Dictionary<string, double>();

        public void Report(JSONMessage value)
        {
            //Debug.WriteLine(value.From);
            
            //Debug.WriteLine(value.Status);
            //Debug.WriteLine(value.ProgressMessage);
            if (value.ProgressMessage != null)
            {
                Debug.WriteLine(value.Progress.Current);
                //Debug.WriteLine(value.ProgressMessage);
                Layers[value.ID] = value.Progress.Current / (double)value.Progress.Total;
                //Console.WriteLine(CurrentDownload);
                OnDownloadProgress?.Invoke("Downloading", CurrentDownload, imagestodownload.Count, Layers, CurrentDownload / (double)imagestodownload.Count);
            }
        }
    }
}