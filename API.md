
API description
===============

The Itinero-transit-server tries to mimic the IRail API.

The First milestone is to answer station-to-station queries for SNCB.

For this, send your queries to the `route`-endpoint. Parameters are

- `to` and `from`, being the destination and departure station. Stations are encoded as URI (such as `http://irail.be/stations/NMBS/008891009`). Find the entire list of corret stations at [https://irail.be/stations/NMBS]
- `date` and `time`, encoding the moment that the traveller wants to depart (or arrive). `date` is encoded `DDMMYY`, `time` is encoded `HHMM`
- `timeSel` indicates if the traveller wants to depart or arrive at the given moment. With `timeSel=depart`, the traveller will depart at the given time (or later); with `timeSel=arrive`, the returned journeys will arrive no later then the specified moment.

There is _no_ format specifier. Format negotiation should be done by using the correct HTTP-headers.

An example query becomes:

        https://host/route?to=http://irail.be/stations/NMBS/008821006&from=http://irail.be/stations/NMBS/008891009&date=201118&time=1338&timeSel=depart
