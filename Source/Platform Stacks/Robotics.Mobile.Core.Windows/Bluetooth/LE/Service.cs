using System;
using System.Collections.Generic;
using System.Linq;
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

        // Win 8.1 does not support GetAllCharacteristics, so we'll have to compile a list by querying for each known characteristic type.
        private void LoadCharacteristics()
        {
            if (_characteristics == null)
                _characteristics = KnownCharacteristics.All()
                    .SelectMany((kc) => _service.GetCharacteristics(kc.ID))
                    .Select((c) => new Characteristic(c))
                    .Cast<ICharacteristic>().ToList();
        }

        public void DiscoverCharacteristics()
        {
            LoadCharacteristics();
            if (CharacteristicsDiscovered != null)
                CharacteristicsDiscovered(this, EventArgs.Empty);
        }

        public Service(GattDeviceService service, bool isPrimary)
        {
            _service = service;
            _primary = isPrimary;
        }
    }
}
