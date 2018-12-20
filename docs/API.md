
API description
==============

This API specification is using the [OpenAPI](https://en.wikipedia.org/wiki/OpenAPI_Specification) spec. To visualize this do the following:

- Open [https://petstore.swagger.io/](https://petstore.swagger.io/).
- Paste in [this url of the raw spec](https://raw.githubusercontent.com/anyways-open/itinero-transit-server/features/new-api/docs/swagger.json) in the search box at the top.

With this API, you can query how to go from one location to another using various transportation modi. As for version 1.0, only traveling via the SNCB/NMBS network is supported.

This document describes how you, as a developer, can query our service to get journeys via the SNCB-network.

Getting stations
----------------

First, you'll have to translate the departure and arrival location into a identiier.
Identifiers look like 'http://irail.be/stations/NMBS/008891009'.

There are two ways to obtain such an identifier:

- based on location
- based on name

Query '/searchStation?lat=50.1234&lon=4.1234' to obtain the identifier of the closest station.
Alternatively, use '/searchStation?name=Brugge' to search based on name.
Note that, in some cases, this might return multiple options.
E.g. `name=Gent` will yield `Gent-Sint-Pieters` and `Gent-Dampoort` as options.

The returned JSON will look somewhat like this: (WORK IN PROGRESS - SUBJECT TO CHANGE)


    {stations:
	[
	{name: 'Brugge'
	,lat: 50.1264
	,lon: 3.1234
	,id='http://irail.be/stations/NMBS/008891009'
	},
	... eventually more stations ...
	]
    }

Getting journeys
----------------

Once you know the ids of your departure and target stations, you can query possible journeys via the SNCB for them on

`host/route?from=...&to=...&departure=2018-12-12 12:30:00CET&arrival=2018-12-12 12:00:00Z&arrival=...

The arguments are:

- `from=` gives the departure location, thus the identifier of a SNCB-station in the 'http://irail.be/stations/NMBS/<some_number>'-format
- `to=` gives the arrival stations, in the same format
- `departure` gives the earliest wanted departure time, in ISO-8601-format. If no time zone indications is given, UTC is assumed
- `arrival` gives the latest wanted arrival time (also in ISO-8601-format).

Note that giving only departure or arrival-time is sufficient: if only departure time is given,
the server will respond with a number of options arriving after the given time.
If only departure time is given, the server will respond with a number of options arriving before the given time.

If _both_ are specified, all options departing after `departure` and arriving before `arrival` will be given.
This can result in a lot of journeys and might be a little slower. Departure and arrival time can be at most 24h apart.

The returned json could look like: (WORK IN PROGRESS - SUBJECT TO CHANGE)


    { queryDate: 2018-12-1- 12:12:34
    , ... some more metadata about the query, as we see fit ...
    , journeys: 
      [ # The collection of all the possible journeys
	    {
	    segments: # The journey consists of multiple segments
	        [     # A single segment describes a single part of the journey, e.g. a train, a transfer, a walk, ...
	            { # This segment represents a train from Brugge to Ghent
	              departure: 2018-12-12 12:00+01:00 # ISO 8601-times
	            , arrival: 2018-12-12 12:35+01:00
	            , departure-delay: 60 # The number of seconds that this train is delayed. Already included into departure time, no need to add it to arrival-time anymore
	            , arrival-delay: 0 # analog to departure-delay. If missing, use default value 0 
                , from-id: 'http://irail.be/nmbs/stations/00000123456'
                , from-nl: 'Brugge'
                , to-id: 'http://irail.be/nmbs/stations/0000000789'
                , to-nl: 'Gent-Sint-Pieters'
	            , vehicle: 'IC1234'
	            , headsign: 'Eupen' # The headsign of the train, often its end destination. Might be localized as well
	            , platform-departure: '4' # might be missing due to lack of data or when a platform number does not make sense (e.g. a segment where we take the bus) 
	            , platform-arrival: '5'
	            },
	            
	            # There is _no_ object explicitly giving the transfer
	            # Transfer station is the arrival station of the previous segment
	            # Tranfser time is the time between the previous arrival and the next departure
	            
	            # IN future versions, _walks_ can be given here
	            
	            {
	              departure: 2018-12-12 12:45+01:00
	              from-nl: 'Gent-Sint-Pieters'
	              to-nl: 'Kortrijk'
	              ...
	            }
	        ],
	        # Apart from the segments, some extra information is included
	        # This extra data is pure convenience for the clients, as all data can be recalculated from the segments
	        
	        # When the journey starts, equals 'segments[0].departure'    
	        departure: 2018-12-12 12:00:00+01:00
	        # When the journey ends, equals 'segments.last.departure'    
	        arrival: ...
	        
	        # where the journey starts and ends, resp. equals segments[0].from and segments.last.to
	        from: ...
	        to: ...
	        # Total traveltime in seconds, equals arrival-departure
	        traveltime: 3600
	        # Total number of transfers, equals segments.length-1
	        transfers: 1	
	    }
        ... More possible journeys here ...
      ]
    }


