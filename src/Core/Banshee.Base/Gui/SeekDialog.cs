/***************************************************************************
 *  SeekDialog.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using Mono.Unix;
using Gtk;
using Glade;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.MediaEngine;

namespace Banshee.Gui
{
    public class SeekDialog : GladeDialog
    {
        [Widget] private VBox seek_box;
        private StreamPositionLabel stream_position_label;
        private SeekSlider seek_slider;
        
        public SeekDialog() : base("SeekDialog")
        {
            PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
            PlayerEngineCore.StateChanged += OnPlayerEngineStateChanged;
            
            seek_slider = new SeekSlider();
            seek_slider.SeekRequested += OnSeekRequested;
            seek_slider.SeekValue = PlayerEngineCore.Position;
            seek_slider.Duration = PlayerEngineCore.Length;
            
            stream_position_label = new StreamPositionLabel(seek_slider);
            stream_position_label.FormatString = "<big>{0}</big>";
            
            seek_box.PackStart(seek_slider, false, false, 0);
            seek_box.PackStart(stream_position_label, false, false, 0);
            
            seek_box.ShowAll();
            
            Dialog.SetSizeRequest(300, -1);
            Dialog.Destroyed += OnDialogDestroyed;
        }
        
        private void OnDialogDestroyed(object o, EventArgs args)
        {
            PlayerEngineCore.EventChanged -= OnPlayerEngineEventChanged;
            PlayerEngineCore.StateChanged -= OnPlayerEngineStateChanged;
        }
        
        private void OnSeekRequested(object o, EventArgs args)
        {
            PlayerEngineCore.Position = (uint)seek_slider.Value;
        }
        
        private void OnPlayerEngineStateChanged(object o, PlayerEngineStateArgs args)
        {
            switch(args.State) {
                case PlayerEngineState.Contacting:
                    stream_position_label.IsContacting = true;
                    seek_slider.SetIdle();
                    break;
                case PlayerEngineState.Loaded:
                    seek_slider.Duration = PlayerEngineCore.CurrentTrack.Duration.TotalSeconds;
                    break;
                case PlayerEngineState.Idle:
                    stream_position_label.IsContacting = false;
                    seek_slider.SetIdle();
                    break;
            }
        }
        
        private void OnPlayerEngineEventChanged(object o, PlayerEngineEventArgs args)
        {
            switch(args.Event) {
                case PlayerEngineEvent.Iterate:
                    seek_slider.CanSeek = PlayerEngineCore.CanSeek;
                    seek_slider.SeekValue = PlayerEngineCore.Position;
                    seek_slider.Duration = PlayerEngineCore.Length;
                    stream_position_label.IsContacting = false;
                    break;
                case PlayerEngineEvent.EndOfStream:
                    seek_slider.SetIdle();
                    break;
                case PlayerEngineEvent.StartOfStream:
                    //seek_slider.CanSeek = PlayerEngineCore.CanSeek;
                    seek_slider.CanSeek = true;
                    break;
                case PlayerEngineEvent.Buffering:
                    if(args.BufferingPercent >= 1.0) {
                        stream_position_label.IsBuffering = false;
                        break;
                    }
                    
                    stream_position_label.IsBuffering = true;
                    stream_position_label.BufferingProgress = args.BufferingPercent;
                    break;
            }
        }
    }
}
