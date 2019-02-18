#!/usr/bin/env bash

set -e

NAME="transit-api"
PORT=5000
IMAGE="anywaysopen/itinero-transit-server"
docker pull $IMAGE

LATEST=`docker inspect --format "{{.Id}}" $IMAGE`
RUNNING=`docker inspect --format "{{.Image}}" $NAME`
echo "Latest:" $LATEST
echo "Running:" $RUNNING

if [ "$RUNNING" != "$LATEST" ];then
    echo "restart $NAME"
    docker stop $NAME
    docker rm $NAME
    docker run -d --name $NAME -p $PORT:5000 $IMAGE
else
  echo "$NAME up to date."
fi
