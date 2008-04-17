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

using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.NowPlaying
{
    public class NowPlayingSource : Source, IDisposable
    {
        private TrackInfo transitioned_track;

        protected override string TypeUniqueId {
            get { return "now-playing"; }
        }
        
        public NowPlayingSource () : base ("now-playing", Catalog.GetString ("Now Playing"), 0)
        {
            Properties.SetString ("Icon.Name", "applications-multimedia");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new NowPlayingInterface ());
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            
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
            if (current_track != null && transitioned_track != current_track && 
                (current_track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                ServiceManager.SourceManager.SetActiveSource (this);
            }

            if ((current_track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                OnUserNotifyUpdated ();
            }
        }
        
        public void Dispose ()
        {
        }
        
#region Video Fullscreen Override

        private Gtk.Window fullscreen_window;
        private ViewActions.FullscreenHandler previous_fullscreen_handler;

        private void DisableFullscreenAction ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            Gtk.ToggleAction action = service.ViewActions["FullScreenAction"] as Gtk.ToggleAction;
            if (action != null) {
                action.Active = false;
            }
        }

        public override void Activate ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            previous_fullscreen_handler = service.ViewActions.Fullscreen;
            service.ViewActions.Fullscreen = FullscreenHandler;
            DisableFullscreenAction ();
        }

        public override void Deactivate ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            service.ViewActions.Fullscreen = previous_fullscreen_handler;
        }
        
        private void OnFullscreenWindowDestroyed (object o, EventArgs args)
        {
            if (fullscreen_window != null) {
                fullscreen_window.Destroyed -= OnFullscreenWindowDestroyed;
                fullscreen_window = null;
            }
            
            DisableFullscreenAction ();
        }
        
        private void FullscreenHandler (bool fullscreen)
        {
            if (fullscreen) {
                if (fullscreen_window == null) {
                    GtkElementsService service = ServiceManager.Get<GtkElementsService> (); 
                    fullscreen_window = new FullscreenWindow (service.PrimaryWindow.Title, service.PrimaryWindow);
                    fullscreen_window.Destroyed += OnFullscreenWindowDestroyed;
                }
                
                fullscreen_window.Show ();
                fullscreen_window.Fullscreen ();
            } else if (fullscreen_window != null) {
                fullscreen_window.Destroy ();
            }
        }
        
#endregion

    }
}
