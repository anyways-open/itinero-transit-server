using System;
using System.Collections.Generic;
// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Global

namespace Itinero.Transit.Api.Models
{
    public class Journeys
    {
        public readonly DateTime ResultGeneratedTime;
        public readonly List<Journey> journeys;

        public Journeys(List<Journey> journeys)
        {
            ResultGeneratedTime = DateTime.Now;
            this.journeys = journeys;
        }
    }
}