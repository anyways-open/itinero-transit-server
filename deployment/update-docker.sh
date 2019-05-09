#!/usr/bin/env bash



set -e

NAME="transit-api"
PORT=5001
IMAGE="anywaysopen/itinero-transit-server"
docker pull $IMAGE


LATEST=`docker inspect --format "{{.Id}}" $IMAGE`
RUNNING=`docker inspect --format "{{.Image}}" $NAME`
echo "Latest:" $LATEST
echo "Running:" $RUNNING


if [ "$RUNNING" == "Error: No such object: transit-api" ]; then
   docker rm $NAME
   docker run -d --name $NAME -v /var/services/transit-api/logs:/var/app/logs -v /var/services/transit-api/db:/var/app/db -p $PORT:5000 $IMAGE
elif [ "$RUNNING" != "$LATEST" ];then
    echo "restart $NAME"
    docker stop $NAME
    docker rm $NAME
    docker run -d --rm --name $NAME -v /var/services/transit-api/logs:/var/app/logs -v /var/services/transit-api/db:/var/app/db -p $PORT:5000 $IMAGE
else
  echo "$NAME up to date."
fi
git pull
