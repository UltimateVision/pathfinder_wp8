using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation;
using Pathfinder.Model;
using Pathfinder.Utils;
using SharpKml.Dom;
using SharpKml.Engine;
using Point = Pathfinder.Model.Point;
using KmlPoint = SharpKml.Dom.Point;

namespace Pathfinder.Logic
{
    public class KmlDocument
    {
        private readonly KmlFile kf;
        private String _documentDescription = "";
        private static readonly CultureInfo FormatProvider = new CultureInfo("en-US");

        public KmlDocument(String data)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            kf = KmlFile.Load(ms);
            Kml kml = kf.Root as Kml;
            Document doc = kml?.Flatten().OfType<Document>().FirstOrDefault();
            if (doc != null) {
                _documentDescription = (doc.Description != null) ? doc.Description.Text : "";
            }
        }

        public List<Folder> GetFolders()
        {
            Kml kml = kf.Root as Kml;
            if (kml != null) {
                return new List<Folder>(kml.Flatten().OfType<Folder>());
            }

            return new List<Folder>();
        }

        public List<Placemark> GetPlacemarks(Folder folder)
        {
            return new List<Placemark>(folder.Flatten().OfType<Placemark>());
        }

        public static bool IsRoutePlacemark(Placemark placemark)
        {
            return placemark.Flatten().OfType<LineString>().Any();
        }

        public Point GetPoint(Placemark placemark)
        {
            Point p = new Point {
                Title = placemark.Name,
                Description = (placemark.Description != null) ? placemark.Description.Text : _documentDescription
            };

            KmlPoint point = placemark.Flatten().OfType<KmlPoint>().FirstOrDefault();
            if (point != null) {
                p.Position = new BasicGeoposition()
                {
                    Longitude = point.Coordinate.Longitude,
                    Latitude = point.Coordinate.Latitude,
                    Altitude = point.Coordinate.Altitude ?? 0d
                };

                return p;
            }

            return null;
        }

        public Route GetRoute(Placemark placemark)
        {
            Route r = new Route {
                Name = placemark.Name,
                Description = (placemark.Description != null) ? placemark.Description.Text : _documentDescription
            };

            LineString ls = placemark.Flatten().OfType<LineString>().FirstOrDefault();
            if (ls != null) {
                foreach (var coordinate in ls.Coordinates)
                {
                    BasicGeoposition bg = new BasicGeoposition()
                    {
                        Longitude = coordinate.Longitude,
                        Latitude = coordinate.Latitude,
                        Altitude = coordinate.Altitude ?? 0d
                    };
                    r.Coordinates.Add(bg);
                }

                return r;
            }

            return null;

        }
    }
}
