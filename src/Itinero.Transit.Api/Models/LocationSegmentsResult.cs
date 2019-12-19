// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A location and a collection of related segments.
    /// </summary>
    public class LocationSegmentsResult
    {
        /// <summary>
        /// The location.
        /// </summary>
        public Location Location { get; set; }
        
        /// <summary>
        /// The segments.
        /// </summary>
        public Segment[] Segments { get; set; }
    }
}