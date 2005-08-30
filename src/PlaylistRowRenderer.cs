/***************************************************************************
 *  PlaylistRowRenderer.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.IO;
using System.Collections;
using Mono.Unix;
using Gdk;
using Pango;
using Gtk;

namespace Banshee
{
	public class PlaylistRowRenderer : CellRenderer
	{
		public bool Playing = false;
		public TrackInfo Track;
		
		private const int cellHeight = 38;
		private const int yOffset = 3;
		private const int trackNumberLeft = 5;
		private const int trackNumberWidth = 25;
		private const int trackTitleLeft = trackNumberWidth + 10;
		private const int secondRowOffset = 18;
		private const int coverOffset = 80;
		private const int rightOffset = coverOffset + 10;
		private int playCountWidth;
		
		private Hashtable svgNumbers = new Hashtable();
		
		public PlaylistRowRenderer()
		{
			
		}

		protected PlaylistRowRenderer(System.IntPtr ptr) : base(ptr)
		{
		
		}
		
		~PlaylistRowRenderer()
		{
			Dispose();
		}
		
		private StateType RendererStateToWidgetState(CellRendererState flags)
		{
			StateType state = StateType.Normal;
			
			if((CellRendererState.Insensitive & flags).Equals(
				CellRendererState.Insensitive)) {
				state = StateType.Insensitive;
			} else if((CellRendererState.Selected & flags).Equals( 
				CellRendererState.Selected)) {
				state = StateType.Selected;
			}
			
			return state;
		}
		
		protected override void Render(Gdk.Drawable drawable, 
			Widget widget, Gdk.Rectangle background_area, 
			Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, 
			CellRendererState flags)
		{
			Gdk.Window window = drawable as Gdk.Window;
			StateType state = RendererStateToWidgetState(flags);
			
			DrawPlayCount(window, widget, cell_area, state);
			DrawTrackNumber(window, widget, cell_area, state);
			DrawTrackTitle(window, widget, cell_area, state);
			DrawTrackArtistAlbum(window, widget, cell_area, state);
			DrawTrackDuration(window, widget, cell_area, state);
			DrawAlbumCover(window, widget, cell_area, state);
		}
		
		public override void GetSize(Widget widget, ref Gdk.Rectangle cell_area, 
			out int x_offset, out int y_offset, out int width, out int height)
		{
			height = cellHeight;
			width = 0;
			x_offset = 0;
			y_offset = 0;
		}
		
		private Pango.Layout GetPlainTextLayout(Gtk.Widget widget, 
			Pango.Weight weight, string text)
		{
			Pango.Layout layout = new Pango.Layout(widget.PangoContext);
			FontDescription fd = widget.PangoContext.FontDescription.Copy();
			fd.Weight = weight;
			layout.FontDescription = fd;
			layout.SetMarkup(text);
			return layout;
		}
		
		private static Gdk.Color ColorBlend(Gdk.Color a, Gdk.Color b)
		{
			// at some point, might be nice to allow any blend?
			double blend = 0.5;
		
			if(blend < 0.0 || blend > 1.0)
				throw new ApplicationException("blend < 0.0 || blend > 1.0");
		
			double blendRatio = 1.0 - blend;
		
			int aR = a.Red >> 8;
			int aG = a.Green >> 8;
			int aB = a.Blue >> 8;
			
			int bR = b.Red >> 8;
			int bG = b.Green >> 8;
			int bB = b.Blue >> 8;
			
			double mR = aR + bR;
			double mG = aG + bG;
			double mB = aB + bB;
			
			double blR = mR * blendRatio;
			double blG = mG * blendRatio;
			double blB = mB * blendRatio;
			
			Gdk.Color color = new Gdk.Color((byte)blR, (byte)blG, (byte)blB);
			Gdk.Colormap.System.AllocColor(ref color, true, true);
			return color;
		}
		
		private void DrawTrackNumber(Gdk.Window canvas, Gtk.Widget widget, 
			Gdk.Rectangle area, StateType state)
		{	
			string text = 
				String.Format("<span size=\"large\">{0}.</span>", 
				Track.TrackNumber);
			int width, height;
			
			Pango.Layout layout = GetPlainTextLayout(widget, 
			Pango.Weight.Normal, text);
			
			layout.GetPixelSize(out width, out height);
		
			Gdk.GC modGC = widget.Style.TextGC(state);
			if(!state.Equals(StateType.Selected)) {
				modGC = new Gdk.GC(canvas);
				modGC.Copy(widget.Style.TextGC(state));
				Gdk.Color fgcolor = widget.Style.Foreground(state);
				Gdk.Color bgcolor = widget.Style.Background(state);
				modGC.RgbFgColor = ColorBlend(fgcolor, bgcolor);
			} 
			
			canvas.DrawLayout(
				modGC,
				trackNumberLeft + trackNumberWidth - width, 
				area.Y + yOffset, layout);		
		}
		
		private void DrawPlayCount(Gdk.Window canvas, Gtk.Widget widget, 
			Gdk.Rectangle area, StateType state)
		{	
			string plays = Catalog.GetPluralString("{0} Play", "{0} Plays", (int)Track.NumberOfPlays);
			string text = 
				String.Format("<span size=\"small\">" + plays + "</span>", 
					      Track.NumberOfPlays);
			int width, height;
			
			Pango.Layout layout = GetPlainTextLayout(widget, 
			Pango.Weight.Normal, text);
			
			layout.GetPixelSize(out width, out height);
		
			Gdk.GC modGC = widget.Style.TextGC(state);
			if(!state.Equals(StateType.Selected)) {
				modGC = new Gdk.GC(canvas);
				modGC.Copy(widget.Style.TextGC(state));
				Gdk.Color fgcolor = widget.Style.Foreground(state);
				Gdk.Color bgcolor = widget.Style.Background(state);
				modGC.RgbFgColor = ColorBlend(fgcolor, bgcolor);
			} 
			
			canvas.DrawLayout(
				modGC,
				area.Width - coverOffset - width - 10, 
				area.Y + yOffset + 3, layout);
				
			playCountWidth = width;		
		}
		
		private void DrawTrackTitle(Gdk.Window canvas, Gtk.Widget widget, 
			Gdk.Rectangle area, StateType state)
		{
			string text = StringUtil.EntityEscape(Track.DisplayTitle);
			
			Pango.Layout layout = GetPlainTextLayout(widget, 
				Pango.Weight.Normal, "<span size=\"large\">" + text + "</span>");
			
			int maxDspWidth = (area.Width - trackTitleLeft) - 
				rightOffset - playCountWidth - 5;
			int dspWidth, dspHeight;
			
			while(true) {
				layout.GetPixelSize(out dspWidth, out dspHeight);
				if(dspWidth <= maxDspWidth)
					break;
				
				int mid = text.Length / 2;
				if(mid == 0)
					return;
					
				string left = text.Substring(0, mid);
				string right = text.Substring(mid + 1, text.Length - mid - 1);
				text = left + right;
				//text = text.Substring(0, text.Length - 1);
				
				layout.SetMarkup("<span size=\"large\">" + 
					left.Trim() + "..." + right.Trim() + "</span>");
				
				text = text.Trim();
				if(text.Length <= 0)
					return;		
			} 
		
			canvas.DrawLayout(
				widget.Style.TextGC(state),
				trackTitleLeft, area.Y + yOffset, layout);
		}
		
		private void DrawTrackArtistAlbum(Gdk.Window canvas, Gtk.Widget widget,
			Gdk.Rectangle area, StateType state)
		{
			string artist = StringUtil.EntityEscape(Track.DisplayArtist);
			string album = StringUtil.EntityEscape(Track.DisplayAlbum);	
			string text = String.Format("{0}  /  {1}", artist, album);
			bool ellipsize = false;
			
			Pango.Layout layout = GetPlainTextLayout(widget,
				Pango.Weight.Normal, text);
			
			int maxDspWidth = (area.Width - trackTitleLeft) - 
				rightOffset - playCountWidth - 5;;
			int dspWidth, dspHeight;
			
			while(true) {
				layout.GetPixelSize(out dspWidth, out dspHeight);
				if(dspWidth <= maxDspWidth)
					break;

				text = text.Substring(0, text.Length - 1);
				
				text = text.Trim();
				if(text.Length <= 0)
					return;
				
				layout.SetMarkup(text + "...");
				ellipsize = true;
			}
			
			if(text.IndexOf("  /  ") >= 0) {
				text = text.Replace("  /  ", "  /  <i>") + "</i>";
				layout.SetMarkup(text + (ellipsize ? "..." : ""));
			}
			
			canvas.DrawLayout(
				widget.Style.TextGC(state),
				trackTitleLeft, area.Y + yOffset + secondRowOffset, layout);			
		}
		
		private void DrawTrackDuration(Gdk.Window canvas, Gtk.Widget widget,
			Gdk.Rectangle area, StateType state)
		{		
			Pango.Layout layout = GetPlainTextLayout(widget, 
				Pango.Weight.Normal, String.Format("{0}:{1}", Track.Duration / 60, 
				(Track.Duration % 60).ToString("00")));
			
			int width, height;
			
			layout.GetPixelSize(out width, out height);
			
			canvas.DrawLayout(
				widget.Style.TextGC(state),
				area.Width - width, area.Y + yOffset, layout);
		}
		
		private void DrawAlbumCover(Gdk.Window canvas, Gtk.Widget widget,
			Gdk.Rectangle area, StateType state)
		{
			Pixbuf cover = new Pixbuf("/home/aaron/Desktop/cover.jpg");			
			canvas.DrawPixbuf(widget.Style.TextGC(state), cover, 0, 0,
				area.Width - coverOffset, area.Y + yOffset,
				cover.Width, cover.Height,
				RgbDither.None, 0, 0);
		}
	}
}
