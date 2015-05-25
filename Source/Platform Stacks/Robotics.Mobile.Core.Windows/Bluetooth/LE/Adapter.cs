using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    public class WindowsAdapter : IAdapter
    {

        private bool _isScanning;
        private List<Guid> _serviceGuids;

        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;

        public event EventHandler<DeviceConnectionEventArgs> DeviceConnected;

        public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected;

        public event EventHandler ScanTimeoutElapsed;

        public bool IsScanning
        {
            get { return _isScanning; }
        }

        private IList<IDevice> _discovered;
        public IList<IDevice> DiscoveredDevices
        {
            get
            {
                return _discovered;
            }
        }

        private IList<IDevice> _connected;
        public IList<IDevice> ConnectedDevices
        {
            get { return _connected; }
        }

        public void StartScanningForDevices()
        {
            RefreshDiscoveredDevices(_serviceGuids.Concat(KnownServices.All().Select((ks) => ks.ID)));
        }

        public void StartScanningForDevices(Guid serviceUuid)
        {
            RefreshDiscoveredDevices(new Guid[] {serviceUuid});
        }

        public void StopScanningForDevices()
        {
            return;
        }

        public void ConnectToDevice(IDevice device)
        {
            Device nativeDevice = device as Device;
            if (nativeDevice == null) throw new ArgumentException("Unknown device type", "device");

            nativeDevice.Connect();
        }

        public void DisconnectDevice(IDevice device)
        {
            ScanNotSupported();
        }

        private void ScanNotSupported()
        {
            throw new NotSupportedException("Windows does not support scanning, discovery, pairing, or unpairing from within an app. Devices must be paired manually from the system settings (PC & Devices>Bluetooth).");
        }

        private class DeviceComparer : IEqualityComparer<DeviceInformation>
        {

            public bool Equals(DeviceInformation x, DeviceInformation y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(DeviceInformation obj)
            {
                return obj.Id.GetHashCode();
            }
        }

        private async void RefreshDiscoveredDevices(IEnumerable<Guid> searchGuids)
        {
            _isScanning = true;
            List<DeviceInformation> allPaired = new List<DeviceInformation>();
            foreach (Guid guid in searchGuids)
                allPaired.AddRange(await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(guid), new string[] { "System.Devices.ContainerId" }));

            _isScanning = false;
            _discovered = allPaired.Distinct(new DeviceComparer()).Select((d) => new Device(d)).Cast<IDevice>().ToList();
            foreach (Device discovered in _discovered)
            {
                discovered.StateChanged += discoveredDevice_StateChanged;
                if (DeviceDiscovered != null)
                        DeviceDiscovered(this, new DeviceDiscoveredEventArgs { Device = discovered});
                if ((discovered.State == DeviceState.Connected) && (DeviceConnected != null))
                    DeviceConnected(this, new DeviceConnectionEventArgs() { Device = discovered });
            }

        }

        void discoveredDevice_StateChanged(object sender, EventArgs e)
        {
            Device device = sender as Device;
            if (device == null) return;
            if ((device.State == DeviceState.Connected) && (DeviceConnected != null))
                DeviceConnected(this, new DeviceConnectionEventArgs() { Device = device });
        }

        public WindowsAdapter(IEnumerable<Guid> serviceGuids)
        {
            _serviceGuids = new List<Guid>(serviceGuids);
        }
    }
}
