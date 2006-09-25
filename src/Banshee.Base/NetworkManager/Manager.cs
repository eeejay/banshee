/***************************************************************************
 *  Manager.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover (aaron@abock.org)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NDesk.DBus;

namespace NetworkManager
{
    public enum State {
        Unknown = 0,
        Asleep,
        Connecting,
        Connected,
        Disconnected
    }
    
    [Interface("org.freedesktop.NetworkManager")]
    public interface IManager
    {
        event DeviceHandler DeviceNoLongerActive;
        event DeviceHandler DeviceNowActive;
        event DeviceHandler DeviceActivating;
        event DeviceHandler DevicesChanged;
        event DeviceActivationFailedHandler DeviceActivationFailed;
        event DeviceStrengthChangedHandler DeviceStrengthChanged;
        
        /* Unsupported methods: 
            
            getDialup
            activateDialup
            setActiveDevice
            createWirelessNetwork
            createTestDevice
            removeTestDevice
        */
    
        ObjectPath [] getDevices();
        uint state();
        void sleep();
        void wake();
        bool getWirelessEnabled();
        void setWirelessEnabled(bool enabled);
        ObjectPath getActiveDevice();
    }
    
    public delegate void DeviceHandler(string device);
    public delegate void DeviceActivationFailedHandler(string device, string essid);
    public delegate void DeviceStrengthChangedHandler(string device, int percent);

    public class Manager : IEnumerable
    {
        private const string BusName = "org.freedesktop.NetworkManager";
        private const string ObjectPath = "/org/freedesktop/NetworkManager";

        private IManager manager;
        
        public event DeviceHandler DeviceNoLongerActive;
        public event DeviceHandler DeviceNowActive;
        public event DeviceHandler DeviceActivating;
        public event DeviceHandler DevicesChanged;
        public event DeviceActivationFailedHandler DeviceActivationFailed;
        public event DeviceStrengthChangedHandler DeviceStrengthChanged;
        
        //TODO: this is a temporary solution
        public static T GetObject<T> (ObjectPath path)
        {
            return DApplication.SystemConnection.GetObject<T>(BusName, path);
        }

        public Manager()
        {
            if(!DApplication.SystemBus.NameHasOwner(BusName)) {
                throw new ApplicationException(String.Format("Name {0} has no owner", BusName));
            }

            manager = DApplication.SystemConnection.GetObject<IManager>(BusName, new ObjectPath(ObjectPath));
            
            manager.DeviceNoLongerActive += delegate(string device) {
                if(DeviceNoLongerActive != null) {
                    DeviceNoLongerActive(device);
                }
            };
            
            manager.DeviceNowActive += delegate(string device) {
                if(DeviceNowActive != null) {
                    DeviceNowActive(device);
                }
            };
            
            manager.DeviceActivating += delegate(string device) {
                if(DeviceActivating != null) {
                    DeviceActivating(device);
                }
            };
            
            manager.DevicesChanged += delegate(string device) {
                if(DevicesChanged != null) {
                    DevicesChanged(device);
                }
            };
            
            manager.DeviceActivationFailed += delegate(string device, string essid) {
                if(DeviceActivationFailed != null) {
                    DeviceActivationFailed(device, essid);
                }
            };
            
            manager.DeviceStrengthChanged += delegate(string device, int percent) {
                if(DeviceStrengthChanged != null) {
                    DeviceStrengthChanged(device, percent);
                }
            };
        }

        public IEnumerator GetEnumerator()
        {
            foreach(ObjectPath device_path in manager.getDevices()) {
                yield return new Device(device_path);
            }
        }
        
        public State State {
            get {
                return (State)manager.state();
            }
        }
        
        public Device [] Devices {
            get {
                List<Device> list = new List<Device>();
                
                foreach(ObjectPath device in manager.getDevices()) {
                    list.Add(new Device(device));
                }
                
                return list.ToArray();
            }
        }
        
        public void Wake()
        {
            manager.wake();
        }
        
        public void Sleep()
        {
            manager.sleep();
        }
        
        public bool WirelessEnabled {
            get {
                return manager.getWirelessEnabled();
            }
            
            set {
                manager.setWirelessEnabled(value);
            }
        }
        
        public Device ActiveDevice {
            get {
                return new Device(manager.getActiveDevice());
            }
        }
    }
}

