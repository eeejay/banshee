//
// DefaultApplicationHelper.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections.Generic;

using Hyena;

using Banshee.Configuration;

namespace Banshee.GnomeBackend
{
    public class DefaultApplicationHelper : IDefaultHelper
    {
        private static string [] uri_schemes = new string [] {
            "feed", "itpc", "lastfm", "mms", "mmsh",
            // "cdda",  Not needed because taken care of by setting ourselves as the autoplay_cda_command
            // Schemes handled by Totem or RB, need testing to ensure we handle
            // "icy", "icyx",        // ice/shoutcast
            // "itms",               // iTunes Music Store
            // "net", "pnm", "uvox", // unknown
            // "rtp", "rtsp",
        };

        private List<IDefaultSchema> schemas = new List<IDefaultSchema> ();
        private bool? is_default;
        private string banshee_cmd;

        #region Public API

        public DefaultApplicationHelper ()
        {
            banshee_cmd = Banshee.ServiceStack.Application.InternalName;
            Add ("/desktop/gnome/applications/media", "needs_term", false);
            Add ("/desktop/gnome/applications/media", "exec", "{0}");

            Add ("/desktop/gnome/volume_manager", "autoipod", true);
            Add ("/desktop/gnome/volume_manager", "autoipod_command", "{0}");

            Add ("/desktop/gnome/volume_manager", "autoplay_cda", true);
            Add ("/desktop/gnome/volume_manager", "autoplay_cda_command", "{0} --device=%d");

            //Add ("/desktop/gnome/volume_manager", "autoplay_dvd", true);
            //Add ("/desktop/gnome/volume_manager", "autoplay_dvd_command", "{0} --device=%d");

            //Add ("/desktop/gnome/volume_manager", "autoplay_vcd", true);
            //Add ("/desktop/gnome/volume_manager", "autoplay_vcd_command", "{0} --device=%d");

            foreach (string uri_scheme in uri_schemes) {
                string ns = String.Format ("/desktop/gnome/url-handlers/{0}", uri_scheme);
                Add (ns, "command", "{0} \"%s\"");
                Add (ns, "enabled", true);
                Add (ns, "needs_terminal", false);
            }

            // TODO set us as handler in Firefox?
            // browser.audioFeeds.handler.default = client
            // browser.audioFeeds.handlers.application = /usr/local/bin/banshee-1
            //
            // browser.videoFeeds.handler.default = client
            // browser.videoFeeds.handlers.application = /usr/local/bin/banshee-1
        }

        public bool IsDefault {
            get {
                if (is_default == null) {
                    is_default = true;
                    foreach (IDefaultSchema schema in schemas) {
                        is_default &= schema.IsDefault;
                    }
                }
                return is_default.Value;
            }
        }

        public void MakeDefault ()
        {
            Log.InformationFormat ("Setting Banshee as the default media application and handler for media urls etc in GNOME");
            foreach (IDefaultSchema schema in schemas) {
                schema.MakeDefault ();
            }
        }

        #endregion

        #region Private helpers

        private void Add (string ns, string key, string val)
        {
            schemas.Add (new SchemaMap<string> (ns, key, String.Format (val, banshee_cmd)));
        }

        private void Add (string ns, string key, bool val)
        {
            schemas.Add (new SchemaMap<bool> (ns, key, val));
        }

        private interface IDefaultSchema
        {
            bool IsDefault { get; }
            void MakeDefault ();
        }

        private class SchemaMap<T> : IDefaultSchema
        {
            private SchemaEntry<T> schema;
            private T val;

            public SchemaMap (string ns, string key, T val)
            {
                schema = new SchemaEntry<T> (ns, key, default (T), null, null);
                this.val = val;
            }

            public bool IsDefault {
                get {
                    T cur_val = schema.Get ();
                    return cur_val != null && cur_val.Equals (val);
                }
            }

            public void MakeDefault ()
            {
                if (!IsDefault) {
                    schema.Set (val);
                }
            }

            public override string ToString ()
            {
                return schema.Namespace + schema.Key;
            }
        }

        #endregion
    }
}
