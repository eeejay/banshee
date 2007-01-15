/***************************************************************************
 *  CellRendererStation.cs
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
using Mono.Unix;
using Gtk;
using Gdk;
using Pango;

using Banshee.Base;
using Banshee.Playlists.Formats.Xspf;

namespace Banshee.Plugins.Radio
{
    public class CellRendererStation : CellRendererText
    {
        private static Gdk.Pixbuf radio_icon = Gdk.Pixbuf.LoadFromResource("radio.png");
        private static Gdk.Pixbuf playing_icon = IconThemeUtils.LoadIcon(16, "media-playback-start", Stock.MediaPlay);
        private static Gdk.Pixbuf loading_icon = IconThemeUtils.LoadIcon(16, "network-receive", Stock.Connect);
        private static Gdk.Pixbuf error_icon = IconThemeUtils.LoadIcon(16, Stock.DialogError);
        
        private StationModel model;
        
        public CellRendererStation(StationModel model)
        {
            this.model = model;
        }
        
        protected CellRendererStation(System.IntPtr ptr) : base(ptr)
        {
        }
        
        private StateType RendererStateToWidgetState(CellRendererState flags)
        {
            StateType state = StateType.Normal;
            
            if((CellRendererState.Selected & flags).Equals(CellRendererState.Selected)) {
                state = StateType.Selected;
            }
            
            return state;
        }
        
        public override void GetSize(Widget widget, ref Gdk.Rectangle cell_area,
            out int x_offset, out int y_offset, out int width, out int height)
        {
            int text_x, text_y, text_w, text_h;

            base.GetSize(widget, ref cell_area, out text_x, out text_y, 
                out text_w, out text_h);
                
            x_offset = 0;
            y_offset = 0;
            width = text_w + 16 + 10;
            height = text_h;
        }
        
        protected override void Render(Gdk.Drawable drawable, 
            Widget widget, Gdk.Rectangle background_area, 
            Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, 
            CellRendererState flags)
        {  
            StateType state = RendererStateToWidgetState(flags);
            int text_indent = 6;
            int text_layout_width, text_layout_height;
            Gdk.Pixbuf render_icon = radio_icon;
            Track track = null;
            RadioTrackInfo radio_track = null;
            string text = Text;
            
            if(widget is TreeView) {
                TreePath path;
                if((widget as TreeView).GetPathAtPos(cell_area.X, cell_area.Y, out path)) {
                    track = model.GetTrack(path);
                    radio_track = model.GetRadioTrackInfo(path);
                }
            }   
            
            FontDescription font_description = widget.PangoContext.FontDescription.Copy();
            
            if(playing_icon != null && track != null && PlayerEngineCore.CurrentTrack is RadioTrackInfo 
                && (PlayerEngineCore.CurrentTrack as RadioTrackInfo).XspfTrack.Title == track.Title
                && (PlayerEngineCore.CurrentTrack as RadioTrackInfo).XspfTrack.Annotation == track.Annotation) {
                render_icon = playing_icon;
                font_description.Style = Pango.Style.Normal;
                font_description.Weight = Pango.Weight.Bold;
            } else if(radio_track != null && radio_track.ParsingPlaylist) {
                render_icon = loading_icon;
                font_description.Style = Pango.Style.Italic;
                font_description.Weight = Pango.Weight.Normal;
                text = String.Format("{0}: {1}", Catalog.GetString("Loading"), Text);
            } else if(radio_track != null && radio_track.PlaybackError != TrackPlaybackError.None) {
                render_icon = error_icon;
                font_description.Style = Pango.Style.Italic;
                font_description.Weight = Pango.Weight.Normal;
                string prefix = null;
                
                switch(radio_track.PlaybackError) {
                    case TrackPlaybackError.ResourceNotFound:
                        prefix = Catalog.GetString("Missing");
                        break;
                    case TrackPlaybackError.CodecNotFound:
                        prefix = Catalog.GetString("No Codec");
                        break;
                    case TrackPlaybackError.Unknown:
                        prefix = Catalog.GetString("Unknown Error");
                        break;
                    default:
                        break;
                }
                
                if(prefix != null) {
                    text = String.Format("({0}) {1}", prefix, Text);
                }
                
                if(!(CellRendererState.Selected & flags).Equals(CellRendererState.Selected)) {
                    state = StateType.Insensitive;
                }
                
                if(track != null) {
                    track.Title = String.Empty;
                }
            } else {
                font_description.Style = Pango.Style.Normal;
                font_description.Weight = Pango.Weight.Normal;
            }
            
            Gdk.GC main_gc = widget.Style.TextGC(state);
            
            if(track != null) {
                drawable.DrawPixbuf(main_gc, render_icon, 0, 0, 
                cell_area.X - render_icon.Width, 
                cell_area.Y + ((cell_area.Height - render_icon.Height) / 2),
                render_icon.Width, render_icon.Height,
                RgbDither.None, 0, 0);
            } else {
                text_indent = 0;
            }
            
            Pango.Layout text_layout = new Pango.Layout(widget.PangoContext);
            text_layout.FontDescription = font_description;
            text_layout.SetMarkup(GLib.Markup.EscapeText(text));
            text_layout.GetPixelSize(out text_layout_width, out text_layout_height);
            
            drawable.DrawLayout(main_gc, 
                cell_area.X + text_indent,
                cell_area.Y + ((cell_area.Height - text_layout_height) / 2), 
                text_layout);
        }
    }
}
