@echo off
echo "Downloading Images for Cache"
docker pull redis:alpine
docker pull mvertes/alpine-mongo
docker pull kusmierz/beanstalkd
docker pull bootlegger/ourstory-worker:dev 
docker pull bootlegger/ourstory-server:dev 
docker pull bootlegger/nginx-local:latest
echo "Exporting Images"
docker save redis:alpine mvertes/alpine-mongo kusmierz/beanstalkd bootlegger/nginx-local:latest bootlegger/ourstory-worker:dev bootlegger/ourstory-server:dev -o images.tar