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
            var routerDb = State.GlobalState.RouterDb;

            var startPoint = routerDb.Snap(fromLon, fromLat);
            var endPoint = routerDb.Snap(toLon, toLat);

            if (startPoint.IsError || endPoint.IsError)
            {
                return null;
            }

            var config = new RoutingSettings()
            {
                Profile =  p,
                MaxDistance =  100000
            };
            
            var route = routerDb.Calculate(config, startPoint.Value, endPoint.Value);

            if (route.IsError)
            {
                return null;
            }

            return route.Value;
        }
    }
}