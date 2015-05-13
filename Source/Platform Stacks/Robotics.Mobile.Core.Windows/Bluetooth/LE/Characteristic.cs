using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Characteristic : ICharacteristic
    {
        private GattCharacteristic _characteristic;

        public event EventHandler<CharacteristicReadEventArgs> ValueUpdated;

        public Guid ID
        {
            get { return _characteristic.Uuid; }
        }

        public string Uuid
        {
            get { return _characteristic.Uuid.ToString(); }
        }

        private byte[] _value;
        public byte[] Value
        {
            get { return _value; }
        }

        private string _stringValue;
        public string StringValue
        {
            get { return _stringValue; }
        }

        // Win 8.1 does not support GetAllDescriptors, so we'll have to compile a list by querying for each known descriptor type.
        private IList<IDescriptor> _descriptors;
        public IList<IDescriptor> Descriptors 
        {
            get 
            {
                if (_descriptors == null)
                    _descriptors = KnownDescriptors.All()
                        .SelectMany((kd) => _characteristic.GetDescriptors(kd.ID))
                        .Select((d) => new Descriptor(d))
                        .Cast<IDescriptor>().ToList();
                return _descriptors;
            }
        }

        public object NativeCharacteristic
        {
            get { return _characteristic; }
        }

        public string Name
        {
            get { return KnownCharacteristics.Lookup(this.ID).Name; }
        }

        public CharacteristicPropertyType Properties
        {
            get 
            {
                CharacteristicPropertyType type = 0;

                if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.Broadcast) == GattCharacteristicProperties.Broadcast)
                    type = type | CharacteristicPropertyType.Broadcast;
                if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.Read) == GattCharacteristicProperties.Read)
                    type = type | CharacteristicPropertyType.Read;
                if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.WriteWithoutResponse) == GattCharacteristicProperties.WriteWithoutResponse)
                    type = type | CharacteristicPropertyType.WriteWithoutResponse;
                //if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.Write) == GattCharacteristicProperties.Write)
                //    type = type | CharacteristicPropertyType.???;
                if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.Notify) == GattCharacteristicProperties.Notify)
                    type = type | CharacteristicPropertyType.Notify;
                if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.Indicate) == GattCharacteristicProperties.Indicate)
                    type = type | CharacteristicPropertyType.Indicate;
                if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.AuthenticatedSignedWrites) == GattCharacteristicProperties.AuthenticatedSignedWrites)
                    type = type | CharacteristicPropertyType.AuthenticatedSignedWrites;
                if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.ExtendedProperties) == GattCharacteristicProperties.ExtendedProperties)
                    type = type | CharacteristicPropertyType.ExtendedProperties;
                //if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.ReliableWrites) == GattCharacteristicProperties.ReliableWrites)
                //    type = type | CharacteristicPropertyType.???;
                //if ((_characteristic.CharacteristicProperties & GattCharacteristicProperties.WritableAuxiliaries) == GattCharacteristicProperties.WritableAuxiliaries)
                //    type = type | CharacteristicPropertyType.???;

                return type;
            }
        }

        public bool CanRead
        {
            get { return (_characteristic.CharacteristicProperties & GattCharacteristicProperties.Read) == GattCharacteristicProperties.Read; }
        }

        public bool CanUpdate
        {
            get { return (_characteristic.CharacteristicProperties & GattCharacteristicProperties.Notify) == GattCharacteristicProperties.Notify; }
        }

        public bool CanWrite
        {
            get { return (_characteristic.CharacteristicProperties & GattCharacteristicProperties.Write) == GattCharacteristicProperties.Write; }
        }

        public async void StartUpdates()
        {
            var doRefresh = Refresh();
            if (CanUpdate) 
                await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            await doRefresh;
        }

        public async void StopUpdates()
        {
            if (!CanUpdate) return;
            await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
        }

        public async Task<ICharacteristic> ReadAsync()
        {
            await Refresh();
            return this;
        }

        public async void Write(byte[] data)
        {
            if (!CanWrite) return;

            using (DataWriter valueWriter = new DataWriter())
            {
                valueWriter.WriteBytes(data);
                await _characteristic.WriteValueAsync(valueWriter.DetachBuffer());

            }
        }

        private async Task Refresh()
        {
            if (!CanRead) return;

            var result = await _characteristic.ReadValueAsync();
            if (result.Status == GattCommunicationStatus.Success)
            {
                using (DataReader valueReader = DataReader.FromBuffer(result.Value))
                {
                    valueReader.ReadBytes(_value);
                    _stringValue = System.Text.Encoding.UTF8.GetString(_value, 0, _value.Length); //Is this the correct encoding?
                }
            }
        }

        private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (ValueUpdated != null)
                ValueUpdated(this, new CharacteristicReadEventArgs() { Characteristic = this });
        }       
        
        public Characteristic(GattCharacteristic characteristic)
        {
            _characteristic = characteristic;
            _characteristic.ValueChanged += OnCharacteristicValueChanged;
        }

    }
}
