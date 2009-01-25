//
// NowPlayingInterface.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008-2009 Novell, Inc.
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

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Sources.Gui;

using Clutter;
using Clutter.Gtk;

namespace Banshee.NowPlaying.Clutter
{
    public class NowPlayingInterface : VBox, ISourceContents
    {   
        private NowPlayingSource source;

        private Embed display;
        private Stage stage;
        private Texture video_texture;
        
        public NowPlayingInterface ()
        {
            display = new Embed ();
            stage = display.Stage;
            video_texture = new Texture (Banshee.ServiceStack.ServiceManager.PlayerEngine.VideoDisplayContext);
            video_texture.SizeChange += OnVideoTextureSizeChange;
            
            stage.Color = Color.Black;
            stage.Add (video_texture);
            
            PackStart (display, true, true, 0);
            
            HBox rotation_box = new HBox ();
            rotation_box.Spacing = 10;
            
            HScale x_angle = new HScale (0, 360, 1);
            x_angle.ValueChanged += delegate { video_texture.RotationAngleX = x_angle.Value; };
            
            HScale y_angle = new HScale (0, 360, 1);
            y_angle.ValueChanged += delegate { video_texture.RotationAngleY = y_angle.Value; };
            
            HScale z_angle = new HScale (0, 360, 1);
            z_angle.ValueChanged += delegate { video_texture.RotationAngleZ = z_angle.Value; };
            
            rotation_box.PackStart (x_angle, true, true, 0);
            rotation_box.PackStart (y_angle, true, true, 0);
            rotation_box.PackStart (z_angle, true, true, 0);
            rotation_box.ShowAll ();
            
            PackStart (rotation_box, false, false, 0);
            
            Show ();
        }
        
        private void OnVideoTextureSizeChange (object o, SizeChangeArgs args)
        {
            ReallocateVideoTexture (args.Width, args.Height);
        }
        
        private void ReallocateVideoTexture (int textureWidth, int textureHeight)
        {
            int stage_width, stage_height;
            
            stage.GetSize (out stage_width, out stage_height);
            
            int new_x, new_y, new_width, new_height;
            
            new_height = (textureHeight * stage_width) / textureWidth;
            if (new_height <= stage_height) {
                new_width = stage_width;
                new_x = 0;
                new_y = (stage_height - new_height) / 2;
            } else {
                new_width = (textureWidth * stage_height) / textureHeight;
                new_height = stage_height;
                new_x = (stage_width - new_width) / 2;
                new_y = 0;
            }
            
            video_texture.SetPosition (new_x, new_y);
            video_texture.SetSize (new_width, new_height);
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            
            int texture_width, texture_height;
            video_texture.GetSize (out texture_width, out texture_height);
            if (texture_width > 0 && texture_height > 0) {
                ReallocateVideoTexture (texture_width, texture_height);
            }
        }

        
#region Video Fullscreen Override

        private ViewActions.FullscreenHandler previous_fullscreen_handler;

        private void DisableFullscreenAction ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            Gtk.ToggleAction action = service.ViewActions["FullScreenAction"] as Gtk.ToggleAction;
            if (action != null) {
                action.Active = false;
            }
        }

        internal void OverrideFullscreen ()
        {
            FullscreenHandler (false);
            
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            previous_fullscreen_handler = service.ViewActions.Fullscreen;
            service.ViewActions.Fullscreen = FullscreenHandler;
            DisableFullscreenAction ();
        }

        internal void RelinquishFullscreen ()
        {
            FullscreenHandler (false);
            
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            service.ViewActions.Fullscreen = previous_fullscreen_handler;
        }

        private void FullscreenHandler (bool fullscreen)
        {
            if (fullscreen) {
                stage.Fullscreen ();
            } else {
                stage.Unfullscreen ();
            }
        }
        
#endregion
        
#region ISourceContents
        
        public bool SetSource (ISource src)
        {
            this.source = source as NowPlayingSource;
            if (display != null) {
                display.Show ();
            }
            return this.source != null;
        }

        public ISource Source {
            get { return source; }
        }

        public void ResetSource ()
        {
            source = null;
        }

        public Widget Widget {
            get { return this; }
        }
        
#endregion

    }
}
