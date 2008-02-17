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

namespace Banshee.Lastfm.Radio
{
    public class LastfmActions : BansheeActionGroup
    {
        private LastfmSource lastfm;

        private ActionButton love_button;
        private ActionButton hate_button;
        private bool last_track_was_lastfm = false;

        private InterfaceActionService action_service;
        private uint actions_id;
        private ActionGroup actions;

        public LastfmActions (LastfmSource lastfm) : base ("Lastfm")
        {
            action_service = ServiceManager.Get<InterfaceActionService> ();
            this.lastfm = lastfm;

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
                ),
                new ActionEntry (
                    "LastfmLoveAction", "face-smile",
                    Catalog.GetString ("Love Track"), null,
                    Catalog.GetString ("Mark current track as loved"), OnLoved
                ),
                new ActionEntry (
                    "LastfmHateAction", "face-sad",
                    Catalog.GetString ("Ban Track"), null,
                    Catalog.GetString ("Mark current track as banned"), OnHated
                )
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

            /*Globals.ActionManager["LastfmLoveAction"].IsImportant = true;
            Globals.ActionManager["LastfmHateAction"].IsImportant = true;
            Globals.ActionManager["LastfmLoveAction"].Visible = false;
            Globals.ActionManager["LastfmHateAction"].Visible = false;
            ActionButton love_button = new ActionButton (actions.GetAction ("LastfmLoveAction"));
            love_button.Pixbuf = IconThemeUtils.LoadIcon ("face-smile", 22);
            love_button.Padding = 1;
            ActionButton hate_button = new ActionButton (actions.GetAction ("LastfmHateAction"));
            hate_button.Pixbuf = IconThemeUtils.LoadIcon ("face-sad", 22);
            hate_button.Padding = 1;
            InterfaceElements.ActionButtonBox.PackStart (love_button, false, false, 0);
            InterfaceElements.ActionButtonBox.PackStart (hate_button, false, false, 0);*/

            /*PlayerEngineCore.EventChanged += delegate (object o, PlayerEngineEventArgs args) {
                if (args.Event == PlayerEngineEvent.TrackInfoUpdated) {
                    if (PlayerEngineCore.CurrentTrack is LastfmTrackInfo) {
                        if (!last_track_was_lastfm) {
                            this["LastfmLoveAction"].Visible = true;
                            this["LastfmHateAction"].Visible = true;
                            last_track_was_lastfm = true;
                        }
                    } else {
                        if (last_track_was_lastfm) {
                            this["LastfmLoveAction"].Visible = false;
                            this["LastfmHateAction"].Visible = false;
                            last_track_was_lastfm = false;
                        }
                    }
                }
            };*/

            UpdateActions ();

            action_service.SourceActions ["SourcePropertiesAction"].Activated += OnSourceProperties;

            action_service.AddActionGroup (this);
        }

        public override void Dispose ()
        {
            //InterfaceElements.ActionButtonBox.Remove (love_button);
            //InterfaceElements.ActionButtonBox.Remove (hate_button);

            //Globals.ActionManager.UI.RemoveUi (actions_id);
            //Globals.ActionManager.UI.RemoveActionGroup (actions);
            actions = null;

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

        private void OnChangeStation (object sender, EventArgs args)
        {
            (ServiceManager.SourceManager.ActiveSource as StationSource).ChangeToThisStation ();
        }

        private void OnRefreshStation (object sender, EventArgs args)
        {
            (ServiceManager.SourceManager.ActiveSource as StationSource).Refresh ();
        }

        private void OnLoved (object sender, EventArgs args)
        {
            LastfmTrackInfo track = null;//PlayerEngineCore.CurrentTrack as LastfmTrackInfo;
            if (track == null)
                return;

            track.Love ();
        }

        private void OnHated (object sender, EventArgs args)
        {
            LastfmTrackInfo track = null; //PlayerEngineCore.CurrentTrack as LastfmTrackInfo;
            if (track == null)
                return;

            track.Ban ();
            action_service.GlobalActions ["NextAction"].Activate ();
        }

        public void ShowLoginDialog ()
        {
            AccountLoginDialog dialog = new AccountLoginDialog (lastfm.Account, true);
            dialog.SaveOnEdit = true;
            if (lastfm.Account.Username == null) {
                dialog.AddSignUpButton ();
            }
            dialog.Run ();
            dialog.Destroy ();
        }

#endregion

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

            bool have_user = (lastfm.Account.Username != null);
            this["LastfmAddAction"].Sensitive = have_user;
            this["LastfmSortAction"].Sensitive = have_user;
            this["LastfmConnectAction"].Visible = lastfm.Connection.State == ConnectionState.Disconnected;

            updating = false;
        }
    }
}
