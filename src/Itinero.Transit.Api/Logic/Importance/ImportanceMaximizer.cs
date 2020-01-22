using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero.Transit.Api.Logic.Importance
{
    public class ImportanceMaximizer<T> : IComparer<Journey<T>> where T : IJourneyMetric<T>
    {
        private readonly Dictionary<StopId, uint> _importances;
        
        // Only used to generate error messages
        private readonly IStopsDb _stops;

        public ImportanceMaximizer(Dictionary<StopId, uint> importances, IStopsDb stops)
        {
            _importances = importances;
            _stops = stops;
        }

        private const int _xWins = -1;
        private const int _yWins = 1;

        public int Compare(Journey<T> x, Journey<T> y)
        {
            // we walk through both journeys until we reach a 'Transfer'
            // We compare, the one with the most important station wins
            // If they are equal, we continue to the next transfer

            if(x == null){throw new NullReferenceException(nameof(x));}
            if(y == null){throw new NullReferenceException(nameof(y));}
            
            do
            {
                while (x.PreviousLink != null && !x.Location.Equals(x.PreviousLink.Location))
                {
                    x = x.PreviousSpecial();
                }

                while (y.PreviousLink != null && !y.Location.Equals(y.PreviousLink.Location))
                {
                    y = y.PreviousSpecial();
                }

                if (x.PreviousLink == null && y.PreviousLink == null)
                {
                    // We've found the root, this is a tie
                    return _xWins;
                }

                if (x.PreviousLink == null || y.PreviousLink == null)
                {
                    // One of them reached the end while the other didn't...
                    // This is not a family, we can not compare them
                    
                    throw new ArgumentException(
                        $"Can not compare two journeys with a different number of transfers:\n" +
                        $"Journey X:\n{x.ToString(_stops)}\n\n" +
                        $"Journey Y:\n{y.ToString(_stops)}");
                }
                
                
                // Both x and y are at a transfer
                // Which one is better?

                _importances.TryGetValue(x.Location, out var xi);
                _importances.TryGetValue(x.Location, out var yi);

                if (xi > yi)
                {
                    return _xWins;
                }

                if (yi < xi)
                {
                    return _yWins;
                }

                if (xi == yi)
                {
                    x = x.PreviousLink;
                    y = y.PreviousLink;
                }


            } while (x.PreviousLink != null && y.PreviousLink != null);

            return _xWins;
        }
    }


    internal static class Extensions
    {
        [Pure]
        public static Journey<T> PreviousSpecial<T>(this Journey<T> j) where T : IJourneyMetric<T>
        {
            while (j.PreviousLink != null)
            {
                j = j.PreviousLink;
                if (j.SpecialConnection)
                {
                    return j;
                }
            }

            return j; // We've found the root
        }
    }
}