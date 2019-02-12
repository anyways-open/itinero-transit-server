namespace Itinero.Transit.Api
{
    public static class Documentation
    {
        public static readonly string ApiDocs =
            "Welcome to documentation of the Anyways BVBA Transit API.<br />\n" +
            "The API offers routing over various public transport networks, such as train, bus, metro or tram networks." +
            "The heavy lifting of the routing is done by the <a href='https://github.com/openplannerteam/itinero-transit'>Itinero-transit</a> library, which is built to use <a href='https://linkedconnections.org/'>Linked Connections</a>.<br />\n" +
            "<br />" +
            "<h1> Usage of the API</h1>" +
            "<p>To query for a route using public transport, the API can be freely queried via HTTP. To do this, one must first obtain the identifiers of the departure and arrival stations. In the linked data philosophy, an identifier is an URL referring to a station. To obtain a station, one can either use the <code>LocationsAround</code> or <code>LocationsByName</code> endpoints. Please see the swagger documentation for more details.<p>" +
            "<p>Once that the locations are known, a journey over the PT-network can be obtained via the <code>Journey</code>-endpoint. Again, see the swagger file for a detailed explanation." +
            "" +
            "<h1>Status of the live server</h1>" +
            "<p>The demo endpoint only loads a few operators over a certain timespan. To see which operators are loaded during what time, consult the <code>status</code> endpoint.</p>" +
            "" +
            "<h1>Deploying the server</h1>" +
            "" +
            "<p>To deploy your own Transit-server, use <code>appsettings.json</code> to specify the connection URLs of the operator which you want to load. An example can be found <a href='https://github.com/anyways-open/itinero-transit-server/blob/master/src/Itinero.Transit.Api/appsettings.json'>here</a></p>";
    }
}