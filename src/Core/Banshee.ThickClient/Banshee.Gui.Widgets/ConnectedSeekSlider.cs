//
// ConnectedSeekSlider.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Gtk;

using Banshee.Widgets;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.Gui.Widgets
{
    public class ConnectedSeekSlider : Alignment
    {
        private SeekSlider seek_slider;
        private StreamPositionLabel stream_position_label;
    
        public ConnectedSeekSlider () : base (0.0f, 0.0f, 1.0f, 1.0f)
        {
            RightPadding = 10;
            LeftPadding = 10;
            
            BuildSeekSlider ();
            
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
            ServiceManager.PlayerEngine.StateChanged += OnPlayerEngineStateChanged;
            seek_slider.SeekRequested += OnSeekRequested;
        }
        
        private void BuildSeekSlider ()
        {
            VBox box = new VBox ();
            Add (box);
            
            seek_slider = new SeekSlider ();
            stream_position_label = new StreamPositionLabel (seek_slider);
            
            seek_slider.SetSizeRequest (125, -1);
            
            box.PackStart (seek_slider, true, true, 0);
            box.PackStart (stream_position_label, true, true, 0);
            
            box.ShowAll ();
        }
        
        private void OnPlayerEngineStateChanged (object o, PlayerEngineStateArgs args)
        {
            switch (args.State) {
                case PlayerEngineState.Contacting:
                    stream_position_label.IsContacting = true;
                    seek_slider.SetIdle ();
                    break;
                case PlayerEngineState.Loaded:
                    seek_slider.Duration = ServiceManager.PlayerEngine.CurrentTrack.Duration.TotalSeconds;
                    break;
                case PlayerEngineState.Idle:
                    seek_slider.SetIdle ();
                    stream_position_label.IsContacting = false;
                    break;
            }
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            switch (args.Event) {
                case PlayerEngineEvent.Iterate:
                    OnPlayerEngineTick ();
                    break;
                case PlayerEngineEvent.StartOfStream:
                    seek_slider.CanSeek = true;
                    break;
                case PlayerEngineEvent.Buffering:
                    if (args.BufferingPercent >= 1.0) {
                        stream_position_label.IsBuffering = false;
                        break;
                    }
                    
                    stream_position_label.IsBuffering = true;
                    stream_position_label.BufferingProgress = args.BufferingPercent;
                    seek_slider.SetIdle ();
                    break;
            }
        }

        private void OnPlayerEngineTick ()
        {
            if (ServiceManager.PlayerEngine == null) {
                return;
            }
            
            uint stream_length = ServiceManager.PlayerEngine.Length;
            uint stream_position = ServiceManager.PlayerEngine.Position;
            
            stream_position_label.IsContacting = false;
            seek_slider.CanSeek = ServiceManager.PlayerEngine.CanSeek;
            seek_slider.Duration = stream_length;
            seek_slider.SeekValue = stream_position;
        }
        
        private void OnSeekRequested (object o, EventArgs args)
        {
            ServiceManager.PlayerEngine.Position = (uint)seek_slider.Value;
        }
    }
}
