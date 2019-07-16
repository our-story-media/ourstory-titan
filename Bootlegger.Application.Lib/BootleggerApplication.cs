using CertificatesToDBandBack;
using Docker.DotNet;
using Docker.DotNet.Models;
using Docker.DotNet.X509;
using MongoDB.Bson;
using MongoDB.Driver;
using NETWORKLIST;
using NLog;
using SimpleWifi;
using SimpleWifi.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Bootlegger.App.Lib
{
    public class BootleggerApplication : IProgress<JSONMessage>
    {

        public BootleggerApplication()
        {
            CurrentPlatform = System.Environment.OSVersion;
            Log = LogManager.GetLogger(typeof(BootleggerApplication).FullName);
            ImagesPath = Path.Combine("downloads", "images.tar");
            Directory.CreateDirectory(Path.Combine("downloads"));
            //ContainerStatus = new List<ContainerListResponse>();
        }

        public Logger Log { get; set; }

        public enum INSTALL_STATE { NOT_SUPPORTED, NEED_DOWNLOAD, NEED_IMAGES, INSTALLED }

        public enum RUNNING_STATE { READY, RUNNING, NOWIFICONFIG }

        public RUNNING_STATE CurrentState { get; private set; }
        public OperatingSystem CurrentPlatform { get; private set; }

        const string DOCKERFORWINDOWS = @"C:\Program Files\Docker\Docker\Docker for Windows.exe";


        Docker.DotNet.DockerClient dockerclient;

        #region Installer

        private string InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OurStoryTitan", "installed");

        public bool IsInstalled
        {
            get
            {
                return File.Exists(InstallPath);
                //return Plugin.Settings.CrossSettings.Current.GetValueOrDefault("ourstory_installed", false);
            }

            set
            {
                if (value)
                    File.WriteAllText(InstallPath, "");
                else
                    File.Delete(InstallPath);
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
                    File.Exists(Path.Combine("downloads", installer));
            }
        }

        public bool IsOnlineInstaller
        {
            get
            {
                return Plugin.Connectivity.CrossConnectivity.Current.IsConnected && Plugin.Connectivity.CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi);
            }
        }

        InstallerType CurrentInstallerType
        {
            get
            {
                //DETECT WHICH VERSION OF DOCKER YOU HAVE, RATHER THAN WHICH VERSION YOU *SHOULD* HAVE

                if (File.Exists(DOCKERFORWINDOWS))
                    return InstallerType.HYPER_V;
                else
                    return InstallerType.NO_HYPER_V;
            }
        }

        //Content locations:
        const string HYPER_V_INSTALLER_REMOTE = "https://download.docker.com/win/stable/Docker%20for%20Windows%20Installer.exe";
        const string HYPER_V_INSTALLER_LOCAL = "DockerForWindows.exe";
        const string INSTALLER_REMOTE = "https://github.com/docker/toolbox/releases/download/v18.09.1/DockerToolbox-18.09.1.exe";
        const string INSTALLER_LOCAL = "DockerToolbox.exe";
        const string TAR_FILE_LOCATION = "https://s3-eu-west-1.amazonaws.com/bootleggerlive/titan/images.tar";

        enum InstallerType { HYPER_V, NO_HYPER_V };

        internal async Task DownloadImagesTar(CancellationToken token)
        {
            string src = TAR_FILE_LOCATION;
            string dst = "images.tar";
            if (!File.Exists(Path.Combine("downloads", dst)))
            {
                Log.Info($"Initiating download of {src}");
                await DownloadFileInBackground(src, Path.Combine("downloads", dst + ".download"), token);
                Log.Info("Download of installer complete");
                File.Move(Path.Combine("downloads", dst + ".download"), Path.Combine("downloads", dst));
                Log.Info("Moving download file to correct name");
            }
            else
                Log.Info($"Download not required for {src}");
        }


        public async Task DownloadInstaller(CancellationToken cancel)
        {
            Log.Info("Starting download of installer");
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

            if (!File.Exists(Path.Combine("downloads", dst)))
            {
                Log.Info($"Initiating download of {src}");
                await DownloadFileInBackground(src, Path.Combine("downloads", dst + ".download"), cancel);
                Log.Info("Download of installer complete");
                File.Move(Path.Combine("downloads", dst + ".download"), Path.Combine("downloads", dst));
                Log.Info("Moving download file to correct name");
            }
            else
                Log.Info($"Download not required for {src}");

        }

        public event Action<DownloadProgressChangedEventArgs> OnFileDownloadProgress;

        public async Task DownloadFileInBackground(string src, string dst, CancellationToken cancel)
        {
            WebClient client = new WebClient();
            cancel.Register(client.CancelAsync);
            Uri uri = new Uri(src);

            // Specify that the DownloadFileCallback method gets called
            // when the download completes.
            //client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback2);
            // Specify a progress notification handler.
            client.DownloadProgressChanged += (o, e) =>
            {
                OnFileDownloadProgress?.Invoke(e);
            };
            await client.DownloadFileTaskAsync(uri, dst);
        }

        Task<int> RunProcessAsync(string fileName, string arg, bool show = false, bool runas = false)
        {
            Log.Info($"{fileName} {arg}");

            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = {
                    FileName = fileName,
                    Arguments = arg,
                    RedirectStandardError = (show)? false : true,
                    RedirectStandardOutput = (show)? false : true,
                    UseShellExecute = (show)? true: false,
                    CreateNoWindow = (show)? false: true,
                    WindowStyle = (show)? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    Verb = (runas)?"runas":"open"
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {


                if (process.ExitCode < 0)
                {
                    tcs.SetException(new Exception("Command failed"));
                    Log.Error(new Exception($"Command failed {fileName}"));
                }
                else
                {
                    tcs.SetResult(process.ExitCode);
                }
                process.Dispose();
            };

            process.OutputDataReceived += (sender, args) =>
             {
                 Log.Info($"{fileName}", args.Data);
             };

            process.ErrorDataReceived += (sender, args) =>
            {
                Log.Error($"{fileName}", args.Data);
            };

            process.Start();

            return tcs.Task;
        }

        Task<string> RunProcessAsyncResult(string fileName, string arg, bool show = false, bool runas = false)
        {
            Log.Info($"{fileName} {arg}");

            var tcs = new TaskCompletionSource<string>();

            var process = new Process
            {
                StartInfo = {
                    FileName = fileName,
                    Arguments = arg,
                    RedirectStandardError = (show)? false : true,
                    RedirectStandardOutput = (show)? false : true,
                    UseShellExecute = (show)? true: false,
                    CreateNoWindow = (show)? false: true,
                    WindowStyle = (show)? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    Verb = (runas)?"runas":"open"
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {

                tcs.SetResult(process.StandardOutput.ReadToEnd());
                process.Dispose();
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                Log.Error($"{fileName}", args.Data);
            };

            process.Start();

            return tcs.Task;
        }

        public async Task RunInstaller(CancellationToken cancel)
        {
            Log.Info("Installing docker");
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
                    args = "/SP- /SILENT /NOCANCEL /SUPPRESSMSGBOXES /NORESTART";
                    break;
            }

            await RunProcessAsync(file, args, true);
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
                        Log.Info("Starting HyperV Docker");

                        dockerclient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                        break;

                    case InstallerType.NO_HYPER_V:
                        Log.Info("Starting Toolbox Docker");

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

                        byte[] certBuffer = Helpers.GetBytesFromPEM(File.ReadAllText(Path.Combine(path, "cert.pem")), PemStringType.Certificate);
                        byte[] keyBuffer = Helpers.GetBytesFromPEM(File.ReadAllText(Path.Combine(path, "key.pem")), PemStringType.RsaPrivateKey);

                        X509Certificate2 certificate = new X509Certificate2(certBuffer);

                        RSACryptoServiceProvider prov = Crypto.DecodeRsaPrivateKey(keyBuffer);
                        certificate.PrivateKey = prov;


                        ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

                        var credentials = new CertificateCredentials(certificate);

                        dockerclient = new DockerClientConfiguration(new Uri(host), credentials).CreateClient();
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

                List<WriteModel<BsonDocument>> operations = new List<WriteModel<BsonDocument>>();
                string line = "";
                while ((line = writer.ReadLine()) != null)
                {
                    operations.Add(new InsertOneModel<BsonDocument>(BsonDocument.Parse(line)));
                }

                writer.Close();

                var result = await table.BulkWriteAsync(operations);
            }
        }


        public async Task UnConfigureNetwork()
        {
            if (IsInstalled)
            {
                try
                {
                    string networkName = "";
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in interfaces)
                    {
                        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && adapter.OperationalStatus == OperationalStatus.Up)
                        {
                            networkName = adapter.Name;
                            await RunProcessAsync("netsh", $"interface ip set address \"{networkName}\" dhcp", true, true);
                        }

                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public async void ConfigureNetwork(string ip_address, string subnet_mask)
        {
            try
            {
                string networkName = "";
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in interfaces)
                {
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && adapter.OperationalStatus == OperationalStatus.Up)
                    {
                        networkName = adapter.Name;
                        await RunProcessAsync("netsh", $"interface ip set address \"{networkName}\" static {IP} 255.255.255.0 10.10.10.254", true, true);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        internal void OpenFolder()
        {
            System.Diagnostics.Process.Start(@"C:\Users\Public\ourstorycontent");
            //switch (CurrentInstallerType)
            //{
            //    case InstallerType.HYPER_V:
            //        try
            //        {
            //            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "upload");
            //        }
            //        catch
            //        {

            //        }
            //        break;

            //    case InstallerType.NO_HYPER_V:
            //        System.Diagnostics.Process.Start(@"C:\Users\Public\ourstorycontent");
            //        break;

            //}
        }

        internal void OpenLog()
        {

            System.Diagnostics.Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\OurStoryTitan\log.txt");
        }

        public bool WiFiSettingsOk
        {
            get
            {
                ManagementClass objMC =
              new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection objMOC = objMC.GetInstances();

                foreach (ManagementObject objMO in objMOC)
                {
                    if ((bool)objMO["IPEnabled"])
                    {
                        var ip = (objMO["IPAddress"] as string[]);

                        if (ip?[0]?.Equals(IP) ?? false)
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

        public void OpenAppLocation()
        {
            System.Diagnostics.Process.Start(Path.Combine(Directory.GetCurrentDirectory(), "install"));
        }

        public void OpenDocs()
        {
            System.Diagnostics.Process.Start("https://guide.ourstory.dev");
        }


        public async Task BackupDatabase()
        {
            try
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
            catch (Exception)
            {

            }
        }

        List<ImagesCreateParameters> imagestodownload;
        private int CurrentDownload = 0;

        public string DockerComposeFile
        {
            get
            {
                return (CurrentInstallerType == InstallerType.HYPER_V) ? "docker-compose.yml" : "docker-compose.yml";
            }
        }

        public string ImagesPath { get; set; }

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
                if (File.Exists(ImagesPath))
                {
                    Log.Info("Using local images.tar file");
                    doonline = false;
                    await dockerclient.Images.LoadImageAsync(new ImageLoadParameters(), File.OpenRead(ImagesPath), this, cancel);
                    Log.Info("Local cached docker load complete");
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
                        }
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
                                throw new Exception($"Forcing image pull: {im.FromImage}");
                            var exists = await dockerclient.Images.InspectImageAsync(im.FromImage, cancel);
                        }
                        catch (Exception)
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

        public const int PORT = 8845;


        //start containers...
        public async Task<bool> RunServer(CancellationToken cancel)
        {
            try
            {
                //Start Docker
                if (!DockerStarted)
                    await StartDocker(cancel);

                //Start Docker Client
                await StartDockerClient();

                //check for drive share enabled
                await CheckSharedDrives();

                //create folder:
                //if (CurrentInstallerType == InstallerType.HYPER_V)
                //{
                var folder = @"C:\Users\Public\ourstorycontent";
                Log.Info($"Creating Folder {folder}");
                Directory.CreateDirectory(folder);
                //}

                if (CurrentInstallerType == InstallerType.HYPER_V)
                {
                    Log.Info($"Setting WiFi Policy for Firewall");
                    WlanClient wlan = new WlanClient();
                    var intf = wlan.Interfaces.First();
                    var profilename = intf.CurrentConnection.profileName;
                    bool matchespolicy = false;
                    var manager = new NetworkListManagerClass();
                    var connectedNetworks = manager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED).Cast<INetwork>();

                    foreach (var network in connectedNetworks)
                    {
                        if (profilename == network.GetName())
                        {
                            //network in profile list is same as network in wifi connection:
                            var cat = network.GetCategory();

                            if (cat != NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PRIVATE)
                            {
                                network.SetCategory(NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PRIVATE);
                                Log.Info($"Success setting WiFi Policy for Firewall to Private");

                            }
                        }
                    }
                }

                if (currentProcess != null && !currentProcess.HasExited)
                {
                    currentProcess.Close();
                }
                //if not running:
                currentProcess = new Process();
                currentProcess.StartInfo = new ProcessStartInfo("docker-compose");
                currentProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                currentProcess.StartInfo.Arguments = $"-f {DockerComposeFile} -p bootleggerlocal up -d";
                //currentProcess.StartInfo.Verb = "RUNAS";
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
                    {
                        Log.Info(o.Data.Trim());
                        OnLog?.Invoke(o.Data.Trim());
                    }
                };

                currentProcess.ErrorDataReceived += (s, o) =>
                {
                    if (o.Data != null)
                    {
                        Log.Info(o.Data.Trim());
                        OnLog?.Invoke(o.Data.Trim());
                    }
                };

                await Task.Run(() =>
                {
                    currentProcess.WaitForExit();
                });

                //start containter monitor:
                StartMonitor();

                WebClient client = new WebClient();

                bool connected = false;
                int count = 0;
                while (!connected && count < 12 && !cancel.IsCancellationRequested)
                {
                    try
                    {
                        Log.Info("Attempting connection to localhost...");
                        var result = await client.DownloadStringTaskAsync($"http://localhost:{PORT}/status");
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
            catch (FileLoadException fe)
            {
                throw fe;
            }
            catch (TaskCanceledException te)
            {
                throw te;
            }
            catch (Exception ex)
            {
                return false;
            }
        }



        void StartMonitor()
        {
            BackgroundWorker monitor = new BackgroundWorker();
            monitor.DoWork += Monitor_DoWork;
            monitor.RunWorkerAsync();
        }

        public event Action<List<NamedException>> OnRunWarning;

        List<NamedException> CurrentErrors = new List<NamedException>();

        public enum MONITOR_TYPE { Wifi, IP, Docker, Containers, Port }



        internal class WiFiPolicyException : NamedException
        {

        }

        internal class WiFiException : NamedException
        {

        }
        internal class ContainerException : NamedException
        {

        }
        internal class ServerException : NamedException
        {

        }

        internal class DriveException : NamedException
        {

        }
        internal class IpException : NamedException
        {

        }

        NamedException IPEXCEPTION = new IpException();
        NamedException CONTAINEREXCEPTION = new ContainerException();
        NamedException SERVEREXCEPTION = new ServerException();
        NamedException DRIVESPACEXCEPTION = new DriveException();
        NamedException WIFIEXCEPTION = new WiFiException();
        NamedException WIFIPOLICYEXCEPTION = new WiFiPolicyException();

        const string IP = "10.10.10.1";

        public class NamedException : Exception
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        private async void Monitor_DoWork(object sender, DoWorkEventArgs e)
        {
            //INIT:
            var Document = File.ReadAllText(DockerComposeFile);
            var input = new StringReader(Document);
            WebClient client = new TimeOutWebClient();


            var KnownContainerCount = 0;

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
                        KnownContainerCount++;
                    }
                }
            }

            var wifi = new Wifi();
            WlanClient wlan = new WlanClient();


            while (true)
            {
                try
                {
                    List<ContainerListResponse> containers = (await dockerclient.Containers.ListContainersAsync(new ContainersListParameters()
                    {
                        All = true,
                        Filters = new Dictionary<string, IDictionary<string, bool>>()
                        {
                            ["status"] = new Dictionary<string, bool>() { { "running", true } },
                            ["label"] = new Dictionary<string, bool>() { { "com.docker.compose.project=bootleggerlocal", true } }
                        }
                    })).ToList();

                    //TODO: Check containers against the known list:

                    if (containers.Count != KnownContainerCount)
                    {
                        if (!CurrentErrors.Contains(CONTAINEREXCEPTION))
                            CurrentErrors.Add(CONTAINEREXCEPTION);
                    }
                    else
                    {
                        CurrentErrors.Remove(CONTAINEREXCEPTION);
                    }

                    //CHECK WIFI CONNECTION:


                    //CHECK IP
                    //get ip information:
                    ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                    ManagementObjectCollection objMOC = objMC.GetInstances();
                    bool ipaddressok = false;
                    //string actualip = "none found";
                    foreach (ManagementObject objMO in objMOC)
                    {
                        if ((bool)objMO["IPEnabled"])
                        {
                            var ip = (objMO["IPAddress"] as string[]);

                            if (ip?[0]?.Equals(IP) ?? false)
                            {
                                ipaddressok = true;
                                //actualip = "10.10.10.1";
                            }
                        }
                    }

                    if (!ipaddressok)
                    {
                        if (!CurrentErrors.Contains(IPEXCEPTION))
                            CurrentErrors.Add(IPEXCEPTION);
                    }
                    else
                    {
                        CurrentErrors.Remove(IPEXCEPTION);
                    }



                    //CHECK DISK SPACE:

                    try
                    {
                        DriveInfo driveInfo = new DriveInfo(@"C:");
                        long FreeSpace = driveInfo.AvailableFreeSpace / 1024 / 1024;
                        if (FreeSpace < 1000)
                        {
                            if (!CurrentErrors.Contains(DRIVESPACEXCEPTION))
                                CurrentErrors.Add(DRIVESPACEXCEPTION);
                        }
                        else
                        {
                            CurrentErrors.Remove(DRIVESPACEXCEPTION);
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        //Console.WriteLine(errorMesage);
                    }

                    //WIFI:
                    if (wifi.ConnectionStatus != WifiStatus.Connected)
                    {
                        if (!CurrentErrors.Contains(WIFIEXCEPTION))
                            CurrentErrors.Add(WIFIEXCEPTION);
                    }
                    else
                    {
                        CurrentErrors.Remove(WIFIEXCEPTION);
                        //wifi is connected, so check for public/private
                       

                       
                        //check for network on right policy otherwise firewall rule wont work...
                        if (CurrentInstallerType == InstallerType.HYPER_V)
                        {
                            var manager = new NetworkListManagerClass();
                            var connectedNetworks = manager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED).Cast<INetwork>();
                            var intf = wlan.Interfaces.First();
                            var profilename = intf.CurrentConnection.profileName;
                            bool matchespolicy = false;

                            foreach (var network in connectedNetworks)
                            {
                                //Console.WriteLine(network.GetName() + " ");
                                if (profilename == network.GetName())
                                {
                                    //network in profile list is same as network in wifi connection:

                                    var cat = network.GetCategory();

                                    if (cat == NLM_NETWORK_CATEGORY.NLM_NETWORK_CATEGORY_PRIVATE)
                                        matchespolicy = true;
                                }
                            }

                            if (!matchespolicy)
                            {
                                if (!CurrentErrors.Contains(WIFIPOLICYEXCEPTION))
                                    CurrentErrors.Add(WIFIPOLICYEXCEPTION);
                            }
                            else
                            {
                                CurrentErrors.Remove(WIFIPOLICYEXCEPTION);
                            }
                        }
                    }


                    OnRunWarning?.Invoke(CurrentErrors);

                    //CHECK APPLICATION
                    bool connected = false;
                    try
                    {

                        var result = await client.DownloadStringTaskAsync($"http://{IP}:{PORT}/status");
                        connected = true;
                    }
                    catch
                    {

                    }

                    if (!connected)
                    {
                        if (!CurrentErrors.Contains(SERVEREXCEPTION))
                            CurrentErrors.Add(SERVEREXCEPTION);
                    }
                    else
                        CurrentErrors.Remove(SERVEREXCEPTION);

                }
                catch (Exception ex)
                {
                    CurrentErrors.Add(new NamedException() { Name = "Unknown", Description = ex.Message });
                }
                finally
                {
                    OnRunWarning?.Invoke(CurrentErrors);
                }

                Thread.Sleep(5000);

            }
        }
        private class TimeOutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = (int)TimeSpan.FromSeconds(2).TotalMilliseconds;
                return w;
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
            Log.Info("Starting docker");

            switch (CurrentInstallerType)
            {
                case InstallerType.HYPER_V:
                    Log.Info("Starting Docker for Windows");

                    var exitcode = await RunProcessAsync(DOCKERFORWINDOWS, "", false, false);
                    if (exitcode == 1)
                    {
                        Log.Error($"Cant start docker for Windows");
                        throw new DockerApiException(HttpStatusCode.BadGateway, "Cant start docker for Windows");
                    }


                    break;

                case InstallerType.NO_HYPER_V:
                    const string name = "PATH";
                    string pathvar = System.Environment.GetEnvironmentVariable(name);
                    var value = pathvar + @";C:\Program Files\Docker Toolbox;C:\Program Files\Oracle\VirtualBox\";
                    Environment.SetEnvironmentVariable(name, value);
                    Environment.SetEnvironmentVariable("DOCKER_TOOLBOX_INSTALL_PATH", @"C:\Program Files\Docker Toolbox");
                    Environment.SetEnvironmentVariable("VBOX_MSI_INSTALL_PATH", @"C:\Program Files\Oracle\VirtualBox\");

                    Log.Info($"Moving boot2docker if does not exist");
                    string userHomePath = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
                    string dockercache = Path.Combine(userHomePath, ".docker", "machine", "cache", "boot2docker.iso");
                    Directory.CreateDirectory(Path.Combine(userHomePath, ".docker", "machine", "cache"));
                    if (!File.Exists(dockercache))
                    {
                        File.Copy(@"C:\Program Files\Docker Toolbox\boot2docker.iso", dockercache, true);
                    }

                    Log.Info($"Stopping docker-machine");

                    await RunProcessAsync(@"docker-machine", "stop");

                    Log.Info($"Stopping Virtualbox VM");

                    await RunProcessAsync("VBoxManage.exe", "controlvm default savestate");

                    Log.Info($"Port forward {PORT}");
                    //port forward rules:
                    await RunProcessAsync("VBoxManage.exe", $"modifyvm \"default\" --natpf1 \"rule1, tcp,, {PORT},, {PORT}\"");

                    Log.Info($"Port forward 27017");

                    await RunProcessAsync("VBoxManage.exe", "modifyvm \"default\" --natpf1 \"rule2, tcp,, 27017,, 27017\"");

                    Log.Info("Stopping existing HTTP port 80 connections");
                    await RunProcessAsync("net", "stop HTTP /y", true, true);

                    Log.Info($"Starting docker-toolbox");
                    await RunProcessAsync(@"C:\Program Files\Git\bin\bash.exe", "-c \" \\\"/c/Program Files/Docker Toolbox/start.sh\\\" \\\"%*\\\"\"", true);

                    DockerStarted = true;
                    break;
            }

            var waitingtask = WaitForDockerToStart();

            if (await Task.WhenAny(waitingtask, Task.Delay(TimeSpan.FromMinutes(4), cancel)) == waitingtask)
            {
                await waitingtask;
            }
            else
            {
                Log.Error(new TimeoutException());
                throw new TimeoutException();
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        async Task WaitForDockerToStart()
        {
            //wait until docker actually started...

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
                    Log.Error(e);
                }
                await Task.Delay(5000);
            }

        }

        public async Task CheckSharedDrives()
        {
            if (CurrentInstallerType == InstallerType.HYPER_V)
            {
                Log.Info("Checking for shared drive");

                var result = await RunProcessAsyncResult(@"C:\Program Files\Docker\Docker\DockerCli.exe", "-SharedDrives");
                if (!result.Contains("C"))
                {
                    throw new FileLoadException();
                }
            }
        }

        public async Task<bool> LiveServerCheck(CancellationToken cancel)
        {
            WebClient client = new WebClient();

            bool connected = false;
            int count = 0;
            while (!connected && count < 3 && !cancel.IsCancellationRequested)
            {
                try
                {
                    var result = await client.DownloadStringTaskAsync($"http://{IP}:{PORT}/status");
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

            if (!connected)
                Log.Error($"Cannot connect to local server");


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
                await RunProcessAsync("docker-compose", $"-f {DockerComposeFile} -p bootleggerlocal stop");
            }
            catch (Exception e)
            {
                //cant stop?
                Log.Error(e);
            }

            //stop docker??

        }

        //message, current, total, sub, overall
        public event Action<string, int, int, Dictionary<string, double>, double> OnDownloadProgress;
        public event Action<int, int, double> OnNextDownload;

        Dictionary<string, double> Layers = new Dictionary<string, double>();

        public void Report(JSONMessage value)
        {
            if (value.ProgressMessage != null)
            {
                Debug.WriteLine(value.Progress.Current);
                Layers[value.ID] = value.Progress.Current / (double)value.Progress.Total;
                try
                {
                    OnDownloadProgress?.Invoke("Downloading", CurrentDownload, imagestodownload.Count, Layers, CurrentDownload / (double)imagestodownload.Count);
                }
                catch
                {
                    //unknown message
                }
            }
        }
    }



}
