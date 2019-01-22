$ProgressPreference = 'SilentlyContinue'
"Downloading Docker for Windows...";
wget "https://download.docker.com/win/stable/Docker%20for%20Windows%20Installer.exe" -OutFile DockerForWindows.exe;

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
"Downloading Docker Toolbox...";
wget "https://github.com/docker/toolbox/releases/download/v18.09.1/DockerToolbox-18.09.1.exe" -OutFile DockerToolbox.exe;

#Docker for windows:
#DockerForWindows.exe install --quiet

#Docker Toolbox:
#DockerToolbox.exe /SP- /VERYSILENT /SUPPRESSMSGBOXES