//
// TrackInfoDisplay.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Larry Ewing <lewing@novell.com> (Is my hero)
//
// Copyright (C) 2007 Novell, Inc.
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
using Mono.Unix;

using Gtk;
using Gdk;

using Hyena;
using Hyena.Gui;
using Hyena.Gui.Theatrics;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Gui.Widgets
{
    public abstract class TrackInfoDisplay : Widget
    {
        private ArtworkManager artwork_manager;
        protected ArtworkManager ArtworkManager {
            get { return artwork_manager; }
        }
        
        private Pixbuf current_pixbuf;
        protected Pixbuf CurrentPixbuf {
            get { return current_pixbuf; }
        }
        
        private Pixbuf incoming_pixbuf;
        protected Pixbuf IncomingPixbuf {
            get { return incoming_pixbuf; }
        }
        
        private Pixbuf missing_audio_pixbuf;
        protected Pixbuf MissingAudioPixbuf {
            get { return missing_audio_pixbuf ?? missing_audio_pixbuf = IconThemeUtils.LoadIcon (MissingIconSizeRequest, "audio-x-generic"); }
        }
        
        private Pixbuf missing_video_pixbuf;
        protected Pixbuf MissingVideoPixbuf {
            get { return missing_video_pixbuf ?? missing_video_pixbuf = IconThemeUtils.LoadIcon (MissingIconSizeRequest, "video-x-generic"); }
        }
        
        private Cairo.Color background_color;
        protected virtual Cairo.Color BackgroundColor {
            get { return background_color; }
        }
        
        private Cairo.Color text_color;
        protected virtual Cairo.Color TextColor {
            get { return text_color; }
        }
        
        private Cairo.Color text_light_color;
        protected virtual Cairo.Color TextLightColor {
            get { return text_light_color; }
        }
        
        private TrackInfo current_track;
        protected TrackInfo CurrentTrack {
            get { return current_track; }
        }
        
        private TrackInfo incoming_track;   
        protected TrackInfo IncomingTrack {
            get { return incoming_track; }
        }
        
        private uint idle_timeout_id = 0;
        private SingleActorStage stage = new SingleActorStage ();
        private Dictionary<Pixbuf, Cairo.Surface> surface_cache = new Dictionary<Pixbuf, Cairo.Surface> ();
        
        protected TrackInfoDisplay (IntPtr native) : base (native)
        {
        }
        
        public TrackInfoDisplay ()
        {
            stage.Iteration += OnStageIteration;
        
            if (ServiceManager.Contains<ArtworkManager> ()) {
                artwork_manager = ServiceManager.Get<ArtworkManager> ();
            }
            
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, 
                PlayerEvent.StartOfStream | 
                PlayerEvent.TrackInfoUpdated | 
                PlayerEvent.StateChange);
                
            WidgetFlags |= WidgetFlags.NoWindow;
        }
        
        public override void Dispose ()
        {
            if (idle_timeout_id > 0) {
                GLib.Source.Remove (idle_timeout_id);
            }
            
            if (ServiceManager.PlayerEngine != null) {
                ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            }
            
            stage.Iteration -= OnStageIteration;
            stage = null;
            
            SurfaceCacheFlush ();
            
            base.Dispose ();
        }
        
        protected override void OnRealized ()
        {
            GdkWindow = Parent.GdkWindow;
            base.OnRealized ();
        }
        
        protected override void OnUnrealized ()
        {
            base.OnUnrealized ();
            SurfaceCacheFlush ();
        }
        
        protected override void OnSizeAllocated (Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            
            if (current_track == null) {
                LoadCurrentTrack ();
            } else {
                LoadPixbuf (current_track);
            }
        }

        protected override void OnStyleSet (Style previous)
        {
            base.OnStyleSet (previous);
            
            text_color = CairoExtensions.GdkColorToCairoColor (Style.Foreground (StateType.Normal));
            background_color = CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Normal));
            text_light_color = Hyena.Gui.Theming.GtkTheme.GetCairoTextMidColor (this);
            
            if (missing_audio_pixbuf != null) {
                missing_audio_pixbuf.Dispose ();
                missing_audio_pixbuf = null;
            }

            if (missing_video_pixbuf != null) {
                missing_video_pixbuf.Dispose ();
                missing_video_pixbuf = null;
            }
        }
        
        protected override bool OnExposeEvent (EventExpose evnt)
        {
            bool idle = incoming_track == null && current_track == null;
            if (!Visible || !IsMapped || (idle && !CanRenderIdle)) {
                return true;
            }
            
            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);
            
            foreach (Gdk.Rectangle damage in evnt.Region.GetRectangles ()) {
                cr.Rectangle (damage.X, damage.Y, damage.Width, damage.Height);
                cr.Clip ();
            
                if (idle) {
                    RenderIdle (cr);
                } else {
                    RenderAnimation (cr);
                }
            
                cr.ResetClip ();
            }
            
            CairoExtensions.DisposeContext (cr);
            
            return true;
        }

        protected virtual bool CanRenderIdle {
            get { return false; }
        }
        
        protected virtual void RenderIdle (Cairo.Context cr)
        {
        }
        
        private void RenderAnimation (Cairo.Context cr)
        {
            if (stage.Actor == null) {
                // We are not in a transition, just render
                RenderStage (cr, current_track, current_pixbuf);
                return;
            } 
            
            if (current_track == null) {
                // Fade in the whole stage, nothing to fade out
                CairoExtensions.PushGroup (cr);
                RenderStage (cr, incoming_track, incoming_pixbuf);
                CairoExtensions.PopGroupToSource (cr);
                
                cr.PaintWithAlpha (stage.Actor.Percent);
                return;
            }
            
            // XFade only the cover art
            RenderCoverArt (cr, incoming_pixbuf);
            
            CairoExtensions.PushGroup (cr);
            RenderCoverArt (cr, current_pixbuf);
            CairoExtensions.PopGroupToSource (cr);
            
            cr.PaintWithAlpha (1.0 - stage.Actor.Percent);
            
            // Fade in/out the text
            bool same_artist_album = incoming_track != null ? incoming_track.ArtistAlbumEqual (current_track) : false;
            bool same_track = incoming_track != null ? incoming_track.Equals (current_track) : false;
            
            if (same_artist_album) {
                RenderTrackInfo (cr, incoming_track, same_track, true);
            }
            
            if (stage.Actor.Percent <= 0.5) {
                // Fade out old text
                CairoExtensions.PushGroup (cr);
                RenderTrackInfo (cr, current_track, !same_track, !same_artist_album);
                CairoExtensions.PopGroupToSource (cr);
               
                cr.PaintWithAlpha (1.0 - (stage.Actor.Percent * 2.0));
            } else {
                // Fade in new text
                CairoExtensions.PushGroup (cr);
                RenderTrackInfo (cr, incoming_track, !same_track, !same_artist_album);
                CairoExtensions.PopGroupToSource (cr);
                
                cr.PaintWithAlpha ((stage.Actor.Percent - 0.5) * 2.0);
            }
        }
        
        private void RenderStage (Cairo.Context cr, TrackInfo track, Pixbuf pixbuf)
        {
            RenderCoverArt (cr, pixbuf);
            RenderTrackInfo (cr, track, true, true);
        }
        
        protected virtual void RenderCoverArt (Cairo.Context cr, Pixbuf pixbuf)
        {
            ArtworkRenderer.RenderThumbnail (cr, pixbuf, false, Allocation.X, Allocation.Y, 
                ArtworkSizeRequest, ArtworkSizeRequest, 
                !IsMissingPixbuf (pixbuf), 0, 
                IsMissingPixbuf (pixbuf), BackgroundColor);
        }

        protected bool IsMissingPixbuf (Pixbuf pb)
        {
            return (pb == missing_audio_pixbuf || pb == missing_video_pixbuf);
        }
        
        protected abstract void RenderTrackInfo (Cairo.Context cr, TrackInfo track, bool renderTrack, bool renderArtistAlbum);
        
        protected virtual int ArtworkSizeRequest {
            get { return Allocation.Height; }
        }
        
        protected virtual int MissingIconSizeRequest {
            get { return 32; }
        }
        
        private void OnPlayerEvent (PlayerEventArgs args)
        {
            if (args.Event == PlayerEvent.StartOfStream || args.Event == PlayerEvent.TrackInfoUpdated) {
                LoadCurrentTrack ();
            } else if (args.Event == PlayerEvent.StateChange && (incoming_track != null || incoming_pixbuf != null)) {
                PlayerEventStateChangeArgs state = (PlayerEventStateChangeArgs)args;
                if (state.Current == PlayerState.Idle) {
                    if (idle_timeout_id > 0) {
                        GLib.Source.Remove (idle_timeout_id);
                    } else {
                        GLib.Timeout.Add (100, IdleTimeout);
                    }
                }
            }
        }
        
        private bool IdleTimeout ()
        {
            if (ServiceManager.PlayerEngine.CurrentTrack == null || 
                ServiceManager.PlayerEngine.CurrentState == PlayerState.Idle) {
                incoming_track = null;
                incoming_pixbuf = null;
                
                if (stage != null && stage.Actor == null) {
                    stage.Reset ();
                }
            }
            
            idle_timeout_id = 0;
            return false;
        }
        
        private void LoadCurrentTrack ()
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;

            if (track == current_track && !IsMissingPixbuf (current_pixbuf)) {
                return;
            } else if (track == null) {
                incoming_track = null;
                incoming_pixbuf = null;
                return;
            }

            incoming_track = track;
            
            LoadPixbuf (track);

            if (stage.Actor == null) {
                stage.Reset ();
            }
        }
        
        private void LoadPixbuf (TrackInfo track)
        {
            Gdk.Pixbuf pixbuf = artwork_manager.LookupScale (track.ArtworkId, ArtworkSizeRequest);

            if (pixbuf == null) {
                LoadMissingPixbuf ((track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0);
            } else {
                incoming_pixbuf = pixbuf;
            }
            
            if (track == current_track) {
                current_pixbuf = incoming_pixbuf;
            }
        }

        private void LoadMissingPixbuf (bool is_video)
        {
            incoming_pixbuf = is_video ? MissingVideoPixbuf : MissingAudioPixbuf;
        }
        
        private double last_fps = 0.0;
        
        private void OnStageIteration (object o, EventArgs args)
        {
            Invalidate ();
            
            if (stage.Actor != null) {
                last_fps = stage.Actor.FramesPerSecond;
                return;
            }
            
            SurfaceCacheFlush ();
            
            if (ApplicationContext.Debugging) {
                Log.DebugFormat ("TrackInfoDisplay RenderAnimation: {0:0.00} FPS", last_fps);
            }
            
            if (current_pixbuf != incoming_pixbuf && !IsMissingPixbuf (current_pixbuf)) {
                ArtworkRenderer.DisposePixbuf (current_pixbuf);
            }
            
            current_pixbuf = incoming_pixbuf;
            current_track = incoming_track;
            
            incoming_track = null;
            
            OnArtworkChanged ();
        }
        
        protected virtual void Invalidate ()
        {
            QueueDraw ();
        }
        
        protected virtual void OnArtworkChanged ()
        {
        }
        
        protected virtual string GetFirstLineText (TrackInfo track)
        {
            return String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (track.DisplayTrackTitle));
        }
        
        protected virtual string GetSecondLineText (TrackInfo track)
        {
            string markup = null;
            Banshee.Streaming.RadioTrackInfo radio_track = track as Banshee.Streaming.RadioTrackInfo;

            if ((track.MediaAttributes & TrackMediaAttributes.Podcast) != 0) {
                // Translators: {0} and {1} are for markup so ignore them, {2} and {3}
                // are Podcast Name and Published Date, respectively;
                // e.g. 'from BBtv published 7/26/2007'
                markup = MarkupFormat (Catalog.GetString ("{0}from{1} {2} {0}published{1} {3}"), 
                    track.DisplayAlbumTitle, track.ReleaseDate.ToShortDateString ());
            } else if (radio_track != null && radio_track.ParentTrack != null) {
                // This is complicated because some radio streams send tags when the song changes, and we
                // want to display them if they do.  But if they don't, we want it to look good too, so we just
                // display the station name for the second line.
                string by_from = GetByFrom (
                    track.ArtistName == radio_track.ParentTrack.ArtistName ? null : track.ArtistName, track.DisplayArtistName,
                    track.AlbumTitle == radio_track.ParentTrack.AlbumTitle ? null : track.AlbumTitle, track.DisplayAlbumTitle, false
                );
                
                if (String.IsNullOrEmpty (by_from)) {
                    // simply: "Chicago Public Radio" or whatever the artist name is
                    markup = GLib.Markup.EscapeText (radio_track.ParentTrack.ArtistName ?? Catalog.GetString ("Unknown Stream"));
                } else {
                    // Translators: {0} and {1} are markup so ignore them, {2} is the name of the radio station
                    string on = MarkupFormat (Catalog.GetString ("{0}on{1} {2}"), radio_track.ParentTrack.TrackTitle);
                    
                    // Translators: {0} is the "from {album} by {artist}" type string, and {1} is the "on {radio station name}" string
                    markup = String.Format (Catalog.GetString ("{0} {1}"), by_from, on);
                }
            } else {
                markup = GetByFrom (track.ArtistName, track.DisplayArtistName, track.AlbumTitle, track.DisplayAlbumTitle, true);
            }
            
            return String.Format ("<span color=\"{0}\">{1}</span>",  
                CairoExtensions.ColorGetHex (TextColor, false),
                markup);
        }
        
        private string MarkupFormat (string fmt, params string [] args)
        {
            string [] new_args = new string [args.Length + 2];
            new_args[0] = String.Format ("<span color=\"{0}\" size=\"small\">", 
                CairoExtensions.ColorGetHex (TextLightColor, false));
            new_args[1] = "</span>";
            
            for (int i = 0; i < args.Length; i++) {
                new_args[i + 2] = GLib.Markup.EscapeText (args[i]);
            }
            
            return String.Format (fmt, new_args);
        }
        
        private string GetByFrom (string artist, string display_artist, string album, string display_album, bool unknown_ok)
        {
            
            bool has_artist = !String.IsNullOrEmpty (artist);
            bool has_album = !String.IsNullOrEmpty (album);

            string markup = null;
            if (has_artist && has_album) {
                // Translators: {0} and {1} are for markup so ignore them, {2} and {3}
                // are Artist Name and Album Title, respectively;
                // e.g. 'by Parkway Drive from Killing with a Smile'
                markup = MarkupFormat (Catalog.GetString ("{0}by{1} {2} {0}from{1} {3}"), display_artist, display_album);
            } else if (has_album) {
                // Translators: {0} and {1} are for markup so ignore them, {2} is for Album Title;
                // e.g. 'from Killing with a Smile'
                markup = MarkupFormat (Catalog.GetString ("{0}from{1} {2}"), display_album);
            } else if (has_artist || unknown_ok) {
                // Translators: {0} and {1} are for markup so ignore them, {2} is for Artist Name;
                // e.g. 'by Parkway Drive'
                markup = MarkupFormat (Catalog.GetString ("{0}by{1} {2}"), display_artist);
            }
            return markup;
        }
        
        protected void SurfaceExpire (Gdk.Pixbuf pixbuf)
        {
            if (pixbuf == null) {
                return;
            }
            
            Cairo.Surface surface = null;
            if (surface_cache.TryGetValue (pixbuf, out surface)) {
                surface.Destroy ();
                surface_cache.Remove (pixbuf);
            }
        }
        
        protected void SurfaceCacheFlush ()
        {
            foreach (Cairo.Surface surface in surface_cache.Values) {
                surface.Destroy ();
            }
            
            surface_cache.Clear ();
        }
        
        protected void SurfaceCache (Gdk.Pixbuf pixbuf, Cairo.Surface surface)
        {
            if (pixbuf == null || surface == null) {
                return;
            }
            
            SurfaceExpire (pixbuf);
            surface_cache.Add (pixbuf, surface);
        }
        
        protected Cairo.Surface SurfaceLookup (Gdk.Pixbuf pixbuf)
        {
            Cairo.Surface surface = null;
            if (pixbuf != null) {
                surface_cache.TryGetValue (pixbuf, out surface);
            }
            return surface;
        }
    }
}
