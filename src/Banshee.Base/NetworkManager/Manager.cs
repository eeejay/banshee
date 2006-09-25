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
        /* Unsupported methods: 
            
            getDialup
            activateDialup
            setActiveDevice
            createWirelessNetwork
            createTestDevice
            removeTestDevice
        */
    
        //IDevice [] getDevices();
        ObjectPath [] getDevices();
        uint state();
        void sleep();
        void wake();
        bool getWirelessEnabled();
        void setWirelessEnabled(bool enabled);
        //IDevice getActiveDevice();
        ObjectPath getActiveDevice();
    }

    //public class Manager : IEnumerable<Device>
    public class Manager : IEnumerable
    {
        private const string BusName = "org.freedesktop.NetworkManager";
        private const string ObjectPath = "/org/freedesktop/NetworkManager";

        private IManager manager;
        
        //FIXME: support signals again with managed dbus
#pragma warning disable 0067
        public event EventHandler DeviceNoLongerActive;
        public event EventHandler DeviceNowActive;
        public event EventHandler DeviceActivating;
        public event EventHandler DevicesChanged;
        public event EventHandler DeviceActivationStage;
        public event EventHandler DeviceIP4AddressChange;
        public event EventHandler StateChange;
        public event EventHandler WirelessNetworkDisappeared;
        public event EventHandler WirelessNetworkAppeared;
        public event EventHandler WirelessNetworkStrengthChanged;
#pragma warning restore 0067
        
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
        }
        
        private void InvokeEvent(string nmSignalName)
        {
            /*
               Ughh, this would be much nicer if it actually worked using reflection.
               But noooo, EventInfo.GetRaiseMethod *always* returns null, so 
               I can't invoke the registered raise method through reflection, which
               leaves me to do this lame switch/case hard coded event raising...
            */
            
            switch(nmSignalName) {
                case "DeviceNoLongerActive": InvokeEvent(DeviceNoLongerActive); break;
                case "DeviceNowActive": InvokeEvent(DeviceNowActive); break;
                case "DeviceActivating": InvokeEvent(DeviceActivating); break;
                case "DeviceActivationStage": InvokeEvent(DeviceActivationStage); break;
                case "DevicesChanged": InvokeEvent(DevicesChanged); break;
                case "DeviceIP4AddressChange": InvokeEvent(DeviceIP4AddressChange); break;
                case "WirelessNetworkDisappeared": InvokeEvent(WirelessNetworkDisappeared); break;
                case "WirelessNetworkAppeared": InvokeEvent(WirelessNetworkAppeared); break;
                case "WirelessNetworkStrengthChanged": InvokeEvent(WirelessNetworkStrengthChanged); break;
                case "StateChange": InvokeEvent(StateChange); break;
            }
            
            /*EventInfo event_info = GetType().GetEvent(nmSignalName);
            if(event_info == null) {
               return;
            }
            
            MethodInfo method = event_info.GetRaiseMethod(true);
            if(method != null) {
                method.Invoke(this, new object [] { this, new EventArgs() });
            }*/
        }
        
        private void InvokeEvent(EventHandler eventHandler)
        {
            EventHandler handler = eventHandler;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        

        //public IEnumerator<Device> GetEnumerator()
        public IEnumerator GetEnumerator()
        {
            /*
            foreach(IDevice device in manager.getDevices()) {
                yield return new Device(device);
            }
            */
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
                
                /*
                foreach(IDevice device in manager.getDevices()) {
                    list.Add(new Device(device));
                }
                */
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
                /*
                foreach(Device device in this) {
                    if(device.IsLinkActive) {
                        return device;
                    }
                }
                
                return null;
                */

                return new Device(manager.getActiveDevice());
            }
        }
    }
}

