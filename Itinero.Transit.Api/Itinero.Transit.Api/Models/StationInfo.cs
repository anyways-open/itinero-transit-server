using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Itinero.Transit.Data;
using Newtonsoft.Json;

namespace Itinero.Transit.Api
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class StationInfo 
    {


        /// <summary>
        /// The Longitude (X) and Latitude (Y) of the station
        /// </summary>
        public readonly double LocationX, LocationY;

        [JsonProperty("@id")]
        public readonly Uri @Id;
        public readonly string Name, Standardname, id;

        public StationInfo(IStop l) : 
            this(l.Longitude, l.Latitude, new Uri(l.GlobalId), string.Empty)
        {
            
        }
        
        public StationInfo(double locationX, double locationY, Uri ldId, string name)
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