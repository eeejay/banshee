//
// LastfmActions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections.Generic;
using Gtk;

using Mono.Unix;

using Lastfm;
using Lastfm.Gui;
using SortType = Hyena.Data.SortType;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Widgets;
using Banshee.MediaEngine;
using Banshee.Database;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.Collection;

using Browser = Banshee.Web.Browser;

namespace Banshee.Lastfm.Radio
{
    public class LastfmActions : BansheeActionGroup
    {
        private LastfmSource lastfm;

        private InterfaceActionService action_service;
        private uint actions_id;
        private ActionGroup actions;

        public LastfmActions (LastfmSource lastfm) : base ("Lastfm")
        {
            action_service = ServiceManager.Get<InterfaceActionService> ();
            this.lastfm = lastfm;
            
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;

            AddImportant (
                new ActionEntry (
                    "LastfmAddAction", Stock.Add,
                     Catalog.GetString ("_Add Station"),
                     null, Catalog.GetString ("Add a new Last.fm radio station"), OnAddStation
                )
            );

            Add (new ActionEntry [] {
                new ActionEntry (
                    "LastfmConnectAction", null,
                     Catalog.GetString ("Connect"),
                     null, String.Empty, OnConnect
                ),
                new ActionEntry (
                    "LastfmSortAction", "gtk-sort-descending",
                    Catalog.GetString ("Sort Stations by"),
                    null, String.Empty, null
                )
            });

            // Artist actions
            Add (new ActionEntry [] {
                new ActionEntry ("LastfmArtistVisitLastfmAction", "audioscrobbler",
                    Catalog.GetString ("View on Last.fm"), null,
                    Catalog.GetString ("View this artist's Last.fm page"), OnArtistVisitLastfm),

                new ActionEntry ("LastfmArtistVisitWikipediaAction", "",
                    Catalog.GetString ("View Artist on Wikipedia"), null,
                    Catalog.GetString ("Find this artist on Wikipedia"), OnArtistVisitWikipedia),

                /*new ActionEntry ("LastfmArtistVisitAmazonAction", "",
                    Catalog.GetString ("View Artist on Amazon"), null,
                    Catalog.GetString ("Find this artist on Amazon"), OnArtistVisitAmazon),*/

                new ActionEntry ("LastfmArtistViewVideosAction", "",
                    Catalog.GetString ("View Artist's Videos"), null,
                    Catalog.GetString ("Find videos by this artist"), OnArtistViewVideos),

                new ActionEntry ("LastfmArtistPlayFanRadioAction", StationType.Fan.IconName,
                    Catalog.GetString ("Listen to 'fans of' Station"), null,
                    Catalog.GetString ("Listen to the Last.fm 'fans of' station for this artist"), OnArtistPlayFanRadio),

                new ActionEntry ("LastfmArtistPlaySimilarRadioAction", StationType.Similar.IconName,
                    Catalog.GetString ("Listen to 'similar to' Station"), null,
                    Catalog.GetString ("Listen to the Last.fm 'similar to' station for this artist"), OnArtistPlaySimilarRadio),

                new ActionEntry ("LastfmArtistRecommendAction", "",
                    Catalog.GetString ("Recommend to"), null,
                    Catalog.GetString ("Recommend this artist to someone"), OnArtistRecommend)

            });

            // Album actions
            Add (new ActionEntry [] {
                new ActionEntry ("LastfmAlbumVisitLastfmAction", "audioscrobbler.png",
                    Catalog.GetString ("View on Last.fm"), null,
                    Catalog.GetString ("View this album's Last.fm page"), OnAlbumVisitLastfm),

                /*new ActionEntry ("LastfmAlbumVisitAmazonAction", "",
                    Catalog.GetString ("View Album on Amazon"), null,
                    Catalog.GetString ("Find this album on Amazon"), OnAlbumVisitAmazon),*/

                new ActionEntry ("LastfmAlbumRecommendAction", "",
                    Catalog.GetString ("Recommend to"), null,
                    Catalog.GetString ("Recommend this album to someone"), OnAlbumRecommend)
            });

            // Track actions
            Add (new ActionEntry [] {
                new ActionEntry (
                    "LastfmLoveAction", "face-smile",
                    Catalog.GetString ("Love Track"), null,
                    Catalog.GetString ("Mark current track as loved"), OnLoved),

                new ActionEntry (
                    "LastfmHateAction", "face-sad",
                    Catalog.GetString ("Ban Track"), null,
                    Catalog.GetString ("Mark current track as banned"), OnHated),

                new ActionEntry ("LastfmTrackVisitLastfmAction", "audioscrobbler",
                    Catalog.GetString ("View on Last.fm"), null,
                    Catalog.GetString ("View this track's Last.fm page"), OnTrackVisitLastfm),

                new ActionEntry ("LastfmTrackRecommendAction", "",
                    Catalog.GetString ("Recommend to"), null,
                    Catalog.GetString ("Recommend this track to someone"), OnTrackRecommend)
            });

            Add (
                new RadioActionEntry [] {
                    new RadioActionEntry (
                        "LastfmSortStationsByNameAction", null,
                         Catalog.GetString ("Station Name"),
                         null, "", 0
                    ),
                    new RadioActionEntry (
                        "LastfmSortStationsByPlayCountAction", null,
                         Catalog.GetString ("Total Play Count"),
                         null, "", 1
                    ),
                    new RadioActionEntry (
                        "LastfmSortStationsByTypeAction", null,
                         Catalog.GetString ("Station Type"),
                         null, "", 2
                    )
                },
                Array.IndexOf (LastfmSource.ChildComparers, lastfm.ChildComparer),
                delegate (object sender, ChangedArgs args) {
                    lastfm.ChildComparer = LastfmSource.ChildComparers[args.Current.Value];
                    lastfm.SortChildSources ();
                }
            );

            actions_id = action_service.UIManager.AddUiFromResource ("GlobalUI.xml");

            lastfm.Connection.StateChanged += HandleConnectionStateChanged;
            
            this["LastfmLoveAction"].Visible = false;
            this["LastfmHateAction"].Visible = false;
            this["LastfmLoveAction"].IsImportant = true;
            this["LastfmHateAction"].IsImportant = true;

            UpdateActions ();

            action_service.SourceActions ["SourcePropertiesAction"].Activated += OnSourceProperties;

            action_service.AddActionGroup (this);
        }

