using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Device : IDevice
    {

        private PnpObjectWatcher _watcher;
        private DeviceInformation _device;
        private string _deviceContainerId;

        public event EventHandler ServicesDiscovered;

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
                return _device.Name;
            }
        }

        public object NativeDevice
        {
            get
            {
                return _device;
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

        private DeviceState _state;
        public DeviceState State
        {
            get
            {
                return _state;
            }
        }

        /// <summary>
        /// Discover services on the BLE device. IMPORTANT: On Windows, this must first be called from the UI 
        /// thread in order to display a consent dialog to the user.
        /// </summary>
        /// <remarks>
        /// See https://msdn.microsoft.com/en-us/library/windows/apps/windows.devices.bluetooth.genericattributeprofile.gattdeviceservice.fromidasync%28v=win.10%29.aspx for
        /// information on the UI thread requirement.
        /// </remarks>
        public async void DiscoverServices()
        {
            var primaryService = await GattDeviceService.FromIdAsync(_device.Id);
            
            var discoveredServices = new List<IService>();
            discoveredServices.Add(new Service(primaryService, true));

            discoveredServices.AddRange(KnownServices.All()
                                            .SelectMany((ks) => primaryService.GetIncludedServices(ks.ID))
                                            .Select((s) => new Service(s, false))
                                            .Cast<IService>());

            _services = discoveredServices;

            if (ServicesDiscovered != null)
                ServicesDiscovered(this, EventArgs.Empty);
        }

        private void StartDeviceConnectionWatcher()
        {
            _watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer,
                new string[] { "System.Devices.Connected" }, String.Empty);

            _watcher.Updated += DeviceConnection_Updated;
            _watcher.Start();
        }

        private void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            var connectedProperty = args.Properties["System.Devices.Connected"];
            bool isConnected = false;
            if (_deviceContainerId == args.Id)
            {
                if (Boolean.TryParse(connectedProperty.ToString(), out isConnected) && isConnected)
                    _state = DeviceState.Connected;
                else
                    _state = DeviceState.Disconnected;
            }
        }

        public Device(DeviceInformation device)
        {
            _device = device;
            StartDeviceConnectionWatcher();
            _deviceContainerId = device.Properties["System.Devices.ContainerId"].ToString();
        }


    }
}
