//
// Source.cs
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
using Hyena.Data.Sqlite;

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
    public class Source : Banshee.Sources.PrimarySource
    {
        private static string name = Catalog.GetString ("Internet Archive");

        private Gtk.Widget header_widget;
        private IA.Search search;

        public IA.Search Search { get { return search; } }

        public Source () : base (name, name, "internet-archive", 210)
        {
            search = new IA.Search () {
                Format = IA.ResultsFormat.Json
            };

            IsLocal = false;
            // TODO Should probably support normal playlists at some point (but not smart playlists)
            SupportsPlaylists = false;
            //Properties.SetStringList ("Icon.Name", "video-x-generic", "video", "source-library");

            Properties.SetString ("TrackView.ColumnControllerXml", String.Format (@"
                <column-controller>
                  <add-default column=""IndicatorColumn"" visible=""true"" />
                  <add-default column=""TitleColumn"" visible=""true"" />
                  <add-default column=""ArtistColumn"" visible=""true"" />
                  <add-default column=""CommentColumn"" visible=""true"" />
                  <add-default column=""RatingColumn"" />
                  <add-default column=""DurationColumn"" visible=""true"" />
                  <add-default column=""GenreColumn"" />
                  <add-default column=""YearColumn"" />
                  <add-default column=""FileSizeColumn"" visible=""true"" />
                  <add-default column=""PlayCountColumn"" visible=""true"" />
                  <add-default column=""MimeTypeColumn"" visible=""true"" />
                  <add-default column=""LicenseUriColumn"" />
                  <column modify-default=""ArtistColumn"">
                    <title>{0}</title><long-title>{0}</long-title>
                  </column>
                  <column modify-default=""PlayCountColumn"">
                    <title>{1}</title><long-title>{1}</long-title>
                  </column>
                  <column modify-default=""CommentColumn"">
                    <title>{2}</title><long-title>{2}</long-title>
                  </column>
                </column-controller>",
                Catalog.GetString ("Creator"), Catalog.GetString ("Downloads"), Catalog.GetString ("Description")
            ));

            AfterInitialized ();

            DatabaseTrackModel.ForcedSortQuery = "CoreTracks.TrackID ASC";
            DatabaseTrackModel.CanReorder = false;

            if (header_widget == null) {
                header_widget = new HeaderFilters (this);
                header_widget.ShowAll ();
                Properties.Set<Gtk.Widget> ("Nereid.SourceContents.HeaderWidget", header_widget);
            }
        }

        public override void Activate ()
        {
            base.Activate ();
        }

        public override void Reload ()
        {
            bool success = false;

            try {
                var results = new IA.JsonResults (search.GetResults ());

                // TODO set the status bar w/ this
                Console.WriteLine ("Have {0} total results, {1} current rows, and {2} is the offset", results.TotalResults, results.Count, results.Offset);

                RemoveTrackRange (DatabaseTrackModel, new RangeCollection.Range (0, Count - 1));
                DatabaseTrackModel.Clear ();

                foreach (var item in results.Items) {
                    var track = new DatabaseTrackInfo () {
                        PrimarySource = this,
                        ArtistName = item.Get (IA.Field.Creator) ?? Catalog.GetString ("Unknown"),
                        TrackTitle = item.Get (IA.Field.Title),
                        Comment    = Hyena.StringUtil.RemoveHtml (item.Get (IA.Field.Description))
                    };

                    track.Save (false);
                }

                base.Reload ();
                success = true;
            } catch (Exception e) {
                Hyena.Log.Exception ("Error refreshing internet archive", e);
            }

            if (!success) {
                // TODO differentiate between various errors types (network, invalid search, etc)
                SetStatus (Catalog.GetString ("Error refreshing Internet Archive"), true);
            }
        }

        /*public override bool AcceptsInputFromSource (Source source)
        {
            return false;
        }*/

        // DatabaseSource overrides

        public override bool ShowBrowser { 
            get { return false; }
        }

        public override bool CanShuffle {
            get { return false; }
        }

        public override bool CanAddTracks {
            get { return false; }
        }

        public override bool CanRemoveTracks {
            get { return false; }
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        protected override bool HasArtistAlbum {
            get { return false; }
        }

        public override bool HasEditableTrackProperties {
            get { return false; }
        }

        public override bool CanSearch {
            get { return false; }
        }

        protected override void Initialize ()
        {
            base.Initialize ();

            //InstallPreferences ();
        }

        /*public void Dispose ()
        {
            if (actions != null) {
                actions.Dispose ();
            }

            UninstallPreferences ();
        }*/
    }
}
