#!/bin/bash
echo "Building Compact Titan Image"
# ,linux/amd64,linux/arm64, linux/arm/v7
docker buildx build --add-host=redis:127.0.0.1,mongo:127.0.0.1,beanstalk:127.0.0.1,web:127.0.0.1 --platform=linux/arm/v7 -t bootlegger/titan-compact --push .

# DOCKER_BUILDKIT=1 docker build --squash --no-cache --platform linux/arm/v7 --build-arg TARGETPLATFORM=linux/arm/v7 -t bootlegger/titan-compact .