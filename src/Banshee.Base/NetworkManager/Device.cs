/***************************************************************************
 *  Device.cs
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
using System.Collections;
using System.Collections.Generic;
using NDesk.DBus;

namespace NetworkManager
{
    public enum DeviceType {
        Unknown,
        Wired,
        Wireless
    }
    
    [Interface("org.freedesktop.NetworkManager.Devices")]
    public interface IDevice
    {
        /* Unsupported methods: 
            
            getProperties
            setLinkActive
            getCapabilities
        */

        string getName();
        uint getMode();
        int getType();
        string getHalUdi();
        uint getIP4Address();
        string getHWAddress();
        bool getLinkActive();
        //INetwork getActiveNetwork();
        ObjectPath getActiveNetwork();
        //INetwork [] getNetworks();
        ObjectPath [] getNetworks();
    }
    
    public class Device : IEnumerable
    {
        private IDevice device;
        
        internal Device(IDevice device)
        {
            this.device = device;
        }

        //TODO: this is a temporary solution
        internal Device(ObjectPath device_path)
        {
            this.device = Manager.GetObject<IDevice> (device_path);
        }
        
        public override string ToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("Name:             " + Name + "\n");
            builder.Append("Type:             " + Type + "\n");
            builder.Append("Mode:             " + Mode + "\n");
            builder.Append("HAL UDI:          " + HalUdi + "\n");
            builder.Append("IP4 Address:      " + IP4Address + "\n");
            builder.Append("Hardware Address: " + HardwareAddress + "\n");
            builder.Append("Link Active:      " + (IsLinkActive ? "Yes" : "No") + "\n");
            builder.Append("Networks: \n");
            
            int i = 0;
            foreach(Network network in Networks) {
                builder.Append("  [" + (i++) + "] Name:      " + network.Name + "\n");
                builder.Append("      Active:    " + (network.Equals(ActiveNetwork) ? "Yes" : "No") + "\n");
                builder.Append("      Strength:  " + network.Strength + "\n");
                builder.Append("      Frequency: " + network.Frequency + "\n");
                builder.Append("      Rate:      " + network.Rate + "\n");
                builder.Append("      Encrypted: " + (network.IsEncrypted ? "Yes" : "No") + "\n");
                builder.Append("      Mode:      " + network.Mode + "\n");
            }
            
            if(i == 0) {
                builder.Append("  (none)\n");
            }
            
            return builder.ToString();
        }
             
        public string Name {
            get {
                return device.getName();
            }
        }
        
        public DeviceType Type {
            get {
                switch(device.getType()) {
                    case 1: return DeviceType.Wired;
                    case 2: return DeviceType.Wireless;
                    default: return DeviceType.Unknown;
                }
            }
        }
        
        public uint Mode {
            get {
                return device.getMode();
            }
        }

        public string HalUdi {
            get {
                return device.getHalUdi();
            }
        }

        public System.Net.IPAddress IP4Address {
            get {
                return new System.Net.IPAddress(device.getIP4Address());
            }
        }
        
        public string HardwareAddress {
            get {
                return device.getHWAddress();
            }
        }
        
        public bool IsLinkActive {
            get {
                return device.getLinkActive();
            }
        }
        
        public Network ActiveNetwork {
            get {
                if(Type != DeviceType.Wireless) {
                    return null;
                }
                
                try {
                    return new Network(device.getActiveNetwork());
                } catch(Exception) {
                    //FIXME: unacceptable silent error handling
                    return null;
                }
            }
        }
        
        public IEnumerator GetEnumerator()
        {
            /*
            foreach(INetwork network in device.getNetworks()) {
                yield return new Network(network);
            }
            */
            foreach(ObjectPath network_path in device.getNetworks()) {
                yield return new Network(network_path);
            }
        }
        
        public Network [] Networks {
            get {
                List<Network> list = new List<Network>();
                
                try {
                    /*
                    foreach(INetwork network in device.getNetworks()) {
                        list.Add(new Network(network));
                    }
                    */
                    foreach(ObjectPath network_path in device.getNetworks()) {
                        list.Add(new Network(network_path));
                    }
                } catch(Exception) {
                }
                
                return list.ToArray();
            }
        }
    }
}