        public override void Dispose ()
        {
            if (actions != null) {
                action_service.UIManager.RemoveUi (actions_id);
                actions = null;
            }
            base.Dispose ();
        }

#region Action Handlers 

        private void OnAddStation (object sender, EventArgs args)
        {
            StationEditor ed = new StationEditor (lastfm);
            ed.Window.ShowAll ();
            ed.RunDialog ();
        }

        private void OnConnect (object sender, EventArgs args)
        {
            lastfm.Connection.Connect ();
        }

        private void OnSourceProperties (object o, EventArgs args)
        {
            Source source = action_service.SourceActions.ActionSource;
            if (source is LastfmSource) {
                ShowLoginDialog ();
            } else if (source is StationSource) {
                StationEditor editor = new StationEditor (lastfm, source as StationSource);
                editor.RunDialog ();
            }
        }

        private void OnLoved (object sender, EventArgs args)
        {
            LastfmTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as LastfmTrackInfo;
            if (track == null) 
                return;

            track.Love ();
        }

        private void OnHated (object sender, EventArgs args)
        {
            LastfmTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as LastfmTrackInfo;
            if (track == null)
                return;

            track.Ban ();
            ServiceManager.PlaybackController.Next ();
        }

        private void OnArtistVisitLastfm (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://last.fm/music/{0}"),
                CurrentArtist
            ));
        }

        private void OnAlbumVisitLastfm (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://last.fm/music/{0}/{1}"),
                CurrentArtist, CurrentAlbum
            ));
        }

        private void OnTrackVisitLastfm (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://last.fm/music/{0}/{1}"),
                CurrentArtist, CurrentTrack
            ));
        }

        private void OnArtistViewVideos (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://www.last.fm/music/{0}/+videos"),
                CurrentArtist
            ));
        }

        private void OnArtistVisitWikipedia (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://en.wikipedia.org/wiki/{0}"),
                CurrentArtist
            ));
        }

        /*private void OnArtistVisitAmazon (object sender, EventArgs args)
        {
            Browser.Open (String.Format (
                Catalog.GetString ("http://amazon.com/wiki/{0}"),
                CurrentArtist
            ));
        }

        private void OnAlbumVisitAmazon (object sender, EventArgs args)
        {
        }*/

        private void OnArtistPlayFanRadio (object sender, EventArgs args)
        {
            StationSource fan_radio = null;
            foreach (StationSource station in lastfm.Children) {
                if (station.Type == StationType.Fan && station.Arg == CurrentArtist) {
                    fan_radio = station;
                    break;
                }
            }
            
            if (fan_radio == null) {
                fan_radio = new StationSource (lastfm,
                    String.Format (Catalog.GetString ("Fans of {0}"), CurrentArtist),
                    "Fan", CurrentArtist
                );
                lastfm.AddChildSource (fan_radio);
            }

            ServiceManager.SourceManager.SetActiveSource (fan_radio);
        }

        private void OnArtistPlaySimilarRadio (object sender, EventArgs args)
        {
            StationSource similar_radio = null;
            foreach (StationSource station in lastfm.Children) {
                if (station.Type == StationType.Similar && station.Arg == CurrentArtist) {
                    similar_radio = station;
                    break;
                }
            }
            
            if (similar_radio == null) {
                similar_radio = new StationSource (lastfm,
                    String.Format (Catalog.GetString ("Similar to {0}"), CurrentArtist),
                    "Similar", CurrentArtist
                );
                lastfm.AddChildSource (similar_radio);
            }

            ServiceManager.SourceManager.SetActiveSource (similar_radio);
        }

        private void OnArtistRecommend (object sender, EventArgs args)
        {
        }

        private void OnAlbumRecommend (object sender, EventArgs args)
        {
        }

        private void OnTrackRecommend (object sender, EventArgs args)
        {
        }

