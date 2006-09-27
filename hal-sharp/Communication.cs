using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace Hal
{
    public static class Communication
    {
        private static Connection connection;
        private static Bus bus;
        
        public static Connection Connection {
            get { 
                if(connection == null) {
                    connection = Connection.Open(Address.SystemBus);
                }
            
                return connection;
            }
        
            set { connection = value; }
        }

        public static Bus Bus {
            get {
                if(bus == null) {
                    bus = Connection.GetObject<Bus>("org.freedesktop.DBus", 
                        new ObjectPath("/org/freedesktop/DBus"));
                    bus.Hello();
                    bus.NameAcquired += OnNameAcquired;
                } 
                
                return bus;
            }
            
            set { bus = value; }
        }
        
        private static void OnNameAcquired(string name)
        {
        }
    }
}
