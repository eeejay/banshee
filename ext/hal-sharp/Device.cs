using System;
using System.Collections;
using System.Collections.Generic;

using NDesk.DBus;

namespace Hal
{
    public struct PropertyModification
    {
        public string Key;
        public bool Added;
        public bool Removed;
    }

    internal delegate void DBusPropertyModifiedHandler(int modificationsLength, 
        PropertyModification [] modifications);
    
    [Interface("org.freedesktop.Hal.Device")]
    internal interface IDevice
    {
        // TODO:
        // Need to support the Condition event, but it has a
        // variable number of arguments, not currently supported
        
        event DBusPropertyModifiedHandler PropertyModified;
    
        void SetPropertyString(string key, string value);
        void SetPropertyInteger(string key, int value);
        void SetPropertyBoolean(string key, bool value);
        void SetPropertyDouble(string key, double value);
        void SetPropertyStringList(string key, string [] value);
        
        void SetProperty(string key, ulong value);
        ulong GetProperty(string key); // nasty hack to get around the fact
                                       // that HAL doesn't actually send this
                                       // in a variant, nor does it have a 
                                       // GetPropertyUInt64
                                       // should be object GetProperty(string key)
                                      
        string GetPropertyString(string key);
        int GetPropertyInteger(string key);
        bool GetPropertyBoolean(string key);
        double GetPropertyDouble(string key);
        string [] GetPropertyStringList(string key);
        
        IDictionary<string, object> GetAllProperties();
        void RemoveProperty(string key);
        PropertyType GetPropertyType(string key);
        bool PropertyExists(string key);
        
        void AddCapability(string capability);
        bool QueryCapability(string capability);
        void Lock(string reason);
        void Unlock();
    }
    
    public enum PropertyType
    {
        Invalid = DType.Invalid,
        Int32 = DType.Int32,
        UInt64 = DType.UInt64,
        Double = DType.Double,
        Boolean = DType.Boolean,
        String = DType.String,
        StrList = ((int)(DType.String << 8) + ('l')) 
    }

    public class PropertyModifiedArgs : EventArgs
    {
        private PropertyModification [] modifications;
        
        public PropertyModifiedArgs(PropertyModification [] modifications)
        {
            this.modifications = modifications;
        }
        
        public PropertyModification [] Modifications {
            get { return modifications; }
        }
    }

    public delegate void PropertyModifiedHandler(object o, PropertyModifiedArgs args);

    public class Device : IEnumerable<KeyValuePair<string, object>>, IEqualityComparer<Device>,
        IEquatable<Device>, IComparer<Device>, IComparable<Device>
    {
        private string udi;
        private IDevice device;
        
        public event PropertyModifiedHandler PropertyModified;
        
        public Device(string udi)
        {
            this.udi = udi;
            
            if(!Bus.System.NameHasOwner("org.freedesktop.Hal")) {
                throw new ApplicationException("Could not find org.freedesktop.Hal");
            }
            
            device = Bus.System.GetObject<IDevice>("org.freedesktop.Hal", new ObjectPath(udi));
            device.PropertyModified += OnPropertyModified;
        }
        
        public static Device [] UdisToDevices(string [] udis)
        {
            if(udis == null || udis.Length == 0) {
                return null;
            }
            
            Device [] devices = new Device[udis.Length];
            for(int i = 0; i < udis.Length; i++) {
                devices[i] = new Device(udis[i]);
            }
            
            return devices;
        }
        
        protected virtual void OnPropertyModified(int modificationsLength, PropertyModification [] modifications)
        {
            if(modifications.Length != modificationsLength) {
                throw new ApplicationException("Number of modified properties does not match");
            }
        
            PropertyModifiedHandler handler = PropertyModified;
            if(handler != null) {
                handler(this, new PropertyModifiedArgs(modifications));   
            }
        }
        
        public void Lock(string reason)
        {
            device.Lock(reason);
        }
        
        public void Unlock()
        {
            device.Unlock();
        }

        public string GetPropertyString(string key)
        {
            return device.GetPropertyString(key);
        }

        public int GetPropertyInteger(string key)
        {
            return device.GetPropertyInteger(key);
        }
        
        public ulong GetPropertyUInt64(string key)
        {
            return device.GetProperty(key);
        }

        public double GetPropertyDouble(string key)
        {
            return device.GetPropertyDouble(key);
        }

        public bool GetPropertyBoolean(string key)
        {
            return device.GetPropertyBoolean(key);
        }

        public string [] GetPropertyStringList(string key)
        {
            return device.GetPropertyStringList(key);
        }

        public PropertyType GetPropertyType(string key)
        {
            return PropertyExists(key) ? device.GetPropertyType(key) : PropertyType.Invalid;
        }
        
        public void SetPropertyString(string key, string value)
        {
            device.SetPropertyString(key, value);
        }
        
        public void SetPropertyUInt64(string key, ulong value)
        {
            device.SetProperty(key, value);
        }

        public void SetPropertyInteger(string key, int value)
        {
            device.SetPropertyInteger(key, value);
        }

        public void SetPropertyDouble(string key, double value)
        {
            device.SetPropertyDouble(key, value);
        }

        public void SetPropertyBoolean(string key, bool value)
        {
            device.SetPropertyBoolean(key, value);
        }
        
        public void SetPropertyStringList(string key, string [] value)
        {
            device.SetPropertyStringList(key, value);
        }
        
        public void RemoveProperty(string key)
        {
            device.RemoveProperty(key);
        }
        
        public bool PropertyExists(string key)
        {
            return device.PropertyExists(key);
        }
        
        public void AddCapability(string capability)
        {
            device.AddCapability(capability);
        }
        
        public bool QueryCapability(string capability)
        {
            return device.QueryCapability(capability);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return device.GetAllProperties().GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return device.GetAllProperties().GetEnumerator();
        }
        
        public bool Equals(Device other)
        {
            return Udi.Equals(other.Udi);
        }
        
        public bool Equals(Device a, Device b)
        {
            return a.Udi.Equals(b.Udi);
        }
        
        public int CompareTo(Device other)
        {
            return Udi.CompareTo(other.Udi);
        }
        
        public int Compare(Device a, Device b)
        {
            return a.Udi.CompareTo(b.Udi);
        }
        
        public int GetHashCode(Device a)
        {
            return a.Udi.GetHashCode();
        }
        
        public override int GetHashCode()
        {
            return Udi.GetHashCode();
        }
        
        public override string ToString()
        {
            return udi;
        }
        
        public string this[string property] {
            get { return PropertyExists(property) ? GetPropertyString(property) : null; }
            set { SetPropertyString(property, value); }
        }
        
        public string Udi {
            get { return udi; }
        }
        
        public Device Parent {
            get {
                if(PropertyExists("info.parent")) {
                    return new Device(this["info.parent"]);
                }
                
                return null;
            }
        }
    }
}
