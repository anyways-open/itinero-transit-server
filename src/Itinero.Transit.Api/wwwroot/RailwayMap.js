// This document is responsible for drawing the map

function addJourney(map, journey){
        
}

/*

Call this function when setting up your map
*/
function initializeMap(){	


    var osmLayer = L.tileLayer("https://a.tile.openstreetmap.org/{z}/{x}/{y}.png",
        {
        attribution : 'Map Data and background &copy <a href="osm.org">OpenStreetMap</a>'
        }
    );

    var nmbsLayer = L.tileLayer("https://trainmap.azureedge.net/tiles/{z}/{x}/{y}.png",
        {
        attribution : 'Map Data and background &copy <a href="osm.org">OpenStreetMap</a> | Tiles provided by NMBS/SNCB (Belgian Railway)'
        }
    );

	map = L.map('map', {
		center: [50.9, 3.9],
		zoom:8,
		layers: [nmbsLayer],	
/*        center:[50.8441,4.3616],
        zoom:14,
        maxZoom:14,
        minZoom:8,
        maxBounds:[[52.637206,1.294401],[48.631913,7.748474]]
        */
		});

	return map;
}

