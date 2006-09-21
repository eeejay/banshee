/***************************************************************************
 *  PowerManagement.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using Mono.Unix;
using DBus;

using Banshee.MediaEngine;
 
namespace Banshee.Base
{
    public delegate void DpmsModeChangedHandler (string mode);
    public delegate void OnAcChangedHandler (bool state);

    [NDesk.DBus.Interface("org.gnome.PowerManager")]
    public interface IPowerManager
    {
        bool Suspend();
        bool Hibernate();
        bool Shutdown();
        bool Reboot();
        bool AllowedSuspend();
        bool AllowedHibernate();
        bool AllowedShutdown();
        bool AllowedReboot();
        //string DpmsMode{get; set;}
        void SetDpmsMode(string mode);
        string GetDpmsMode();
        uint Inhibit(string application, string reason);
        void UnInhibit(uint cookie);
        bool GetOnAc();
        //bool LowPowerMode{get;}
        bool GetLowPowerMode();
        event DpmsModeChangedHandler DpmsModeChanged;
        event OnAcChangedHandler OnAcChanged;
    }

    public static class PowerManagement
    {
        [Interface(GnomePowerManager.INTERFACE_NAME)]
        private abstract class GnomePowerManager : IDisposable
        {
            // http://cvs.gnome.org/viewcvs/*checkout*/gnome-power-manager/docs/dbus-interface.html
        
            internal const string INTERFACE_NAME = "org.gnome.PowerManager";
            internal const string SERVICE_NAME = "org.gnome.PowerManager";
            internal const string PATH_NAME = "/org/gnome/PowerManager";
            
            public EventHandler DpmsModeChanged;
            public EventHandler AcChanged;
            
            private Service service;
            
            public static GnomePowerManager FindInstance()
            {
                Connection connection = Bus.GetSessionBus();
                Service service = Service.Get(connection, SERVICE_NAME);
                GnomePowerManager gpm = (GnomePowerManager)service.GetObject(
                    typeof(GnomePowerManager), PATH_NAME);
                gpm.Service = service;
                return gpm;
            }
            
            public void Dispose()
            {
                System.GC.SuppressFinalize(this);
            }
            
            private void OnSignalCalled(Signal signal)
            {
                if(signal.PathName != PATH_NAME || signal.InterfaceName != INTERFACE_NAME) {
                    return;
                }
                
                switch(signal.Name) {
                    case "DpmsModeChanged": RaiseEvent(DpmsModeChanged); break;
                    case "OnAcChanged": RaiseEvent(AcChanged); break;
                }
            }
            
            private void RaiseEvent(EventHandler eventHandler) 
            {
                EventHandler handler = eventHandler;
                if(handler != null) {
                    handler(this, new EventArgs());
                }
            }
            
            private Service Service {
                get { return service; }
                set {
                    if(service == null) {
                        service = value;
                        service.SignalCalled += OnSignalCalled;
                    }
                }
            }

            [Method] public abstract bool Suspend();
            [Method] public abstract bool Hibernate();
            [Method] public abstract bool Shutdown();
            [Method] public abstract bool Reboot();
            [Method] public abstract bool AllowedSuspend();
            [Method] public abstract bool AllowedHibernate();
            [Method] public abstract bool AllowedShutdown();
            [Method] public abstract bool AllowedReboot();
            [Method] public abstract void SetDpmsMode(string mode);
            [Method] public abstract string GetDpmsMode();
            [Method] public abstract uint Inhibit(string application, string reason);
            [Method] public abstract void UnInhibit(uint cookie);
            [Method] public abstract void GetOnAc();
            [Method] public abstract void GetLowPowerMode();
        }
        
        private static readonly string INHIBIT_PLAY_REASON = Catalog.GetString("Playing Music");
        
        private static Dictionary<string, uint> gpm_inhibit_cookie_map = null;
        private static GnomePowerManager gpm = null;
        
        public static void Initialize()
        {
            try {
                gpm = GnomePowerManager.FindInstance();
            } catch(Exception e) {
                LogError("Cannot find GNOME Power Manager: " + e.Message);
                gpm = null;
                return;
            }
            
            try {
                gpm.GetOnAc();
            } catch(Exception e) {
                LogError("Unsupported version of GNOME Power Manager: " + e.Message);
                gpm.Dispose();
                gpm = null;
                return;
            }
            
            gpm_inhibit_cookie_map = new Dictionary<string, uint>();
            PlayerEngineCore.StateChanged += OnPlayerEngineCoreStateChanged;
        }
        
        public static void Dispose()
        {
            if(gpm != null) {
                UnInhibitAll();
                gpm_inhibit_cookie_map = null;
                gpm.Dispose();
            }
        }
        
        private static void OnPlayerEngineCoreStateChanged(object o, PlayerEngineStateArgs args)
        {
            if(args.State == PlayerEngineState.Playing) {
                Inhibit(INHIBIT_PLAY_REASON);
            } else {
                UnInhibit(INHIBIT_PLAY_REASON);
            }
        }
        
        private static void LogError(string message)
        {
            LogCore.Instance.PushWarning("Power Management Call Failed", message, false);
        }
        
        public static void Inhibit(string reason)
        {
            if(gpm == null || gpm_inhibit_cookie_map.ContainsKey(reason)) {
                return;
            }
            
            try {
                uint cookie = gpm.Inhibit("Banshee", reason);
                gpm_inhibit_cookie_map[reason] = cookie;
            } catch(Exception e) {
                LogError("Inhibit: " + e.Message);
            }
        }
        
        public static void UnInhibit(string reason)
        {
            UnInhibit(reason, true);
        }

        private static void UnInhibit(string reason, bool removeCookie)
        {
            if(gpm == null || !gpm_inhibit_cookie_map.ContainsKey(reason)) {
                return;
            }
            
            try {
                uint cookie = gpm_inhibit_cookie_map[reason];
                gpm.UnInhibit(cookie);
            } catch(Exception e) {
                LogError("UnInhibit: " + e.Message);
            }
            
            if (removeCookie) {
                gpm_inhibit_cookie_map.Remove(reason);
            }
        }

        public static void UnInhibitAll()
        {
            if(gpm == null || gpm_inhibit_cookie_map == null) {
                return;
            }

            foreach(string reason in gpm_inhibit_cookie_map.Keys) {
                UnInhibit(reason, false);
        }

            gpm_inhibit_cookie_map.Clear();
        }

    }
}
 
