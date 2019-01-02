// This script contains all code necessary to access the api

  
/************** Helper functions for functional programmers ****/

// Execute the given 'delayed' function later with the given arguments.
// Usage: g = delay(f, 1, 2, 3); ... more code or a callback ... ; g() 
function delay(delayed, arg1, arg2, arg3){
    return function(){
        delayed(arg1, arg2, arg3);
    };
}

// Manipulate tables
function addCell(row, text){
	var cell = row.insertCell(row.length);
	cell.innerHTML = text;
	return cell;
}

function addCells(row, textList){
    for(var i in textList){
        addCell(row, textList[i]);
    }
}


// 
var cache = {}

function searchStation(inputSource, id){

    let query = inputSource.value;
    if(query.length < 3){
        return;
    }
    
    $.getJSON("/LocationsByName?name="+query,function(data){
        let e = document.getElementById(id);
        e.innerHTML = '<a href=' + data[0].id + '>' + data[0].name + '</a>'
        cache[id] = data[0].id;
    });
}


let src = document.getElementById("from");
src.addEventListener('input', delay(searchStation, src, "foundFrom"));
src = document.getElementById("to");
src.addEventListener('input', delay(searchStation, src, "foundTo"));

/**** Journey querying setup *****/


function lookupJourney(){
    
    let from = cache.foundFrom;
    let to = cache.foundTo;
    let transferTime = document.getElementById("transfertime").value;
    if(from === undefined || to === undefined){
        alert("Please specify departure and arrival stations first");
        return;
    }
    
    var depTime = document.getElementById('depDate').value + " "+document.getElementById('depTime').value+'+01:00';
    
    let query = "/journey?from="+encodeURIComponent(from)+
                    "&to="+encodeURIComponent(to)+
                    "&departure="+encodeURIComponent(depTime)+
                    "&internalTransferTime="+transferTime;
    $.getJSON(query, function(data){
        
        let t = document.getElementById("journeys");
        let rows = t.rows;
        for(var i = rows.length-1; i >= 1; i--){
            t.deleteRow(i);
        }
        
        for(var i in data){
            var j = data[i];
            console.log(j);
            var row = t.insertRow(1);
        	addCells(row, [j.departure.time, j.arrival.time, j.transfers]);
        	
        	var overview = "<ol>";
        	overview += "<li>Start your journey in <strong>"+j.departure.location.name+"</strong></li>";
        	for(var sn in j.segments){
        	    var s = j.segments[sn]
        	    overview += "<li>Get on the train to <strong>"+s.headsign+"</strong> in "+s.departure.location.name+" at "+s.departure.time+"</li> at <a href='Meme.jpg'>platform</a>";
        	    overview += "<li>Get off in <strong>"+s.arrival.location.name+"</strong>";
        	}
        	overview += "</ol>"
        	addCell(row, overview)
        }
    }).fail(function(){alert("No routes were found. Keep in mind that only today works")});

}

