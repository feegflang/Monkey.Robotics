using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Adapter : IAdapter
    {
        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;

        public event EventHandler<DeviceConnectionEventArgs> DeviceConnected;

        public event EventHandler<DeviceConnectionEventArgs> DeviceDisconnected;

        public event EventHandler ScanTimeoutElapsed;

        public bool IsScanning
        {
            get { throw new NotImplementedException(); }
        }

        public IList<IDevice> DiscoveredDevices
        {
            get { throw new NotImplementedException(); }
        }

        public IList<IDevice> ConnectedDevices
        {
            get { throw new NotImplementedException(); }
        }

        public void StartScanningForDevices()
        {
            throw new NotImplementedException();
        }

        public void StartScanningForDevices(Guid serviceUuid)
        {
            throw new NotImplementedException();
        }

        public void StopScanningForDevices()
        {
            throw new NotImplementedException();
        }

        public void ConnectToDevice(IDevice device)
        {
            throw new NotImplementedException();
        }

        public void DisconnectDevice(IDevice device)
        {
            throw new NotImplementedException();
        }
    }
}
