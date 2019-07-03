 # itinero-transit-server

[![Build status](https://build.anyways.eu/app/rest/builds/buildType:(id:anyways_routing_ItineroTransitServer)/statusIcon)](https://build.anyways.eu/viewType.html?buildTypeId=anyways_routing_ItineroTransitServer)  

The Itinero-Transit-Server is a wrapper around the core library [itinero-transit](https://github.com/openplannerteam/itinero-transit). The core library offers routing over a public transport network, this small wrapper makes that available via HTTPS.

A demo is deployed [here](https://routing.anyways.eu/transit/index.html).
The full documentation (as swagger file) can be found [there as well](http://routing.anyways.eu/transitapi/swagger/index.html).

If the server is offline, a backup swagger file is available [in the repo too](docs/swagger.json). To load this interactively:

- Open [https://petstore.swagger.io/](https://petstore.swagger.io/).
- Paste [this url of the raw spec](https://raw.githubusercontent.com/anyways-open/itinero-transit-server/master/docs/swagger.json) in the search box at the top.

## Getting started

This project assumes that you have a working dotnet environment on your Linux device. If you don't have dotnet, follow [this guide](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)

        # Clone the project
        git clone git@github.com:anyways-open/itinero-transit-server.git
        cd itinero-transit-server
        # Start the server
        cd src/Itinero.Transit.Api/
        dotnet run

The program will now load a bunch of data for the Belgian Public Transport agencies.

Have a look at the [status page](http://localhost:5000/status) how your server is doing. The loaded time windows indicate what dataset has been loaded already. In the loaded parts, planning is possible. There is no need to wait until every PT-operator has been completely loaded.

With the server running, visit the [API-documentation page](http://127.0.0.1:5000/swagger/index.html) to see what is possible.

## Project overview

First, a small overview of the directories in the git repo:

- `src` contains all the source code
- `test` contains all the test code
- `docs` contains an example swagger file
- `deployment` contains a script to deploy this as a docker container on a server

### Source overview

The project itself is a pretty straightforward C#/ASP.net project. Requests come in via HTTP and are delegated to a controller-class in `Controllers/`. They return an appropriate `Model` as JSON.

The controllers themselfs do not contain the algoirthms themselves. These are contained in `Logic/`. One important class in `Logic/` is `State.cs`. The `State` contains the _global variables_, namely a pointer to the transitdbs which the logic and controllers use.

As State is a central piece in the app, go read it now. Every field is documented.

The next important class is `Startup` where the State is constructed.

Overview of other `Logic`: classes:

- `ChangeableMaxNumberOfTransferFilter` filters journeys based on a maximum number of total transfers - but this setting can be changed halfway calculations if needed. This is one of the few classes which has a changeable state (most classes only have readonly fields)
- ImportanceCounter counts for each stop how many connections there are to estimate their weight in search
- JourneyBuilder creates journeys based on the passed search criteria
- JourneyTranslator creates a Model.Journey based on a journey
- NameIndex contains a trie for fuzzy search
- NameIndexBuilder builds such a trie
- State contains the global state
- TransitDbFactory reads the config, creates transitDbs for them and starts loading them

