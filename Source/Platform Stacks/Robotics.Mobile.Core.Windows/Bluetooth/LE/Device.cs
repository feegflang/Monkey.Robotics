using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Device : IDevice
    {

#if WINDOWS_PHONE_APP
        private BluetoothLEDevice _device;
#else
        private PnpObjectWatcher _watcher;
#endif
        private DeviceInformation _deviceInfo;
        private bool _isConnecting;
        
        private string _deviceContainerId;

        public event EventHandler ServicesDiscovered;
        public event EventHandler StateChanged;

        public Guid ID
        {
            get
            {
                return Guid.Parse(_deviceContainerId);
            }
        }

        public string Name
        {
            get
            {
                return _deviceInfo.Name;
            }
        }

        public object NativeDevice
        {
            get
            {
                return _deviceInfo;
            }
        }

        public int Rssi
        {
            get
            {
                return 0; //Not available in Windows/Windows Phone
            }
        }

        private IList<IService> _services;
        public IList<IService> Services
        {
            get
            {
                return _services;
            }
        }

        public DeviceState State
        {
            get
            {
                if (_isConnecting)
                    return DeviceState.Connecting;
                return GetState();
            }
        }

#if WINDOWS_PHONE_APP
        private DeviceState GetState()
        {
            switch (_device.ConnectionStatus)
            {
                case BluetoothConnectionStatus.Connected:
                    return DeviceState.Connected;
                case BluetoothConnectionStatus.Disconnected:
                default:
                    return DeviceState.Disconnected;
            }
        }
#else
        private DeviceState _state;
        private DeviceState GetState()
        {
                return _state;
        }
#endif

        /// <summary>
        /// Discover services on the BLE device. IMPORTANT: On Windows, this must first be called from the UI 
        /// thread in order to display a consent dialog to the user.
        /// </summary>
        /// <remarks>
        /// See https://msdn.microsoft.com/en-us/library/windows/apps/windows.devices.bluetooth.genericattributeprofile.gattdeviceservice.fromidasync%28v=win.10%29.aspx for
        /// information on the UI thread requirement.
        /// </remarks>
        public void DiscoverServices()
        {
            DiscoverServices(true);
        }

        internal async void Connect()
        {
            _isConnecting = true;


            //var f = await PnpObject.CreateFromIdAsync(PnpObjectType.DeviceContainer, "{" + _deviceInfo.Properties["System.Devices.ContainerId"].ToString() + "}", new string[] { "System.Devices.Connected" });//_deviceInfo.Properties[_deviceInfo.Properties.Keys.ToList()[1]].ToString(), new string[] { });

            if (StateChanged != null)
                StateChanged(this, EventArgs.Empty);

            try
            {
                // There is no "Connect" function. In a wireless device API.
                // The OS will automatically connect a device if we try to read from it.
                await DiscoverServices(false);
                foreach (Service service in _services)
                    if (service != null)
                        if (await service.TryAttemptConnect()) break;
            }
            finally
            {
                _isConnecting = false;
                if (StateChanged != null)
                    StateChanged(this, EventArgs.Empty);
            }
        }

#if WINDOWS_PHONE_APP
        private void DiscoverServices(bool notify)
        {
            var discoveredServices = new List<IService>();

            discoveredServices.AddRange(_device.GattServices.Select((s) => new Service(s, false)));

            _services = discoveredServices;

            if (notify && (ServicesDiscovered != null))
                ServicesDiscovered(this, EventArgs.Empty);
        }

        public Device(DeviceInformation device)
        {
            _deviceInfo = device;
            _device = BluetoothLEDevice.FromIdAsync(_deviceInfo.Id).AsTask().Result;
            _device.ConnectionStatusChanged += device_ConnectionStatusChanged;
            _deviceContainerId = device.Properties["System.Devices.ContainerId"].ToString();
        }

        void device_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (StateChanged != null)
                StateChanged(this, EventArgs.Empty);
        }
#else
        private async Task DiscoverServices(bool notify)
        {
            var primaryService = await GattDeviceService.FromIdAsync(_deviceInfo.Id);
            if (primaryService != null)
            {
                var discoveredServices = new List<IService>();
                discoveredServices.Add(new Service(primaryService, true));

                discoveredServices.AddRange(KnownServices.All()
                                                .SelectMany((ks) => primaryService.GetIncludedServices(ks.ID))
                                                .Select((s) => new Service(s, false))
                                                .Cast<IService>());

                _services = discoveredServices;
            }
            else
            {
                _services = new List<IService>();
            }

            if (notify && (ServicesDiscovered != null))
                ServicesDiscovered(this, EventArgs.Empty);
        }

        private async void StartDeviceConnectionWatcher()
        {
            var container = await PnpObject.CreateFromIdAsync(PnpObjectType.DeviceContainer, _deviceContainerId, new string[] { "System.Devices.Connected" });
            SetConnectedState(container.Properties);

            _watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer,
                new string[] { "System.Devices.Connected" }, string.Empty);

            _watcher.Updated += DeviceConnection_Updated;
            _watcher.Start();
        }

        private void SetConnectedState(IReadOnlyDictionary<string, object> args)
        {
            bool isConnected = false;
            if (Boolean.TryParse(args["System.Devices.Connected"].ToString(), out isConnected) && isConnected)
                _state = DeviceState.Connected;
            else
                _state = DeviceState.Disconnected;

            if (StateChanged != null)
                StateChanged(this, EventArgs.Empty);
        }

        private void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            if (_deviceContainerId == args.Id)
                SetConnectedState(args.Properties);
        }

        public Device(DeviceInformation device)
        {
            _deviceInfo = device;
            _deviceContainerId = "{" + device.Properties["System.Devices.ContainerId"].ToString() + "}";
            
            StartDeviceConnectionWatcher();
        }

#endif

    }
}
