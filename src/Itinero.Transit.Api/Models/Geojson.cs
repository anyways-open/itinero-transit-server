using System.Collections.Generic;
using System.Linq;
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    public class Geojson
    {
        public string Type { get; }
        public List<Feature> Features { get; }

        public Geojson(List<Feature> features, string type = "FeatureCollection")
        {
            Type = type;
            Features = features;
        }
    }

    public class Feature
    {
        public Geometry Geometry { get; }
        public Properties Properties { get; }
        public string Type { get; }

        public Feature(
            Geometry geometry,
            Properties properties,
            string type = "Feature"
        )
        {
            Geometry = geometry;
            Properties = properties;
            Type = type;
        }
    }

    public class Properties
    {
        public string Stroke { get; }

        public Properties(
            string color)
        {
            Stroke = color;
        }
    }

    public class Geometry
    {
        public string Type { get; }

        public List<List<float>> Coordinates { get; }

        public Geometry(
            IEnumerable<Coordinate> coordinates,
            string type = "LineString")
        {
            Type = type;
            Coordinates = coordinates.Select(
                coor => new List<float>
                {
                    (float) coor.Lon, (float) coor.Lat
                }).ToList();
        }
    }
}