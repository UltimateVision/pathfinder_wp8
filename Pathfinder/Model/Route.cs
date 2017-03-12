using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace Pathfinder.Model
{
    public class Route
    {
        public String Name;
        public String Description;
        public List<BasicGeoposition> Coordinates = new List<BasicGeoposition>();
    }
}
