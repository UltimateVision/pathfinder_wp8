using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation;
using Pathfinder.Model;
using Pathfinder.Utils;

namespace Pathfinder.Logic
{
    public class GeocacheSearchService
    {
        private String _serviceUrl = @"https://www.geocaching.pl/services/getcaches.php?BBOX=";

        public async Task<List<Geocache>> FindCachesNearby(BasicGeoposition position, double radius)
        {
            List<Geocache> caches = new List<Geocache>();

            var bboxRightCoord = GeoUtils.DestinationPoint(position, radius, 45.0);
            var bboxLeftCoord = GeoUtils.DestinationPoint(position, radius, -135.0);
            String uri = String.Format("{0}{1},{2},{3},{4}", _serviceUrl, bboxLeftCoord.Longitude, bboxLeftCoord.Latitude, bboxRightCoord.Longitude, bboxRightCoord.Latitude);

            try {
                HttpClient client = new HttpClient();
                var callUri = new Uri(uri);
                String response = await client.GetStringAsync(callUri);
                ParseXmlResponse(response, caches);
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }

            return caches;
        }

        private void ParseXmlResponse(string response, List<Geocache> caches)
        {
            var formatProvider = new CultureInfo("en-US");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response);
            var list = doc.SelectNodes("/markers/marker[@available='1']");
            foreach (IXmlNode xmlNode in list) {
                try {
                    Geocache cache = new Geocache();
                    var attributes = XmlUtils.GetAttributeMap(xmlNode);
                    cache.Name = attributes["cachename"];
                    cache.Username = attributes["username"];
                    cache.Id = attributes["waypoint"];
                    cache.Type = attributes["cache_type_name"];
                    cache.Difficulty = Convert.ToDouble(attributes["difficulty"], formatProvider);
                    cache.Terrain = Convert.ToDouble(attributes["terrain"], formatProvider);
                    cache.Size = attributes["container_name"];
                    cache.Available = attributes["available"] == "1";
                    cache.Position = new BasicGeoposition() {
                        Latitude = Convert.ToDouble(attributes["lat"], formatProvider), Longitude = Convert.ToDouble(attributes["lng"], formatProvider)
                    };
                    caches.Add(cache);
                } catch (Exception ex) {
                    Debug.WriteLine("[ERROR] " + ex.Message);
                }
            }
        }
    }
}
