//
// RecommendationService.cs
//
// Authors:
//   Aaron Bockover <aaron@abock.org>
//   Fredrik Hedberg
//   Gabriel Burt <gburt@novell.com>
//   Lukas Lipka
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Gtk;
using Mono.Unix;

using Hyena;

using Lastfm;
using Lastfm.Gui;

using Banshee.MediaEngine;
using Banshee.Base;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.Networking;

using Banshee.Collection;

using Browser = Lastfm.Browser;

namespace Banshee.Lastfm.Recommendations
{
    public class RecommendationService : IExtensionService
    {
        private RecommendationPane pane;
        private RecommendationActions actions;

        void IExtensionService.Initialize ()
        {
            pane = new RecommendationPane ();

            BaseClientWindow nereid = ServiceManager.Get<IService> ("NereidPlayerInterface") as Banshee.Gui.BaseClientWindow;
            if (nereid != null) {
                nereid.ViewContainer.PackEnd (pane, false, false, 0);
            }

            actions = new RecommendationActions (this);

            ServiceManager.PlaybackController.SourceChanged += OnSourceChanged;
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, PlayerEvent.StartOfStream | PlayerEvent.EndOfStream);
        }

        public void Dispose ()
        {
            ServiceManager.PlaybackController.SourceChanged -= OnSourceChanged;
            ServiceManager.SourceManager.ActiveSourceChanged -= OnActiveSourceChanged;
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);

            if (pane != null) {
                pane.Destroy ();
                pane = null;
            }

            actions.Dispose ();
        }

        private void OnActiveSourceChanged (EventArgs args)
        {
            UpdateVisibility ();
        }

        private void OnSourceChanged (object sender, EventArgs args)
        {
            UpdateVisibility ();
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
            if (track != null) {
                ShowRecommendations (track.ArtistName);
            }
        }

        private void UpdateVisibility ()
        {
            bool source_is_playback_source = (ServiceManager.SourceManager.ActiveSource == ServiceManager.PlaybackController.Source);
            pane.ShowWhenReady = ShowSchema.Get () && source_is_playback_source;
            if (!source_is_playback_source) {
                pane.HideWithTimeout ();
            } else if (!pane.ShowWhenReady) {
                pane.Hide ();
            }
        }

        private void ShowRecommendations (string artist)
        {
            lock (this) {
                if (pane.Visible && pane.Artist == artist) {
                    return;
                }

                if (!String.IsNullOrEmpty (artist)) {
                    pane.Artist = artist;
                }

                UpdateVisibility ();
            }
        }

        public bool RecommendationsShown {
            set {
                ShowSchema.Set (value);

                TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
                if (track != null) {
                    pane.Artist = track.ArtistName;
                }
                UpdateVisibility ();
            }
        }

        string IService.ServiceName {
            get { return "LastfmRecommendationService"; }
        }

        public static readonly SchemaEntry<bool> ShowSchema = new SchemaEntry<bool>(
            "plugins.recommendation", "show",
            true,
            "Show recommendations",
            "Show recommendations for the currently playing artist"
        );
    }
}
