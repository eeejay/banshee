//
// GnomeScreensaverManager.cs
//
// Author:
//   Christopher James Halse Rogers <raof@ubuntu.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using NDesk.DBus;
using Mono.Unix;
using Banshee.NowPlaying;

namespace Banshee.GnomeBackend
{    
    [Interface("org.gnome.ScreenSaver")]
    internal interface IGnomeScreensaver
    {
        uint Inhibit (string application_name, string reason);
        void UnInhibit (uint cookie);
    }

    class GnomeScreensaverManager : IScreensaverManager
    {
        const string DBUS_INTERFACE = "org.gnome.ScreenSaver";
        const string DBUS_PATH = "/org/gnome/ScreenSaver";
        
        IGnomeScreensaver manager;
        uint? cookie;

        public GnomeScreensaverManager ()
        {
            if(!Bus.Session.NameHasOwner (DBUS_INTERFACE)) {
                throw new ApplicationException (String.Format ("Could not find {0}", DBUS_INTERFACE));
            }
            
            manager = Bus.Session.GetObject<IGnomeScreensaver> (DBUS_INTERFACE,
                                                                new ObjectPath (DBUS_PATH));
            
            if (manager == null) {
                throw new ApplicationException (String.Format ("The {0} object could not be located on the DBus interface {1}",
                                                               DBUS_PATH, DBUS_INTERFACE));
            }
        }
        
        public void Inhibit ()
        {
            if (!cookie.HasValue) {
                cookie = manager.Inhibit ("Banshee", 
                                          Catalog.GetString ("Fullscreen video playback active"));
            }
        }

        public void UnInhibit ()
        {
            if (cookie.HasValue) {
                manager.UnInhibit (cookie.Value);
                cookie = null;
            }
        }
    }
}
