using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Core;

namespace Pathfinder.Logic
{
    public class GpsTracker
    {
        private readonly Geolocator _geolocator;
        public event EventHandler<Geocoordinate> PosChanged; 

        public GpsTracker()
        {
            _geolocator = new Geolocator() { DesiredAccuracy = PositionAccuracy.High };
        }

        public void StartTracking()
        {
            if (_geolocator.ReportInterval == 0) {
                _geolocator.ReportInterval = (uint) TimeSpan.FromSeconds(5).TotalMilliseconds;
            }
            _geolocator.PositionChanged += PositionChanged;
        }

        public void StopTracking()
        {
            _geolocator.PositionChanged -= PositionChanged;
            if (_geolocator.ReportInterval > 0) {
                _geolocator.ReportInterval = (uint) TimeSpan.FromSeconds(0).TotalMilliseconds;
            }
        }

        private async void PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            OnPosChanged(args.Position.Coordinate);
        }

        protected virtual void OnPosChanged(Geocoordinate e)
        {
            var handler = PosChanged;
            if (handler != null) handler(this, e);
        }

        public async Task<Geoposition> GetCoordinates()
        {
            Geoposition pos = await _geolocator.GetGeopositionAsync();
            return pos;
        }
    }
}
