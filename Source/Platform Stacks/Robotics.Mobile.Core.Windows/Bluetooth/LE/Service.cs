
using Robotics.Mobile.Core.Bluetooth.LE;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
    class Service : IService
    {
        public event System.EventHandler CharacteristicsDiscovered;

        public System.Guid ID
        {
            get { throw new System.NotImplementedException(); }
        }

        public string Name
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsPrimary
        {
            get { throw new System.NotImplementedException(); }
        }

        public System.Collections.Generic.IList<ICharacteristic> Characteristics
        {
            get { throw new System.NotImplementedException(); }
        }

        public ICharacteristic FindCharacteristic(KnownCharacteristic characteristic)
        {
            throw new System.NotImplementedException();
        }

        public void DiscoverCharacteristics()
        {
            throw new System.NotImplementedException();
        }
    }
}
