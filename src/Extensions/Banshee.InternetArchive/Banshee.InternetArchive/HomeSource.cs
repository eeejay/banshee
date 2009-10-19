//
// HomeSource.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;

using Mono.Unix;

using Hyena.Collections;
using Hyena.Data;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.Database;
using Banshee.Gui;
using Banshee.Library;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Playlist;
using Banshee.Preferences;
using Banshee.ServiceStack;
using Banshee.Sources;

using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class HomeSource : Banshee.Sources.Source, IDisposable
    {
        private static string name = Catalog.GetString ("Internet Archive");
        private SearchSource search_source;
        private Actions actions;

        public SearchSource SearchSource {
            get { return search_source; }
        }

        public HomeSource () : base (name, name, 190, "internet-archive")
        {
            InstallPreferences ();

            //Properties.SetStringList ("Icon.Name", "video-x-generic", "video", "source-library");
            Properties.SetString ("ActiveSourceUIResource", "HomeSourceActiveUI.xml");
            Properties.SetString ("GtkActionPath", "/IaHomeSourcePopup");
            Properties.Set<Gtk.Widget> ("Nereid.SourceContents", new HomeView (this));

            actions = new Actions (this);

            foreach (var item in Item.LoadAll ()) {
                AddChildSource (new DetailsSource (item));
            }
        }

        public void SetSearch (SearchDescription search)
        {
            if (search_source == null) {
                search_source = new SearchSource ();
                AddChildSource (search_source);
            }

            SearchSource.SetSearch (search);
        }

        public override int Count {
            get { return 0; }
        }

        public void Dispose ()
        {
            UninstallPreferences ();

            if (actions != null) {
                actions.Dispose ();
            }
        }

#region Preferences

        private SourcePage pref_page;
        private Section pref_section;

        private void InstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }

            pref_page = new Banshee.Preferences.SourcePage (this);

            pref_section = pref_page.Add (new Section ("mediatypes", Catalog.GetString ("Preferred Media Types"), 20));

            pref_section.Add (new SchemaPreference<string> (AudioTypes,
                Catalog.GetString ("_Audio"), Catalog.GetString ("")));

            pref_section.Add (new SchemaPreference<string> (VideoTypes,
                Catalog.GetString ("_Video"), Catalog.GetString ("")));

            pref_section.Add (new SchemaPreference<string> (TextTypes,
                Catalog.GetString ("_Text"), Catalog.GetString ("")));
        }

        private void UninstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null || pref_page == null) {
                return;
            }

            pref_page.Dispose ();
            pref_page = null;
            pref_section = null;
        }

        public override string PreferencesPageId {
            get { return pref_page.Id; }
        }

        public static readonly SchemaEntry<string> AudioTypes = new SchemaEntry<string> (
            "plugins.internetarchive", "audio_types",
            "Audio, VBR Mp3, Ogg Vorbis, 128Kbps MP3,  64Kbps MP3, Flac, VBR ZIP, 64Kbps MP3 ZIP",
            "Ordered list of preferred mediatypes for audio items", null);

        public static readonly SchemaEntry<string> VideoTypes = new SchemaEntry<string> (
            "plugins.internetarchive", "video_types",
            "Ogg Video, 512Kb MPEG4, MPEG2, h.264 MPEG4, DivX, Quicktime, MPEG1",
            "Ordered list of preferred mediatypes for video items", null);

        public static readonly SchemaEntry<string> TextTypes = new SchemaEntry<string> (
            "plugins.internetarchive", "text_types",
            "Text PDF, Standard LuraTech PDF, Grayscale LuraTech PDF, ZIP, Text, Hypertext",
            "Ordered list of preferred mediatypes for text items", null);

#endregion
    }
}