#endregion

        private string artist;
        public string CurrentArtist {
            get { return artist; }
            set { artist = value; }
        }

        private string album;
        public string CurrentAlbum {
            get { return album; }
            set { album = value; }
        }

        private string track;
        public string CurrentTrack {
            get { return track; }
            set { track = value; }
        }

        public void ShowLoginDialog ()
        {
            AccountLoginDialog dialog = new AccountLoginDialog (lastfm.Account, true);
            dialog.SaveOnEdit = true;
            if (lastfm.Account.UserName == null) {
                dialog.AddSignUpButton ();
            }
            dialog.Run ();
            dialog.Destroy ();
        }

        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        { 
            if (args.Event == PlayerEngineEvent.EndOfStream || args.Event == PlayerEngineEvent.StartOfStream) {
                TrackInfo current_track = ServiceManager.PlayerEngine.CurrentTrack;
                this["LastfmLoveAction"].Visible = current_track is LastfmTrackInfo;
                this["LastfmHateAction"].Visible = current_track is LastfmTrackInfo;
                this["LastfmAddAction"].IsImportant = !(current_track is LastfmTrackInfo);
            }
        }

        private void HandleConnectionStateChanged (object sender, ConnectionStateChangedArgs args)
        {
            UpdateActions ();
        }

        private bool updating = false;
        private void UpdateActions ()
        {
            lock (this) {
                if (updating)
                    return;
                updating = true;
            }

            bool have_user = (lastfm.Account.UserName != null);
            this["LastfmAddAction"].Sensitive = have_user;
            this["LastfmSortAction"].Sensitive = have_user;
            this["LastfmConnectAction"].Visible = lastfm.Connection.State == ConnectionState.Disconnected;

            updating = false;
        }
    }
}
