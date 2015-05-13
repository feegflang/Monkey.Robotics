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

        public IList<IDevice> DiscoveredDevices
        {
            get
            {
                return new IDevice[] { }.ToList();
            }
        }

        private IList<IDevice> _connected;
        public IList<IDevice> ConnectedDevices
        {
            get { return _connected; }
        }

        public void StartScanningForDevices()
        {
            RefreshConnectedDevices(_serviceGuids.Concat(KnownServices.All().Select((ks) => ks.ID)));
        }

        public void StartScanningForDevices(Guid serviceUuid)
        {
            RefreshConnectedDevices(new Guid[] {serviceUuid});
        }

        public void StopScanningForDevices()
        {
            return;
        }

        public void ConnectToDevice(IDevice device)
        {
            ScanNotSupported();
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

        private async void RefreshConnectedDevices(IEnumerable<Guid> searchGuids)
        {
            _isScanning = true;
            List<DeviceInformation> allConnected = new List<DeviceInformation>();
            foreach (Guid guid in searchGuids)
                allConnected.AddRange(await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(guid), new string[] { "System.Devices.ContainerId" }));

            _isScanning = false;
            _connected = allConnected.Distinct(new DeviceComparer()).Select((d) => new Device(d)).Cast<IDevice>().ToList();
            if (DeviceConnected != null)
                foreach (IDevice connected in _connected)
                    DeviceConnected(this, new DeviceConnectionEventArgs { Device = connected, ErrorMessage = null });

        }

        public WindowsAdapter(IEnumerable<Guid> serviceGuids)
        {
            _serviceGuids = new List<Guid>(serviceGuids);
        }
    }
}
