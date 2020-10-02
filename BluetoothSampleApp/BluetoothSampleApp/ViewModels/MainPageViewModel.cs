using MvvmHelpers;
using Plugin.Permissions;
using Shiny;
using Shiny.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using static Xamarin.Essentials.Permissions;

namespace BluetoothSampleApp.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        public ObservableCollection<IPeripheral> Devices { get; set; } = new ObservableCollection<IPeripheral>();
        public Command Search { get; set; }
        public Command Stop { get; set; }
        public Command<IPeripheral> ReadFromDeviceCommand { get; set; }

        public IPeripheral SelectedDevice { get; set; }

        public bool IsSearching { get; set; }

        IDisposable _searching;

        private Dictionary<Guid, IPeripheral> _cache = new Dictionary<Guid, IPeripheral>();
        private IBleManager _manager;

        public MainPageViewModel()
        {
            _manager = ShinyHost.Resolve<IBleManager>();

            Search = new Command(async () => await StartSearch());
            Stop = new Command(StopSearch);
            ReadFromDeviceCommand = new Command<IPeripheral>(async (peripheral) => await ReadFromDevice(peripheral));
        }

        private async Task ReadFromDevice(IPeripheral device)
        {
            StopSearch();
            var ServiceId = Guid.Parse("0000180A-0000-1000-8000-00805F9B34FB");
            var ManufacturerName = Guid.Parse("00002A29-0000-1000-8000-00805F9B34FB");

            var connectedDevice = await device.ConnectWait();
            var service = await device.GetKnownService(ServiceId);
            var characteristic = await service.GetKnownCharacteristics(new Guid[] { ManufacturerName });

            if (characteristic.CanRead())
            {
                var data = await characteristic.Read();
                var readValue = Encoding.UTF8.GetString(data.Data);
                Debug.WriteLine($"> Read characteristic is: {readValue}");
            }
        }

        private async Task StartSearch()
        {
            bool result = await RequestPermissions<LocationAlwaysPermission>();

            if (!result)
            {
                return;
            }

            ToggleIsSearching(true);
            
            _searching = _manager.Scan().Subscribe(scanResult =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (!_cache.ContainsKey(scanResult.Peripheral.Uuid))
                    {
                        _cache.Add(scanResult.Peripheral.Uuid, scanResult.Peripheral);
                        Devices.Add(scanResult.Peripheral);
                    }
                    else
                    {
                        _cache[scanResult.Peripheral.Uuid] = scanResult.Peripheral;
                        var device = Devices.First(d => d.Uuid == scanResult.Peripheral.Uuid);
                        device = scanResult.Peripheral;
                    }
                });
            });
        }

        private void StopSearch()
        {
            if (_manager.IsScanning)
            {
                ToggleIsSearching(false);
                _searching?.Dispose();
            }
        }

        private void ToggleIsSearching(bool value)
        {
            IsSearching = value;
            OnPropertyChanged(nameof(IsSearching));
        }

        private async Task<bool> RequestPermissions<T>() where T: Plugin.Permissions.BasePermission
        {
            var status = await CrossPermissions.Current.CheckPermissionStatusAsync<LocationAlwaysPermission>();
            if (status != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {
                Console.WriteLine("Does not have storage permission granted, requesting.");
                var results = await CrossPermissions.Current.RequestPermissionAsync<LocationAlwaysPermission>();
                if (results != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    Console.WriteLine("Storage permission Denied.");
                    return false;
                }
            }

            return true;
        }
    }
}
