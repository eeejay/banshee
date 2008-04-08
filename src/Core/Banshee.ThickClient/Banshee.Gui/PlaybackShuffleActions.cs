//
// PlaybackShuffleActions.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
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
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Gui;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.PlaybackController;

namespace Banshee.Gui
{
    public class PlaybackShuffleActions : BansheeActionGroup, IRadioActionGroup
    {
        private RadioAction active_action;
        private PlaybackActions playback_actions;

        public RadioAction Active {
            get { return active_action; }
            set {
                active_action = value;
                ShuffleMode.Set (active_action == null ? String.Empty : ActionNameToConfigId (active_action.Name));
                ServiceManager.PlaybackController.ShuffleMode = (PlaybackShuffleMode)active_action.Value;
            }
        }

        public event ChangedHandler Changed;
        
        public PlaybackShuffleActions (InterfaceActionService actionService, PlaybackActions playbackActions)
            : base ("PlaybackShuffle")
        {
            playback_actions = playbackActions;
            actionService.AddActionGroup (this);

            Add (new RadioActionEntry [] {
                new RadioActionEntry ("ShuffleOffAction", null, 
                    Catalog.GetString ("Shuffle _Off"), null,
                    Catalog.GetString ("Do not shuffle playlist"),
                    (int)PlaybackShuffleMode.Linear),
                    
                new RadioActionEntry ("ShuffleSongAction", null,
                    Catalog.GetString ("Shuffle by _Song"), null,
                    Catalog.GetString ("Play songs randomly from the playlist"),
                    (int)PlaybackShuffleMode.Song),
                    
                new RadioActionEntry ("ShuffleArtistAction", null,
                    Catalog.GetString ("Shuffle by A_rtist"), null,
                    Catalog.GetString ("Play all songs by an artist, then randomly choose another artist"),
                    (int)PlaybackShuffleMode.Artist),
                    
                new RadioActionEntry ("ShuffleAlbumAction", null,
                    Catalog.GetString ("Shuffle by A_lbum"), null,
                    Catalog.GetString ("Play all songs from an album, then randomly choose another album"),
                    (int)PlaybackShuffleMode.Album)
            }, 0, OnChanged);
                
            this["ShuffleOffAction"].IconName = "media-skip-forward";
            this["ShuffleSongAction"].IconName = "media-playlist-shuffle";
            this["ShuffleArtistAction"].IconName = "media-playlist-shuffle";
            this["ShuffleAlbumAction"].IconName = "media-playlist-shuffle";
            this["ShuffleArtistAction"].Sensitive = false;
            this["ShuffleAlbumAction"].Sensitive = false;

            Gtk.Action action = this[ConfigIdToActionName (ShuffleMode.Get ())];
            if (action is RadioAction) {
                active_action = (RadioAction)action;
            } else {
                Active = (RadioAction)this["ShuffleOffAction"];
            }
            
            Active.Activate ();
        }

        private void OnChanged (object o, ChangedArgs args)
        {
            Active = args.Current;
            
            playback_actions["NextAction"].IconName = Active.IconName;
            
            ChangedHandler handler = Changed;
            if (handler != null) {
                handler (o, args);
            }
        }

        public IEnumerator<RadioAction> GetEnumerator ()
        {
            yield return (RadioAction)this["ShuffleOffAction"];
            yield return (RadioAction)this["ShuffleSongAction"];
            yield return (RadioAction)this["ShuffleArtistAction"];
            yield return (RadioAction)this["ShuffleAlbumAction"];
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        private static string ConfigIdToActionName (string configuration)
        {
            return String.Format ("{0}Action", StringUtil.UnderCaseToCamelCase (configuration));
        }

        private static string ActionNameToConfigId (string actionName)
        {
            return StringUtil.CamelCaseToUnderCase (actionName.Substring (0, 
                actionName.Length - (actionName.EndsWith ("Action") ? 6 : 0)));
        }

        public static readonly SchemaEntry<string> ShuffleMode = new SchemaEntry<string> (
            "playback", "shuffle_mode",
            "off",
            "Shuffle playback",
            "Shuffle mode (shuffle_off, shuffle_song, shuffle_artist, shuffle_album)"
        );
    }
}
