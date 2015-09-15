﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Characteristic : ICharacteristic
    {
        private GattCharacteristic _characteristic;

        public event EventHandler<CharacteristicReadEventArgs> ValueUpdated;

        public static int TransactionTimeout { get; set; }

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

        private IList<IDescriptor> _descriptors;
        public IList<IDescriptor> Descriptors 
        {
            get 
            {
                if (_descriptors == null)
#if WINDOWS_PHONE_APP
                    _descriptors = _characteristic.GetAllDescriptors().Select((c) => new Descriptor(c) as IDescriptor).ToList();
#else
                    // Win 8.1 does not support GetAllDescriptors, so we'll have to compile a list by querying for each known descriptor type.
                    _descriptors = KnownDescriptors.All()
                        .SelectMany((kd) => _characteristic.GetDescriptors(kd.ID))
                        .Select((d) => new Descriptor(d))
                        .Cast<IDescriptor>().ToList();
#endif
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

        internal async Task<ICharacteristic> ForceReadAsync()
        {
            await Refresh(true);
            return this;
        }

        private object _writeLock = new object();
        private List<IBuffer> _currentTransaction;
        private List<IAsyncAction> _currentWriteOperations;


        // This function provides the capability to write to a characteristic in the order that write commands are called, even though
        // the write command on the native characteristic object is an async call. The write performance is significantly faster than 
        // if we called _characteristic.WriteValueAsync(bytes).AsTask().Wait 
        private IAsyncAction PerformWriteOperationAsync(IBuffer writeBuffer)
        {
            lock (_writeLock)
            {
                if (_currentTransaction == null) _currentTransaction = new List<IBuffer>();
                _currentTransaction.Add(writeBuffer);
            }

            return AsyncInfo.Run(async (cancelationToken) =>
                {
                    await Task.Delay(TransactionTimeout);
                    if (cancelationToken.IsCancellationRequested) return;
                    List<IBuffer> myTransaction;
                    lock (_writeLock)
                    {
                        if (_currentTransaction == null) return;
                        myTransaction = _currentTransaction;
                        _currentTransaction = null;
                    }
                    foreach (IBuffer toWrite in myTransaction)
                        await _characteristic.WriteValueAsync(toWrite);
                });
        }

        internal async Task ForceConnectionWriteAsync()
        {
            if (!CanWrite) return;
            using (DataWriter valueWriter = new DataWriter())
            {
                valueWriter.WriteBytes(new byte[] { 13 });
                IBuffer bytes;
                bytes = valueWriter.DetachBuffer();
                await _characteristic.WriteValueAsync(bytes);
            }
        }

        public void Write(byte[] data)
        {
            if (!CanWrite) return;

            using (DataWriter valueWriter = new DataWriter())
            {
                valueWriter.WriteBytes(data);
                IBuffer bytes;
                bytes = valueWriter.DetachBuffer();
                lock (_currentWriteOperations)
                {
                    foreach (var writeOperation in _currentWriteOperations)
                        if (writeOperation.Status == AsyncStatus.Started) writeOperation.Cancel();
                    _currentWriteOperations.Clear();
                    _currentWriteOperations.Add(PerformWriteOperationAsync(bytes));
                }
            }
        }

        private async Task Refresh(bool force = false)
        {
            if (!CanRead) return;

            GattReadResult result;
            try
            {
                result = await _characteristic.ReadValueAsync(force ? Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached : Windows.Devices.Bluetooth.BluetoothCacheMode.Cached );
            }
            catch (Exception ex)
            {
                if ((ex.HResult == -2140864511) || (ex.HResult == -2140864510)) //The characteristic was not found..?
                    return;
                else
                    throw new Exception("Could not read the value.", ex);
            }

            if (result.Status == GattCommunicationStatus.Success)
            {
                using (DataReader valueReader = DataReader.FromBuffer(result.Value))
                {
                    _value = new byte[valueReader.UnconsumedBufferLength];
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
            _currentWriteOperations = new List<IAsyncAction>();
            //_characteristic.ValueChanged += OnCharacteristicValueChanged;
        }

        static Characteristic () {
            TransactionTimeout = 50;
        }

    }
}
