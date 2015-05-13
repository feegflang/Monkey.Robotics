using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Descriptor : IDescriptor
    {

        private GattDescriptor _descriptor;

        public object NativeDescriptor
        {
            get { return _descriptor; }
        }

        public Guid ID
        {
            get { return _descriptor.Uuid; }
        }

        private string _name;
        public string Name
        {
            get {
                if (this._name == null)
                    this._name = KnownDescriptors.Lookup(this.ID).Name;
                return this._name;
            }
        }

        public Descriptor(GattDescriptor descriptor)
        {
            _descriptor = descriptor;
        }
    }
}
