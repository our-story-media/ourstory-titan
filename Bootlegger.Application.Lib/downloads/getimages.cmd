@echo off
echo "Downloading Images for Cache"
docker pull redis:alpine
docker pull mvertes/alpine-mongo
docker pull kusmierz/beanstalkd
docker pull bootlegger/ourstory-worker:latest 
docker pull bootlegger/ourstory-server:latest 
docker pull bootlegger/nginx-local:latest
echo "Exporting Images"
docker save redis:alpine mvertes/alpine-mongo kusmierz/beanstalkd bootlegger/nginx-local:latest bootlegger/ourstory-worker:latest bootlegger/ourstory-server:latest -o images.tar
rem docker save bootlegger/ourstory-worker:latest -o images2.tar
rem docker save bootlegger/ourstory-server:latest -o images3.tar