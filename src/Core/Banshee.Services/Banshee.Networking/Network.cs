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

using Hyena;

using Banshee.Base;

using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.Configuration;

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

    public class Network : IService, IRegisterOnDemandService, IDisposable
    {
        public event NetworkStateChangedHandler StateChanged;
        
        private NetworkManager nm_manager;
        private State current_state;
        private bool disable_internet_access = false;
        
        public Network ()
        {
            InstallPreferences ();
            
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
        
        public void Dispose ()
        {
            UninstallPreferences ();
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
                    bool was_connected = Connected;
                    current_state = new_state;

                    if (Connected != was_connected) {
                        OnStateChanged ();
                    }
                    
                }
            } catch(Exception) {
            }
        }

        private void OnStateChanged ()
        {
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
        
        public bool Connected {
            get { return disable_internet_access ? false : (nm_manager == null ? true : current_state == State.Connected); }
        }
        
        public NetworkManager Manager {
            get { return nm_manager; }
        }
        
#region Offline Preference

        private PreferenceBase disable_internet_access_preference;

        private void InstallPreferences ()
        {
            disable_internet_access = DisableInternetAccess.Get ();
            
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }
            
            disable_internet_access_preference = service["general"]["misc"].Add (new SchemaPreference<bool> (DisableInternetAccess, 
                Catalog.GetString ("_Disable features requiring Internet access"),
                Catalog.GetString ("Some features require a broadband Internet connection such as Last.fm or cover art fetching"),
                delegate {
                    bool was_connected = Connected;
                    disable_internet_access = DisableInternetAccess.Get ();
                    if (Connected != was_connected) {
                        OnStateChanged ();
                    }
                }
            ));
        }
        
        private void UninstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }
            
            service["general"]["misc"].Remove (disable_internet_access_preference);
            disable_internet_access_preference = null;
        }
        
        public static readonly SchemaEntry<bool> DisableInternetAccess = new SchemaEntry<bool> (
            "core", "disable_internet_access", 
            false,
            "Disable internet access",
            "Do not allow components to have internet access within Banshee"
        );

#endregion

        string IService.ServiceName {
            get { return "Network"; }
        }
    }
}
