using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Pathfinder.Model
{
    public class Point
    {
        public BasicGeoposition Position { get; set; }
        public String Title { get; set; }
        public String Description { get; set; }
    }
}
