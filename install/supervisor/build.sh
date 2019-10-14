#!/bin/bash
echo "Building Indaba Supervisor ARM7"
# ,linux/amd64,linux/arm64, linux/arm/v7
docker build --platform linux/arm/v7 -t bootlegger/indaba-supervisor .
# docker buildx build --platform=linux/arm/v7 -t bootlegger/indaba-supervisor --load .


docker create -ti --name indaba-supervisor bootlegger/indaba-supervisor bash
docker cp indaba-supervisor:/build/indaba-supervisor ./build/indaba-supervisor
docker rm -f indaba-supervisor