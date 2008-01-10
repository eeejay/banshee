//
// NetworkDetect.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using Mono.Unix;
using Banshee.Base;

namespace Banshee.Networking
{
    public delegate void NetworkStateChangedHandler(object o, NetworkStateChangedArgs args);
    
    public class NetworkStateChangedArgs : EventArgs
    {
        public bool Connected;
    }

    public class NetworkUnavailableException : ApplicationException
    {
        public NetworkUnavailableException() : base(Catalog.GetString("There is no available network connection"))
        {
        }

        public NetworkUnavailableException(string message) : base(message)
        {
        }
    }

    public class NetworkDetect
    {
        public event NetworkStateChangedHandler StateChanged;
        
        private NetworkManager nm_manager;
        private State current_state;
        
        private static NetworkDetect instance;
        public static NetworkDetect Instance {
            get {
                if(instance == null) {
                    instance = new NetworkDetect();
                }
                
                return instance;
            }
        }
        
        private NetworkDetect()
        {
            try {
                ConnectToNetworkManager();
            } catch(Exception) {
                nm_manager = null;
                Log.Warning(
                    Catalog.GetString("Cannot connect to NetworkManager"),
                    Catalog.GetString("An available, working network connection will be assumed"),
                    false);
            }
        }

        private void ConnectToNetworkManager()
        {
            nm_manager = new NetworkManager();
            nm_manager.StateChange += OnNetworkManagerEvent;
            current_state = nm_manager.State;
        }
        
        private void OnNetworkManagerEvent(State new_state)
        {
            try {
                if(new_state != current_state && (new_state == State.Connected || new_state == State.Disconnected)) {
                    current_state = new_state;
                    
                    NetworkStateChangedHandler handler = StateChanged;
                    if(handler != null) {
                        NetworkStateChangedArgs state_changed_args = new NetworkStateChangedArgs();
                        state_changed_args.Connected = Connected;
                        handler(this, state_changed_args);
                    }
                    
                    if(Connected) {
                        Log.Debug("Network Connection Established", "Connected");
                    } else {
                        Log.Debug("Network Connection Unavailable", "Disconnected");
                    }
                }
            } catch(Exception) {
            }
        }
        
        public bool Connected {
            get { return nm_manager == null ? true : current_state == State.Connected; }
        }
        
        public NetworkManager Manager {
            get { return nm_manager; }
        }
    }
}
