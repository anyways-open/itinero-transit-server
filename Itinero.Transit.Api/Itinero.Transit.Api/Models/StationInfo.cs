using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Itinero.Transit.Api
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class StationInfo 
    {


        /// <summary>
        /// The Longitude (X) and Latitude (Y) of the station
        /// </summary>
        public readonly float LocationX, LocationY;

        [JsonProperty("@id")]
        public readonly Uri @Id;
        public readonly string Name, Standardname, id;

        public StationInfo(Location l) : 
            this(l.Lon, l.Lat, l.Uri, l.Name)
        {
            
        }
        
        public StationInfo(float locationX, float locationY, Uri ldId, string name)
        {
            LocationX = locationX;
            LocationY = locationY;
            @Id = ldId;
            id = "BE.NMBS." + ldId.Segments.Last();
            Name = name;
            Standardname = name;
        }
    }
}