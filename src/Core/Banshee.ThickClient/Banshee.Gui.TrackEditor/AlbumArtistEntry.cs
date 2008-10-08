//
// AlbumArtistEntry.cs
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

using Mono.Unix;

using Gtk;

namespace Banshee.Gui.TrackEditor
{
    public class AlbumArtistEntry : VBox, IEditorField
    {
        public event EventHandler Changed;

        private CheckButton enable_compilation = new CheckButton ();
        private TextEntry entry = new TextEntry ("CoreAlbums", "ArtistName");
        private object tooltip_host = Hyena.Gui.TooltipSetter.CreateHost ();
        private Button track_artist_sync_button;
        
        public AlbumArtistEntry (Button trackArtistSyncButton) : base ()
        {
            track_artist_sync_button = trackArtistSyncButton;
        
            enable_compilation.Label = Catalog.GetString ("Compilation Album Artist:");

            Hyena.Gui.TooltipSetter.Set (tooltip_host, enable_compilation,
                Catalog.GetString ("Check this if this track is part of an album with tracks by various artists"));
            Hyena.Gui.TooltipSetter.Set (tooltip_host, entry,
                Catalog.GetString ("This value will affect how this album is sorted; if you enter 'Various Artists' then the album will located with other albums that start with 'V'."));

            PackStart (enable_compilation, false, false, 0);
            PackStart (entry, false, false, 0);
            ShowAll ();

            enable_compilation.Toggled += OnChanged;
            entry.Changed += OnChanged;
            UpdateSensitivities ();
        }

        public Widget LabelWidget {
            get { return enable_compilation; }
        }

        public bool IsCompilation {
            get { return enable_compilation.Active; }
            set {
                enable_compilation.Active = value;
                UpdateSensitivities ();
            }
        }

        public string Text {
            get { return entry.Text; }
            set { entry.Text = value ?? String.Empty; }
        }   

        private void OnChanged (object o, EventArgs args)
        {
            UpdateSensitivities ();
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void UpdateSensitivities ()
        {
            entry.Sensitive = IsCompilation;
            track_artist_sync_button.Sensitive = !IsCompilation;
        }
    }
}
