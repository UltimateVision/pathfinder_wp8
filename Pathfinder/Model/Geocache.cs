using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Pathfinder.Model
{
    public class Geocache
    {
        public String Name { get; set; }
        public String Id { get; set; }
        public String Username { get; set; }
        public String Type { get; set; }
        public double Terrain { get; set; }
        public double Difficulty { get; set; }
        public String Size { get; set; }
        public BasicGeoposition Position { get; set; }
        public bool Available { get; set; }
        public DateTime Date { get; set; }
    }
}
