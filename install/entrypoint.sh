#!/bin/bash
echo "Starting Our Story Titan"

echo "Recovering from Data Loss"

mongod --dbpath=/data/db --repair

APPS=("mongod" "redis-server --dir /redis --appendonly yes" "beanstalkd" "nginx -g \"daemon off;\"")

for ((i = 0; i < ${#APPS[@]}; i++))
do
  echo "Starting ${APPS[$i]}"
  eval "${APPS[$i]}" &
done

sleep 5

cd /ourstory-worker && npm start &
cd /ourstory-server && npm start &

while sleep 10; do
  #check node apps:
  # echo "Checking worker"
  ps aux |grep "node index.js" |grep -q -v grep
  STATUS=$?
  if [ $STATUS -ne 0 ] ; then
    echo "Worker Not Running!"
    exit 1
  fi

  # echo "Checking server"
  ps aux |grep "node app.js --prod" |grep -q -v grep
  STATUS=$?
  if [ $STATUS -ne 0 ] ; then
    echo "Server Not Running!"
    exit 1
  fi

  #check deps:
  for ((i = 0; i < ${#APPS[@]}; i++))
  do
    set -- ${APPS[$i]}
    APP=$1
    # echo "Checking $APP"
    ps aux |grep $APP |grep -q -v grep
    STATUS=$?
    if [ $STATUS -ne 0 ] ; then
      echo "$APP Not Running!"
      exit 1
    fi
  done
done