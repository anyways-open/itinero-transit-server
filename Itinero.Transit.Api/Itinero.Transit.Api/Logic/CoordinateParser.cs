using System.Globalization;

namespace Itinero.Transit.Api.Logic
{
    internal static class CoordinateParser
    {
        /// <summary>
        /// Tries to parse a location string into a lat/lon pair.
        /// </summary>
        /// <param name="location">The location string, potentially containing a lat/lon pair.</param>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>True if parsing succeeds.</returns>
        public static bool TryParse(string location, out (double longitude, double latitude) coordinates)
        {
            coordinates = default((double longitude, double latitude));
            if (string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            var split = location.Split(",");
            if (split == null ||
                split.Length != 2)
            {
                return false;
            }
            if (!double.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var longitude) ||
                !double.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var latitude))
            {
                return false;
            }

            coordinates = (longitude, latitude);
            return true;
        }
    }
}