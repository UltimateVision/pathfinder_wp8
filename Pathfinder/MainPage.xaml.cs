using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Pathfinder.Logic;
using Pathfinder.Model;
using Pathfinder.Views;

namespace Pathfinder
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Compass _compass;
        private GpsTracker _geolocator;
        private GeocacheSearchService _searchService;
        private uint _desiredReportInterval;
        private bool _lockAzimuth;
        private AppState _runtimeSettings;
        private bool _adjustAzimuth = false;
        private double _lastAzimuthAdjustment = 0.0;
        private const String ConfigFilename = "pathfinderSettings.json";

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            _compass = Compass.GetDefault();
            if (_compass != null) {
                uint minReportingInterval = _compass.MinimumReportInterval;
                _desiredReportInterval = minReportingInterval > 15 ? minReportingInterval : 15;
                EnableCompass();
            }

            _geolocator = new GpsTracker();
            _searchService = new GeocacheSearchService();
            _runtimeSettings = AppState.GetInstance();

            Application.Current.Resuming += CurrentOnResuming;
        }

        private void CurrentOnResuming(object sender, object o)
        {
            try {
                DataContext = _runtimeSettings;
//                PointsList.ItemsSource = _runtimeSettings.Points;
                EnableCompass();
                EnableGps();
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, HideStatusBar);
                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, GetInitialGpsPosition);
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }
        }

        private async void PositionChanged(object sender, Geocoordinate args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                UpdateGpsData(args);
                if (_runtimeSettings.TargetSet) {
                    ShowDistance(args);
                }
            });
        }

        private void ShowDistance(Geocoordinate args)
        {
            double distanceToTarget = GeoUtils.Distance(args.Point.Position, _runtimeSettings.Target);
            String distance = (distanceToTarget / 1000 < 1.0)
                ? String.Format("{0,5:0.0} m", distanceToTarget)
                : String.Format("{0,5:0.0} km", distanceToTarget / 1000);
            DistanceBox.Text = distance;
            GpsAccurracyBlock.Text = String.Format("{0,5:0.0} m", args.Accuracy);
            if (_runtimeSettings.TargetSet && _lastAzimuthAdjustment - distanceToTarget > 20.0) {
                _lastAzimuthAdjustment = distanceToTarget;
                _runtimeSettings.Azimuth = GeoUtils.DegreeBearing(args.Point.Position, _runtimeSettings.Target);
            }
        }

        private void UpdateGpsData(Geocoordinate geocoordinate)
        {
            BasicGeoposition position = geocoordinate.Point.Position;
            LatitudeTB.Text = String.Format("{0} {1}", position.Latitude > 0 ? "N" : "S", position.Latitude);
            LongitudeTB.Text = String.Format("{0} {1}", position.Longitude > 0 ? "E" : "W", position.Longitude);
        }

        private async void ReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                CompassReading reading = args.Reading;
                double headingMagneticNorth = reading.HeadingMagneticNorth;
                MagneticNorthHeading.Text = String.Format("{0,5:0.0}", headingMagneticNorth);
                if (reading.HeadingTrueNorth != null) {
                    double? headingTrueNorth = reading.HeadingTrueNorth.Value;
                    double diff = Math.Abs(headingTrueNorth.Value - headingMagneticNorth);
                    DiffTrueMag.Text = String.Format("Diff: {0,5:0.0}", diff);
                }

                if (_lockAzimuth) {
                    _runtimeSettings.Azimuth = headingMagneticNorth;
                    _lockAzimuth = false;
                }

                RotateTransform rt = new RotateTransform {Angle = 360.0 - headingMagneticNorth};
                CompassRose.RenderTransform = rt;
                CompassNeedle.RenderTransform = _runtimeSettings.Azimuth < 0 
                                              ? rt 
                                              : new RotateTransform {Angle = _runtimeSettings.Azimuth - headingMagneticNorth};

                String accuracy = "Accurracy: ";

                switch (reading.HeadingAccuracy) {
                    case MagnetometerAccuracy.Unreliable:
                        accuracy += "Unreliable";
                        break;
                    case MagnetometerAccuracy.Approximate:
                        accuracy += "Approximate";
                        break;
                    case MagnetometerAccuracy.High:
                        accuracy += "High";
                        break;
                    case MagnetometerAccuracy.Unknown:
                        accuracy += "Unknown";
                        break;
                    default:
                        accuracy += "No data";
                        break;
                }

                AccurracyTB.Text = accuracy;

            });
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = _runtimeSettings;
            SetAzimuth();
            EnableCompass();
            EnableGps();
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, HideStatusBar);
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, GetInitialGpsPosition);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DisableCompass();
            DisableGps();
            base.OnNavigatedFrom(e);
        }

        private void SerializeRuntimeSettings()
        {
            AppState.SaveAppState();
        }

        private async Task DeserializeRuntimeSettings()
        {
            _runtimeSettings = await AppState.ReadSavedAppState();
        }

        private void EnableCompass()
        {
            if (_compass != null)
            {
                _compass.ReportInterval = _desiredReportInterval;
                _compass.ReadingChanged += ReadingChanged;
            }
        }

        private void DisableCompass()
        {
            if (_compass != null)
            {
                _compass.ReadingChanged -= ReadingChanged;
                _compass.ReportInterval = 0;
            }
        }

        private void EnableGps()
        {
            try {
                _geolocator.PosChanged += PositionChanged;
                _geolocator.StartTracking();
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        private void DisableGps()
        {
            try {
                _geolocator.PosChanged -= PositionChanged;
                _geolocator.StopTracking();
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void GetInitialGpsPosition()
        {
            Geoposition pos = await _geolocator.GetCoordinates();
            UpdateGpsData(pos.Coordinate);
        }

        private async void HideStatusBar()
        {
            StatusBar statusBar = StatusBar.GetForCurrentView();
            await statusBar.HideAsync();
        }

        private async void ShowStatusBar()
        {
            StatusBar statusBar = StatusBar.GetForCurrentView();
            await statusBar.ShowAsync();
        }

        private void LockAzimuthClick(object sender, RoutedEventArgs e)
        {
            _lockAzimuth = true;
        }

        private void CalculateAzimuthClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (CacheList));
        }

        private void NavigateHomeClick(object sender, RoutedEventArgs e)
        {
            try {
                var home = _runtimeSettings.Points.First(t => t.Title == "Home");
                if (home != null) {
                    SetAzimuth(home.Position);
                    _runtimeSettings.Target = home.Position;
                }
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }
        }

        private async void SetAzimuth(BasicGeoposition destination)
        {
            Geoposition geopos = await _geolocator.GetCoordinates();
            _runtimeSettings.Azimuth = GeoUtils.DegreeBearing(geopos.Coordinate.Point.Position, destination);
            LatLonGrid.Visibility = Visibility.Collapsed;
            DistanceGrid.Visibility = Visibility.Visible;
            _runtimeSettings.Target = destination;
            _runtimeSettings.TargetSet = true;
            ShowDistance(geopos.Coordinate);
        }

        private async void SetAzimuth()
        {
            Geoposition geopos = await _geolocator.GetCoordinates();
            _runtimeSettings.Azimuth = GeoUtils.DegreeBearing(geopos.Coordinate.Point.Position, _runtimeSettings.Target);
            LatLonGrid.Visibility = Visibility.Collapsed;
            DistanceGrid.Visibility = Visibility.Visible;
            _runtimeSettings.TargetSet = true;
            ShowDistance(geopos.Coordinate);
        }

        private void ReleaseAzimuthClick(object sender, RoutedEventArgs e)
        {
            _runtimeSettings.Azimuth = -1;
            if (_runtimeSettings.TargetSet) {
                _runtimeSettings.TargetSet = false;
                LatLonGrid.Visibility = Visibility.Visible;
                DistanceGrid.Visibility = Visibility.Collapsed;
            }
        }

        private async void SetHomeClick(object sender, RoutedEventArgs e)
        {
            Geoposition geopos = await _geolocator.GetCoordinates();
            _runtimeSettings.Points.Add(new Point(){Description = "", Position = geopos.Coordinate.Point.Position, Title = "Home"});
        }

        private void SetTargetClick(object sender, RoutedEventArgs e)
        {
            Point p = BuildPointFromPopupData();
            _runtimeSettings.Points.Add(p);
            SetAzimuth(p.Position);
            _runtimeSettings.Target = p.Position;
        }

        private void ClosePointPopup(object sender, RoutedEventArgs e)
        {
            PointPopup.IsOpen = false;
        }

        private void AddPointClick(object sender, RoutedEventArgs e)
        {
            BuildPointFromPopupData();
        }

        private Point BuildPointFromPopupData()
        {
            PointPopup.IsOpen = false;
            String pos = PointCoordinatesTextBox.Text.Trim();
            String title = PointDescriptionTextBox.Text.Trim();

            var geopos = DegMinFormatRB.IsChecked.Value ? GeoUtils.ParseWgs84(pos) : GeoUtils.ParseGeopos(pos);
            Point p = new Point() { Description = "", Position = geopos, Title = title };
            _runtimeSettings.Points.Add(p);
            return p;
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            try {
                var point = _runtimeSettings.Points.First(p => p.Title == "Home");
                _runtimeSettings.Points.Clear();
                if (point != null) {
                    _runtimeSettings.Points.Add(point);
                }
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }
        }

        private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            try {
                var pos = await _geolocator.GetCoordinates();
                var list = await _searchService.FindCachesNearby(pos.Coordinate.Point.Position, 5000);
                foreach (var cache in list) {
                    _runtimeSettings.CachesNearby.Add(cache);
                }
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            //TODO: Navigate to settings page
        }
    }
}
