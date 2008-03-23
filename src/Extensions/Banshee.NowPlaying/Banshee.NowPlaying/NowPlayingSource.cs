//
// NowPlayingSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

using Banshee.Sources.Gui;

namespace Banshee.NowPlaying
{
    public class NowPlayingSource : Source, IDisposable
    {
        private TrackInfo transitioned_track;
        private NowPlayingInterface nowplaying_interface;
        
        public NowPlayingSource () : base ("now-playing", Catalog.GetString ("Now Playing"), 0)
        {
            nowplaying_interface = new NowPlayingInterface ();
            
            Properties.SetString ("Icon.Name", "media-playback-start");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", nowplaying_interface);
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);
            
            ServiceManager.SourceManager.AddSource (this);
            
            ServiceManager.PlaybackController.Transition += OnPlaybackControllerTransition;
            ServiceManager.PlaybackController.TrackStarted += OnPlaybackControllerTrackStarted;
        }
        
        private void OnPlaybackControllerTransition (object o, EventArgs args)
        {
            transitioned_track = ServiceManager.PlaybackController.CurrentTrack;
        }
        
        private void OnPlaybackControllerTrackStarted (object o, EventArgs args)
        { 
            TrackInfo current_track = ServiceManager.PlaybackController.CurrentTrack;
            if (current_track != null && 
                (current_track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                if (transitioned_track != current_track) {
                    ServiceManager.SourceManager.SetActiveSource (this);
                }
                
                nowplaying_interface.HideVisualisationBox ();
            } else {
                nowplaying_interface.ShowVisualisationBox ();
            }
        }
        
        public void Dispose ()
        {
        }
        
#region Source Overrides

        public override int Count {
            get { return 0; }
        }

#endregion

    }
}
