using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.Phone.UI.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Pathfinder.Logic;
using Pathfinder.Model;
using SharpKml.Dom;
using Point = Pathfinder.Model.Point;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Pathfinder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CacheList : Page
    {
        private readonly AppState _appState;
        private readonly GpsTracker _geolocator;
        private readonly GeocacheSearchService _searchService;

        public CacheList()
        {
            _appState = AppState.GetInstance();
            _geolocator = new GpsTracker();
            _searchService = new GeocacheSearchService();  

            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter.GetType() == typeof(KmlDocument)) {
                ImportFromKmlFile(e);
            }

            DataContext = _appState;
        }

        private void ImportFromKmlFile(NavigationEventArgs e)
        {
            KmlDocument kd = e.Parameter as KmlDocument;
            List<Folder> folderNodes = kd.GetFolders();
            List<Placemark> placemarkNodes = kd.GetPlacemarks(folderNodes[0]);
            foreach (var placemarkNode in placemarkNodes)
            {
                try
                {
                    if (!KmlDocument.IsRoutePlacemark(placemarkNode))
                    {
                        Point p = kd.GetPoint(placemarkNode);
                        _appState.Points.Add(p);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.StackTrace);
                }

            }
        }

        private void PointsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try {
                Point selected = PointsList.SelectedItem as Point;
                if (selected != null) {
                    _appState.Target = selected.Position;
                    Frame.Navigate(typeof (MainPage));
                }
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }
        }

        private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            try {
                ProgressRing.IsActive = true;
                var pos = await _geolocator.GetCoordinates();
                var list = await _searchService.FindCachesNearby(pos.Coordinate.Point.Position, 5000);
                _appState.CachesNearby.Clear();
                foreach (var cache in list) {
                    _appState.CachesNearby.Add(cache);
                }
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            } finally {
                ProgressRing.IsActive = false;
            }
        }

        private void SetTargetClick(object sender, RoutedEventArgs e)
        {
            Point p = BuildPointFromPopupData();
            _appState.Points.Add(p);
            _appState.Target = p.Position;
        }

        private void AddPointClick(object sender, RoutedEventArgs e)
        {
            BuildPointFromPopupData();
        }

        private void ClosePointPopup(object sender, RoutedEventArgs e)
        {
            PointPopup.IsOpen = false;
        }

        private Point BuildPointFromPopupData()
        {
            PointPopup.IsOpen = false;
            String pos = PointCoordinatesTextBox.Text.Trim();
            String title = PointDescriptionTextBox.Text.Trim();

            var geopos = DegMinFormatRB.IsChecked.Value ? GeoUtils.ParseWgs84(pos) : GeoUtils.ParseGeopos(pos);
            Point p = new Point() { Description = "", Position = geopos, Title = title };
            _appState.Points.Add(p);
            return p;
        }

        private void CalculateAzimuthClick(object sender, RoutedEventArgs e)
        {
            if (!PointPopup.IsOpen)
            {
                PointPopup.IsOpen = true;
            }
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            try {
                var point = _appState.Points.First(p => p.Title == "Home");
                _appState.Points.Clear();
                if (point != null) {
                    _appState.Points.Add(point);
                }
            } catch (Exception ex) {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (MainPage));
        }

        private void CachesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Geocache selected = CachesList.SelectedItem as Geocache;
                if (selected != null)
                {
                    _appState.Target = selected.Position;
                    Frame.Navigate(typeof(MainPage));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ERROR] " + ex.Message);
            }
        }
    }
}
