//
// NowPlayingInterface.cs
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

using Gtk;

using Banshee.Sources;
using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.NowPlaying
{
    public class NowPlayingInterface : VBox, ISourceContents
    {   
        private NowPlayingSource source;
        private VideoDisplay video_display;
        private Hyena.Widgets.RoundedFrame frame;
        private Hyena.Gui.AnimatedVBox anim_vbox;
        
        private VisualisationConfigBox vis_box;
        private VisualisationConfigBox old_box;
        
        public NowPlayingInterface ()
        {
            video_display = new VideoDisplay ();
            video_display.Show ();
            
            vis_box = new VisualisationConfigBox ();
            vis_box.BorderWidth = 6;
            vis_box.Show ();
            
            anim_vbox = new Hyena.Gui.AnimatedVBox ();
            anim_vbox.Spacing = 6;
            anim_vbox.Show ();
            
            frame = new Hyena.Widgets.RoundedFrame ();
            frame.SetFillColor (new Cairo.Color (0, 0, 0));
            frame.DrawBorder = false;
            frame.Add (video_display);
            frame.Show ();
            
            ShowVisualisationBox ();
            PackStart (anim_vbox, false, false, 0);
            PackStart (frame, true, true, 0);
        }
        
        public void ShowVisualisationBox ()
        {
            if (!anim_vbox.Contains (vis_box)) {
                if (old_box != null) {
                    vis_box = new VisualisationConfigBox (old_box);
                    old_box.Dispose ();
                }
                
                anim_vbox.Add (vis_box);
            }
        }
        
        public void HideVisualisationBox ()
        {
            if (anim_vbox.Contains (vis_box)) {
                anim_vbox.Remove (vis_box, Hyena.Gui.Easing.QuadraticOut);
                old_box = vis_box;
            }
        }
        
#region ISourceContents
        
        public bool SetSource (ISource src)
        {
            this.source = source as NowPlayingSource;
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
