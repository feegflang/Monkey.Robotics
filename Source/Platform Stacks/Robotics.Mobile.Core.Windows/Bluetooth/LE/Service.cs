using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Service : IService
    {

        private GattDeviceService _service;

        public event EventHandler CharacteristicsDiscovered;

        public Guid ID
        {
            get { return _service.Uuid; }
        }

        private string _name;
        public string Name
        {
            get
            {
                if (this._name == null)
                    this._name = KnownServices.Lookup(this.ID).Name;
                return this._name;
            }
        }

        private bool _primary;
        public bool IsPrimary
        {
            get { return _primary; }
        }

        private IList<ICharacteristic> _characteristics;
        public IList<ICharacteristic> Characteristics
        {
            get { 
                if (_characteristics != null) return _characteristics;
                return new List<ICharacteristic>();
            }
        }

        public ICharacteristic FindCharacteristic(KnownCharacteristic characteristic)
        {
            LoadCharacteristics();
            return Characteristics.Where((c) => characteristic.ID == c.ID).First();
        }

        private void LoadCharacteristics()
        {
            if (_characteristics == null)
#if WINDOWS_PHONE_APP
                _characteristics = _service.GetAllCharacteristics().Select((c) => new Characteristic(c) as ICharacteristic).ToList();
#else
                // Win 8.1 (desktop/tablet) does not support GetAllCharacteristics, so we'll have to compile a list by querying for each known characteristic type.
                _characteristics = KnownCharacteristics.All()
                    .SelectMany((kc) => _service.GetCharacteristics(kc.ID))
                    .Select((c) => new Characteristic(c))
                    .Cast<ICharacteristic>().ToList();
#endif
        }

        public void DiscoverCharacteristics()
        {
            LoadCharacteristics();
            if (CharacteristicsDiscovered != null)
                CharacteristicsDiscovered(this, EventArgs.Empty);
        }

        /// <summary>
        /// Try to connect to the characteristic by reading it. Returns true if a readable characteristic was found 
        /// and a connection was attempted. A return value of false means a connection was not attempted. A return value
        /// of true does NOT mean that the connection was actually successful.
        /// </summary>
        /// <remarks>
        /// This is what happens when your wireless device API does not have a thing called "ConnectToTheDevice"
        /// </remarks>
        internal async Task<bool> TryAttemptConnect()
        {
            LoadCharacteristics();
            foreach (Characteristic characteristic in _characteristics)
            {
                if (characteristic.CanRead)
                {
                    await characteristic.ForceReadAsync();
                    return true;
                }
                if (characteristic.CanWrite)
                {
                    await characteristic.ForceConnectionWriteAsync();
                    return true;
                }
            }
            return false;
        }

        public Service(GattDeviceService service, bool isPrimary)
        {
            _service = service;
            _primary = isPrimary;
        }

    }
}
