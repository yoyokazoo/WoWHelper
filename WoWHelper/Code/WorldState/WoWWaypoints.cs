using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WoWHelper.Code.WorldState
{
    public static class WoWWaypoints
    {
        public static readonly WoWWaypointDefinition LEVEL_11_DUROTAR_COAST_WAYPOINTS = new WoWWaypointDefinition { 
            Waypoints = new List<Vector2>
            { 
                //new Vector2(38.13f, 16.10f),
                new Vector2(37.52f, 22.80f),
                new Vector2(37.07f, 27.55f),
                new Vector2(36.30f, 31.59f),
                new Vector2(36.03f, 33.09f),
                new Vector2(36.19f, 35.34f),
                new Vector2(36.28f, 39.20f),
                new Vector2(36.57f, 43.56f),
                new Vector2(36.60f, 47.55f),
                new Vector2(36.65f, 51.08f),
                new Vector2(36.30f, 53.50f)
            },
            TraversalMethod = WoWWaypointDefinition.WaypointTraversalMethod.LINEAR
        };
    }
}
