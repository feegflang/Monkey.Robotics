﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robotics.Mobile.Core.Utils;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	// Source: https://developer.bluetooth.org/gatt/services/Pages/ServicesHome.aspx
	public static class KnownServices
	{
		private static Dictionary<Guid, KnownService> _items;
		private static object _lock = new object();

		static KnownServices ()
		{

		}

		public static KnownService Lookup(Guid id)
		{
			lock (_lock) {
				if (_items == null)
					LoadItemsFromJson ();
			}

			if (_items.ContainsKey (id))
				return _items [id];
			else
				return new KnownService { Name = "Unknown", ID = Guid.Empty };

		}

        public static IEnumerable<KnownService> All()
        {
            lock (_lock)
            {
                if (_items == null)
                    LoadItemsFromJson();
            }
            return _items.Values;
        }

        public static void Add(KnownService service)
        {
            if (service.ID == Guid.Empty) throw new ArgumentException("service must have a valid GUID", "service");
            lock (_lock)
            {
                if (_items == null)
                    LoadItemsFromJson();
                _items[service.ID] = service;
            }

        }

		public static void LoadItemsFromJson()
		{
			_items = new Dictionary<Guid, KnownService> ();
			//TODO: switch over to ServiceStack.Text when it gets bound.
			KnownService service;
			string itemsJson = ResourceLoader.GetEmbeddedResourceString (typeof(KnownServices).GetTypeInfo ().Assembly, "KnownServices.json");
			var json = JValue.Parse (itemsJson);
			foreach (var item in json.Children() ) {
				JProperty prop = item as JProperty;
				service = new KnownService () { Name = prop.Value.ToString(), ID = Guid.ParseExact (prop.Name, "d") };
				_items.Add (service.ID, service);
			}
		}

	}

	public struct KnownService
	{
		public string Name;
		public Guid ID;
	}

}

