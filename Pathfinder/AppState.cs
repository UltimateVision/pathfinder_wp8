using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Newtonsoft.Json;
using Pathfinder.Model;

namespace Pathfinder
{
    public sealed class AppState
    {
        private const String ConfigFilename = "pathfinderSettings.json";

        private ObservableCollection<Point> _points = new ObservableCollection<Point>();
        private ObservableCollection<Geocache> _cachesNearby = new ObservableCollection<Geocache>();

        private Boolean _isLoaded;

        public double Azimuth = -1;
        public bool TargetSet;
        public BasicGeoposition Target { get; set; }

        public ObservableCollection<Point> Points
        {
            get { return _points; }
            set { _points = value; }
        }

        public ObservableCollection<Geocache> CachesNearby
        {
            get { return _cachesNearby; }
            set { _cachesNearby = value; }
        }

        public bool IsLoaded()
        {
            return _isLoaded;
        }

        private static AppState _appState;

        private AppState() {}

        public static AppState GetInstance()
        {
            if (_appState == null) {
                _appState = new AppState();
            }

            return _appState;
        }

        public static async Task<AppState> ReadSavedAppState()
        {
            if (_appState == null || !_appState.IsLoaded()) {
                try {
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    StorageFile configFile = await folder.GetFileAsync(ConfigFilename);
                    String json = await FileIO.ReadTextAsync(configFile);
                    _appState = JsonConvert.DeserializeObject<AppState>(json);
                    _appState._isLoaded = true;
                } catch (Exception e) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                }
            }

            return GetInstance();
        }

        public static async void SaveAppState()
        {
            try {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile configFile = await folder.CreateFileAsync(ConfigFilename, CreationCollisionOption.OpenIfExists);
                String json = JsonConvert.SerializeObject(_appState);
                await FileIO.WriteTextAsync(configFile, json);
            } catch (Exception e) {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                //Is this first run?
            }
        }
    }
}