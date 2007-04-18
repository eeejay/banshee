/***************************************************************************
 *  GnomeSettingsDaemon.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 *             Jan Arne Petersen <jap@gnome.org>
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

using Gtk;
using NDesk.DBus;

namespace Banshee.Base
{
    public class GnomeMMKeys : IDisposable
    {
        private const string BusName = "org.gnome.SettingsDaemon";
        private const string ObjectPath = "/org/gnome/SettingsDaemon";
        
        private delegate void MediaPlayerKeyPressedHandler(string application, string key);
        
        [Interface("org.gnome.SettingsDaemon")]
        private interface ISettingsDaemon
        {
            void GrabMediaPlayerKeys(string application, uint time);
            void ReleaseMediaPlayerKeys(string application);
            event MediaPlayerKeyPressedHandler MediaPlayerKeyPressed;
        }
        
        private static GnomeMMKeys instance;

        public static void Initialize()
        {
            if(instance == null) {
                try {
                    instance = new GnomeMMKeys();
                    LogCore.Instance.PushDebug("Enabled multimedia keys support", "Using org.gnome.SettingsDaemon");
                } catch {
                    instance = null;
                }
            }
        }
        
        public static bool IsLoaded {
            get { return instance != null; }
        }
        
        private const string app_name = "Banshee";
        
        private ISettingsDaemon settings_daemon;
        
        public GnomeMMKeys()
        {
            settings_daemon = Bus.Session.GetObject<ISettingsDaemon>(BusName, new ObjectPath(ObjectPath));;

            settings_daemon.GrabMediaPlayerKeys(app_name, 0);
            settings_daemon.MediaPlayerKeyPressed += OnMediaPlayerKeyPressed;
                
            Globals.ShutdownRequested += OnShutdownRequested;

            if(!Globals.UIManager.IsInitialized) {
                Globals.UIManager.Initialized += OnInterfaceInitialized;
            } else {
                OnInterfaceInitialized(null, null);
            }
        }
        
        private bool OnShutdownRequested()
        {
            Dispose();
            return true;
        }
        
        private void OnInterfaceInitialized(object o, EventArgs args)
        {
            if(InterfaceElements.MainWindow != null) {
                InterfaceElements.MainWindow.FocusInEvent += OnFocusInEvent;
            }
        }
        
        public void Dispose()
        {
            if(InterfaceElements.MainWindow != null) {
                InterfaceElements.MainWindow.FocusInEvent -= OnFocusInEvent;
            }
            
            if(settings_daemon == null) {
                return;
            }
            
            settings_daemon.MediaPlayerKeyPressed -= OnMediaPlayerKeyPressed;
            settings_daemon.ReleaseMediaPlayerKeys(app_name);
            settings_daemon = null;
        }
        
        private void OnMediaPlayerKeyPressed(string application, string key)
        {
            if(application != app_name) {
                return;
            }
            
            switch(key) {
                case "Play":
                    Globals.ActionManager["PlayPauseAction"].Activate();
                    break;
                case "Next":
                    Globals.ActionManager["NextAction"].Activate();
                    break;
                case "Previous":
                    Globals.ActionManager["PreviousAction"].Activate();
                    break;
            }
        }
        
        private void OnFocusInEvent(object o, FocusInEventArgs args)
        {
            if(settings_daemon != null) {
                settings_daemon.GrabMediaPlayerKeys(app_name, 0);
            }
        }
    }
}
