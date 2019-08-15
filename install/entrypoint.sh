#!/bin/bash
echo "Starting Our Story Titan"

mongod &
redis-server --dir /redis --appendonly yes &
beanstalkd &

sleep 2

cd /ourstory-worker && npm start &
cd /ourstory-server && npm start &

nginx -g "daemon off;"