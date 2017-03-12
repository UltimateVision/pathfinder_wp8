using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.ViewManagement;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Pathfinder.Logic;

namespace PathfinderTests
{
    [TestClass]
    public class UnitTest1
    {
        private BasicGeoposition geoposition;
        private String _serviceUrl = @"https://www.geocaching.pl/services/getcaches.php?BBOX=";

        [TestMethod]
        public async Task TestMethod1()
        {
            GpsTracker tracker = new GpsTracker();
            var pos = await tracker.GetCoordinates();
            geoposition = pos.Coordinate.Point.Position;
            var bboxRightCoord = GeoUtils.DestinationPoint(geoposition, 5000, 45.0);
            var bboxLeftCoord = GeoUtils.DestinationPoint(geoposition, 5000, -135.0);
            String uri = String.Format("{0}{1},{2},{3},{4}", _serviceUrl, bboxLeftCoord.Longitude, bboxLeftCoord.Latitude, bboxRightCoord.Longitude, bboxRightCoord.Latitude);
            Debug.WriteLine(uri);
        }
    }
}
