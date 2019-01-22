@echo off
echo "Downloading Images for Cache"
docker pull redis:alpine
docker pull mvertes/alpine-mongo
docker pull kusmierz/beanstalkd
docker pull bootlegger/ourstory-worker:latest 
docker pull bootlegger/ourstory-server:latest 
docker pull bootlegger/nginx:latest
echo "Exporting Images"
docker save redis:alpine mvertes/alpine-mongo kusmierz/beanstalkd bootlegger/ourstory-worker:latest bootlegger/ourstory-server:latest bootlegger/nginx:latest -o images.tar