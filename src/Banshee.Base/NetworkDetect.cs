
/***************************************************************************
 *  NetworkDetect.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using Mono.Unix;
using NetworkManager;

namespace Banshee.Base
{
    public delegate void NetworkStateChangedHandler(object o, NetworkStateChangedArgs args);
    
    public class NetworkStateChangedArgs : EventArgs
    {
        public bool Connected;
    }

    public class NetworkDetect : IDisposable
    {
        public event NetworkStateChangedHandler StateChanged;
        
        private Manager nm_manager;
        private State last_state;
        
        private static NetworkDetect instance;
        public static NetworkDetect Instance {
            get {
                if(instance == null) {
                    instance = new NetworkDetect();
                }
                
                return instance;
            }
        }
        
        public void Dispose()
        {
            if(nm_manager != null) {
                nm_manager.Dispose();
            }
        }
        
        private NetworkDetect()
        {
            try {
                ConnectToNetworkManager();
            } catch(Exception e) {
                nm_manager = null;
                LogCore.Instance.PushWarning(
                    Catalog.GetString("Cannot connect to NetworkManager"),
                    Catalog.GetString("An available, working network connection will be assumed"),
                    false);
            }
        }

        private void ConnectToNetworkManager()
        {
            nm_manager = new Manager();
            nm_manager.StateChange += OnNetworkManagerEvent;
            nm_manager.DeviceNowActive += OnNetworkManagerEvent;
            nm_manager.DeviceNoLongerActive += OnNetworkManagerEvent;
            last_state = nm_manager.State;
        }
        
        private void OnNetworkManagerEvent(object o, EventArgs args)
        {
            try {
                State new_state = nm_manager.State;
                if(new_state != last_state && (new_state == State.Connected || new_state == State.Disconnected)) {
                    last_state = new_state;
                    
                    NetworkStateChangedHandler handler = StateChanged;
                    if(handler != null) {
                        NetworkStateChangedArgs state_changed_args = new NetworkStateChangedArgs();
                        state_changed_args.Connected = Connected;
                        handler(this, state_changed_args);
                    }
                    
                    Device active_device = nm_manager.ActiveDevice;
                    
                    if(Connected && active_device != null) {
                        LogCore.Instance.PushDebug("Network Connection Established", String.Format("{0} ({1})", 
                            active_device.Name, active_device.IP4Address));
                    } else if(Connected) {
                        LogCore.Instance.PushDebug("Network Connection Established", "Active Device Unknown");
                    } else {
                        LogCore.Instance.PushDebug("Network Connection Unavailable", "Disconnected");
                    }
                }
            } catch(Exception) {
            }
        }
        
        public bool Connected {
            get {
                try {
                    return nm_manager == null ? true : nm_manager.State == State.Connected;
                } catch(Exception) {
                    return true;
                }
            }
        }
        
        public Manager Manager {
            get {
                return nm_manager;
            }
        }
    }
}
