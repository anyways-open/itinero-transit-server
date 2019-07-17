using Itinero.Profiles;

namespace Itinero.Transit.Api.Logic
{
    public class RoutePlanner
    {
        public Route PlanRoute(
            Profile p,
            float fromLat,
            float fromLon,
            float toLat,
            float toLon)
        {
            var _routerDb = State.GlobalState.RouterDb;

            var startPoint = _routerDb.Snap((float) fromLon, (float) fromLat);
            var endPoint = _routerDb.Snap((float) toLon, (float) toLat);

            if (startPoint.IsError || endPoint.IsError)
            {
                return null;
            }

            var route = _routerDb.Calculate(p, startPoint.Value, endPoint.Value);

            if (route.IsError)
            {
                return null;
            }

            return route.Value;
        }
    }
}