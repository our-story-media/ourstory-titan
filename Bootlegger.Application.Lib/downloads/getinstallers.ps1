$ProgressPreference = 'SilentlyContinue'
"Downloading Docker for Windows...";
wget "https://download.docker.com/win/stable/Docker%20for%20Windows%20Installer.exe" -OutFile DockerForWindows.exe;

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
"Downloading Docker Toolbox...";
wget "https://github.com/docker/toolbox/releases/download/v18.09.1/DockerToolbox-18.09.1.exe" -OutFile DockerToolbox.exe;

"Downloading Dot Net 4.6.1 Offline Installer...";
wget "https://download.microsoft.com/download/E/4/1/E4173890-A24A-4936-9FC9-AF930FE3FA40/NDP461-KB3102436-x86-x64-AllOS-ENU.exe" -OutFile DockerToolbox.exe;


#Docker for windows:
#DockerForWindows.exe install --quiet

#Docker Toolbox:
#DockerToolbox.exe /SP- /VERYSILENT /SUPPRESSMSGBOXES