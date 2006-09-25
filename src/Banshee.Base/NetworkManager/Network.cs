/***************************************************************************
 *  Network.cs
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
using NDesk.DBus;

namespace NetworkManager
{
    [Interface("org.freedesktop.NetworkManager.Devices")]
    public interface INetwork
    {
        /* Unsupported methods: 
            
            getProperties
        */

        string getName();
        //string getAddress(); Calling this crashes NM for me
        int getStrength();
        double getFrequency();
        int getRate();
        bool getEncrypted();
        uint getMode();
    }
    
    public class Network
    {
        private INetwork network;
        
        internal Network(INetwork network)
        {
            this.network = network;
        }

        //TODO: this is a temporary solution
        internal Network(ObjectPath network_path)
        {
            this.network = Manager.GetObject<INetwork> (network_path);
        }
        
        public string Name {
            get {
                return network.getName();
            }
        }
        
        /*public string Address {
            get {
                return network.getAddress();
            }
        }*/
        
        public int Strength {
            get {
                return network.getStrength();
            }
        }
        
        public double Frequency {
            get {
                return network.getFrequency();
            }
        }
        
        public int Rate {
            get {
                return network.getRate();
            }
        }
        
        public bool IsEncrypted {
            get {
                return network.getEncrypted();
            }
        }
        
        public uint Mode {
            get {
                return network.getMode();
            }
        }

        public override bool Equals(object o)
        {
            Network compare = o as Network;
            if(compare == null) {
                return false;
            }
            
            return Name == compare.Name;
        }
        
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
