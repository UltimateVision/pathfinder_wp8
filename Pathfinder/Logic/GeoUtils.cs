using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Media.Devices;

namespace Pathfinder.Logic
{
    public static class GeoUtils
    {
        private const double EquatorialEarthRadius = 6378137.0;
        private const double PolarEarthRadius = 6356752.3;

        public static double DegreeBearing(BasicGeoposition initialPosition, BasicGeoposition destination)
        {
            var dLon = ToRad(destination.Longitude - initialPosition.Longitude);
            var dPhi = Math.Log(
                Math.Tan(ToRad(destination.Latitude) / 2 + Math.PI / 4) / Math.Tan(ToRad(initialPosition.Latitude) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        public static double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
            return (ToDegrees(radians) + 360) % 360;
        }

        public static double Distance(BasicGeoposition initialPosition, BasicGeoposition destination)
        {
            double initialRadLat = ToRad(initialPosition.Latitude);
            double destinationRadLat = ToRad(destination.Latitude);
            double latDiff = ToRad(destination.Latitude - initialPosition.Latitude);
            double lonDiff = ToRad(destination.Longitude - initialPosition.Longitude);

            double a = Math.Sin(latDiff / 2) * Math.Sin(latDiff / 2) + 
                       Math.Cos(initialRadLat) * Math.Cos(destinationRadLat) *
                       Math.Sin(lonDiff / 2) * Math.Sin(lonDiff / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (EquatorialEarthRadius + PolarEarthRadius) / 2 * c;
        }

        public static double DegMinSecToDecimal(String value)
        {
            String[] split = value.Split(' ');
            double deg = Double.Parse(split[0]);
            if (split.Length > 1) {
                double min = Double.Parse(split[1], new CultureInfo("en-US")) / 60;
                deg += min;
                if (split.Length > 2) {
                    double sec = Double.Parse(split[2], new CultureInfo("en-US")) / 3600;
                    deg += sec;
                }
            }

            return deg;
        }

        public static BasicGeoposition DestinationPoint(BasicGeoposition position, double distance, double bearing)
        {
            var latRad = ToRad(position.Latitude);
            var distanceTravelled = distance / EquatorialEarthRadius;
            var bearingRad = ToRad(bearing);

            var lat = Math.Asin(Math.Sin(latRad) * Math.Cos(distanceTravelled) + Math.Cos(latRad) * Math.Sin(distanceTravelled) * Math.Cos(bearingRad));
            var lon = ToRad(position.Longitude) + Math.Atan2(Math.Sin(bearingRad) * Math.Sin(distanceTravelled) * Math.Cos(latRad), Math.Cos(distanceTravelled) - Math.Sin(latRad) * Math.Sin(lat));

            return new BasicGeoposition() {Latitude = ToDegrees(lat), Longitude = ToDegrees(lon)};
        }

        public static BasicGeoposition ParseWgs84(string pos)
        {
            String[] split = pos.Split(';');
            double lat = DegMinSecToDecimal(split[0].Trim());
            double lon = DegMinSecToDecimal(split[1].Trim());
            return new BasicGeoposition() {Latitude = lat, Longitude = lon};
        }

        public static BasicGeoposition ParseGeopos(string pos)
        {
            String[] split = pos.Split(';');
            double lat = Double.Parse(split[0], new CultureInfo("en-US"));
            double lon = Double.Parse(split[1], new CultureInfo("en-US"));
            return new BasicGeoposition() {Latitude = lat, Longitude = lon};
        }
    }
}
