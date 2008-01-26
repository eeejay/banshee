//
// MultimediaKeyService.cs
//
// Authors:
//   Alexander Hixon <hixon.alexander@mediati.org>
//   Aaron Bockover <aaron@abock.org>
//   Jan Arne Petersen <jap@gnome.org>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using Mono.Unix;

using Banshee.ServiceStack;
using Banshee.Configuration;
using NDesk.DBus;

namespace Banshee.MultimediaKeys
{
    public class MultimediaKeysService : IService, IDisposable
    {
        private const string BusName = "org.gnome.SettingsDaemon";
        private const string ObjectPath = "/org/gnome/SettingsDaemon";
        private ISettingsDaemon settings_daemon;
        
        private delegate void MediaPlayerKeyPressedHandler (string application, string key);
        
        [Interface("org.gnome.SettingsDaemon")]
        private interface ISettingsDaemon
        {
            void GrabMediaPlayerKeys (string application, uint time);
            void ReleaseMediaPlayerKeys (string application);
            event MediaPlayerKeyPressedHandler MediaPlayerKeyPressed;
        }
                
        private const string app_name = "Banshee";
    
        public MultimediaKeysService ()
        {
            Initialize ();
        }
        
        private void Initialize ()
        {
            settings_daemon = Bus.Session.GetObject<ISettingsDaemon> (BusName, new ObjectPath (ObjectPath));
            settings_daemon.GrabMediaPlayerKeys (app_name, 0);
            settings_daemon.MediaPlayerKeyPressed += OnMediaPlayerKeyPressed;
        }
  
        public void Dispose()
        {
            if (settings_daemon == null) {
                return;
            }
            
            settings_daemon.MediaPlayerKeyPressed -= OnMediaPlayerKeyPressed;
            settings_daemon.ReleaseMediaPlayerKeys (app_name);
            settings_daemon = null;
        }
        
        private void OnMediaPlayerKeyPressed (string application, string key)
        {
            if (application != app_name) {
                return;
            }
            
            switch (key) {
                case "Play":
                    ServiceManager.PlayerEngine.TogglePlaying ();
                    break;
                case "Next":
                    ServiceManager.PlaybackController.Next ();
                    break;
                case "Previous":
                    ServiceManager.PlaybackController.Previous ();
                    break;
                case "Stop":
                    ServiceManager.PlayerEngine.Close ();
                    break;
            }
        }
        
        string IService.ServiceName {
            get { return "MultimediaKeysService"; }
        }
    }
}
