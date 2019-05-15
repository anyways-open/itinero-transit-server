#!/usr/bin/env bash



set -e
echo "Use with '-staging' as argument to use the staging build"
NAME="transit-api$1"
PORT=5001
IMAGE="anywaysopen/itinero-transit-server$1"
docker pull $IMAGE
echo "Docker pull is done for $NAME"

if [ "$(docker ps -q -f name=$NAME$)" ] # The dollar at the end forces the "end of line" in the regex, causing not to match prefixed names
then
    echo "Does exist"
    # Docker container does exist
    LATEST=`docker inspect --format "{{.Id}}" $IMAGE`
    RUNNING=`docker inspect --format "{{.Image}}" $NAME`
    echo "Latest:" $LATEST
    echo "Running:" $RUNNING

    if [ "$RUNNING" == "$LATEST" ]
    then
        echo "Is up to date and running - nothing to do here"
        exit
    fi
    
    # Docker container is running, but not the latest version
    docker stop $NAME
fi
echo "Fallen through"
# At this point, docker might or might not be running
# But we have to update and start it anyway

if [ "$(docker ps -aq -f status=exited -f name=$NAME)" ]; then
        # cleanup residual container
        docker rm $NAME
fi

echo "Starting the docker image"

if [ $1 == "-staging" ]
then
    echo "Using staging deploy (port 5002)"
    PORT=5002
    docker run -d --rm --name $NAME -v /var/services/transit-api/logs:/var/app/logs -v /var/services/transit-api/db:/var/app/db -p $PORT:5000 $IMAGE
else
    docker run -d --rm --name $NAME -v /var/services/transit-api/logs:/var/app/logs -v /var/services/transit-api/db:/var/app/db -p $PORT:5000 $IMAGE
fi


git pull
