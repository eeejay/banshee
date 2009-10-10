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
    public class Source : Banshee.Sources.PrimarySource
    {
        private static string name = Catalog.GetString ("Internet Archive");

        private Actions actions;
        private Gtk.Widget header_widget;
        private IA.Search search;
        private string status_text = "";

        public IA.Search Search { get { return search; } }

        public Source () : base (name, name, "internet-archive", 190)
        {
            IA.Search.UserAgent = Banshee.Web.Browser.UserAgent;
            IA.Search.TimeoutMs = 12*1000;

            search = new IA.Search ();

            IsLocal = false;
            // TODO Should probably support normal playlists at some point (but not smart playlists)
            SupportsPlaylists = false;
            //Properties.SetStringList ("Icon.Name", "video-x-generic", "video", "source-library");

            Properties.SetString ("TrackView.ColumnControllerXml", String.Format (@"
                <column-controller>
                  <add-default column=""TitleColumn"" />
                  <add-default column=""ArtistColumn"" />
                  <add-default column=""ComposerColumn"" />
                  <add-default column=""CommentColumn"" />
                  <add-default column=""RatingColumn"" />
                  <add-default column=""YearColumn"" />
                  <add-default column=""PlayCountColumn"" />
                  <add-default column=""MimeTypeColumn"" />
                  <add-default column=""LicenseUriColumn"" />
                  <column modify-default=""TitleColumn"">
                      <width>0.50</width>
                  </column>
                  <column modify-default=""ArtistColumn"">
                    <width>0.25</width>
                    <title>{0}</title><long-title>{0}</long-title>
                  </column>
                  <column modify-default=""PlayCountColumn"">
                    <title>{1}</title><long-title>{1}</long-title>
                  </column>
                  <column modify-default=""RatingColumn""><visible>true</visible></column>
                  <column modify-default=""YearColumn""><visible>true</visible></column>
                  <column modify-default=""PlayCountColumn""><visible>true</visible></column>
                  <column modify-default=""LicenseUriColumn""><visible>true</visible></column>
                  <column modify-default=""CommentColumn"">
                    <title>{2}</title><long-title>{2}</long-title>
                    <visible>false</visible>
                  </column>
                  <column modify-default=""ComposerColumn"">
                    <title>{3}</title><long-title>{3}</long-title>
                    <visible>false</visible>
                  </column>
                </column-controller>",
                Catalog.GetString ("Creator"), Catalog.GetString ("Downloads"),
                Catalog.GetString ("Description"), Catalog.GetString ("Publisher")
            ));

            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            Properties.SetString ("GtkActionPath", "/InternetArchiveContextMenu");

            actions = new Actions (this);

            AfterInitialized ();

            DatabaseTrackModel.ForcedSortQuery = "CoreTracks.TrackID ASC";
            DatabaseTrackModel.CanReorder = false;

            if (header_widget == null) {
                header_widget = new HeaderFilters (this);
                header_widget.ShowAll ();
                Properties.Set<Gtk.Widget> ("Nereid.SourceContents.HeaderWidget", header_widget);
            }

            if (ApplicationContext.CommandLine.Contains ("internet-archive-offline-mode")) {
                AddChildSource (new DetailsSource ("Local H Live at Levis L2 Lazer Rock Stage [Summerfest 1999] on 1999-06-24", "banshee-internet-archive-offline-mode"));
            }
        }

        public override void Reload ()
        {
            ThreadAssist.SpawnFromMain (ThreadedReload);
        }

        private void ThreadedReload ()
        {
            bool success = false;
            int total_results = 0;
            status_text = "";

            ThreadAssist.ProxyToMain (delegate {
                SetStatus (Catalog.GetString ("Searching the Internet Archive"), false, true, "gtk-find");
            });

            IA.SearchResults results = null;

            try {
                results = search.GetResults ();
                total_results = results.TotalResults;
            } catch (System.Net.WebException e) {
                Hyena.Log.Exception ("Error searching the Internet Archive", e);
                results = null;
            } catch (Exception e) {
                Hyena.Log.Exception ("Error searching the Internet Archive", e);
                results = null;
            }

            if (results != null) {
                try {
                    ServiceManager.DbConnection.BeginTransaction ();

                    ServiceManager.DbConnection.Execute ("DELETE FROM CoreTracks WHERE PrimarySourceID = ?", this.DbId);
                    DatabaseTrackModel.Clear ();

                    foreach (var result in results) {
                        var track = new DatabaseTrackInfo () {
                            PrimarySource = this,
                            ArtistName = result.Creator ?? "",
                            Comment    = Hyena.StringUtil.RemoveHtml (result.Description),
                            Composer   = result.Publisher ?? "",
                            LicenseUri = result.LicenseUrl,
                            PlayCount  = result.Downloads,
                            Rating     = (int) Math.Round (result.AvgRating),
                            TrackTitle = result.Title,
                            Uri        = new Banshee.Base.SafeUri (result.WebpageUrl),
                            MimeType   = result.Format,
                            Year       = result.Year
                        };

                        // HACK to remove ugly empty description
                        if (track.Comment == "There is currently no description for this item.")
                            track.Comment = null;

                        track.Save (false);
                    }

                    ServiceManager.DbConnection.CommitTransaction ();
                    success = true;
                } catch (Exception e) {
                    ServiceManager.DbConnection.RollbackTransaction ();
                    Hyena.Log.Exception ("Error searching the Internet Archive", e);
                }
            }

            if (success) {
                base.Reload ();

                int count = DatabaseTrackModel.Count;
                if (total_results == 0) {
                    ThreadAssist.ProxyToMain (delegate {
                        SetStatus (Catalog.GetString ("No matches. Try another search?"), false, false, "gtk-info");
                    });
                } else {
                    ThreadAssist.ProxyToMain (ClearMessages);
                    status_text = String.Format (Catalog.GetPluralString (
                        "Showing 1 match", "Showing 1 to {0:N0} of {1:N0} total matches", total_results),
                        count, total_results
                    );
                }
            } else {
                // TODO differentiate between various errors types (network, invalid search, etc)
                //if (error.Status == WebExceptionStatus.Timeout) 
                ThreadAssist.ProxyToMain (delegate {
                    SetStatus (Catalog.GetString ("Error searching the Internet Archive"), true);
                });
            }
        }

        public override int Count {
            get { return 0; }
        }

        protected override int StatusFormatsCount { get { return 1; } }

        public override string GetStatusText ()
        {
            return status_text;
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

        public override bool HasViewableTrackProperties {
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

        public override void Dispose ()
        {
            if (actions != null) {
                actions.Dispose ();
            }

            base.Dispose ();

            //UninstallPreferences ();
        }
    }
}
