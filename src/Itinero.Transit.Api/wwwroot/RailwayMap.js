// This document is responsible for drawing the map

function dateHHMM(date){
    return new Date(date).toTimeString().split(' ')[0].substring(0,5);
}

function createJourneyLayer(journey){
    let journeyLayer = L.featureGroup();
  
  
    let depLoc = journey.departure.location;
	let depPin = L.marker([parseFloat(depLoc.lat), parseFloat(depLoc.lon)]);
	depPin.bindPopup("Departure in "+depLoc.name+"<br />Take the train towards <strong>"+journey.segments[0].headsign+"</strong> at <strong>"+dateHHMM(journey.departure.time)+"</strong");
	depPin.addTo(journeyLayer);
	
	
	
	let pointCurrent = new L.LatLng(parseFloat(depLoc.lat), parseFloat(depLoc.lon));
    let pointList = [pointCurrent]
	
	
	let segs = journey.segments;
	for(var i = 1; i < segs.length; i++){
	    let transLoc = segs[i].departure.location;
	    let transPin = L.marker([parseFloat(transLoc.lat), parseFloat(transLoc.lon)]);
	    transPin.bindPopup("Transfer in <strong>"+transLoc.name+"</strong><br />Take the train to <strong>"+segs[i].headsign+"</strong> at <strong>"+dateHHMM(segs[i].departure.time)+"</strong");
	    transPin.addTo(journeyLayer);
	    
	    pointCurrent = new L.LatLng(parseFloat(transLoc.lat), parseFloat(transLoc.lon));
	    pointList.push(pointCurrent)
	}


	let arrLoc = journey.arrival.location;
	let arrPin = L.marker([parseFloat(arrLoc.lat), parseFloat(arrLoc.lon)]);
	arrPin.bindPopup("Arrive in <strong>"+arrLoc.name+"</strong> at <strong>"+dateHHMM(journey.departure.time)+"</strong");
	arrPin.addTo(journeyLayer);
	
    pointCurrent = new L.LatLng(parseFloat(arrLoc.lat), parseFloat(arrLoc.lon));
    pointList.push(pointCurrent)
	
    var firstpolyline = new L.Polyline(pointList, {
        color: '#00a4ac',
        weight: 3,
        opacity: 0.5,
        smoothFactor: 1
    });
    firstpolyline.addTo(journeyLayer);
	
    return journeyLayer;        
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
        attribution : 'Map Data and background &copy <a href="osm.org">OpenStreetMap</a> | Tiles & style by NMBS/SNCB (Belgian Railway)'
        }
    );

	map = L.map('map', {
		center: [50.9, 4.4],
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

