/***************************************************************************
 *  TrackProperties.cs
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
using Gtk;
using Glade;

namespace Banshee
{
	public class TrackProperties
	{
		[Glade.WidgetAttribute] private Gtk.Window WindowTrackInfo;
		private Glade.XML glade;

		public TrackProperties(TrackInfo ti)
		{
			if(ti == null)
				return;
		
			glade = new Glade.XML(null, 
				"trackinfo.glade", "WindowTrackInfo", null);
			glade.Autoconnect(this);
			WindowTrackInfo.Icon = 
				Gdk.Pixbuf.LoadFromResource("banshee-icon.png");
			
			((Gtk.Image)glade["ImageAlbumCover"]).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("album-cover-container.png");
				
			glade["ButtonClose"].HasFocus = true;
			
			SetField("Artist", ti.Artist);
			SetField("Performer", ti.Performer);
			SetField("Album", ti.Album);
			SetField("Title", ti.Title);
			SetField("Genre", ti.Genre);
			//SetField("DateAdded", ti.DateAdded.ToString());
			SetField("Duration", String.Format("{0}:{1}",
				ti.Duration / 60, (ti.Duration % 60).ToString("00")));
			SetField("TrackNumber", ti.TrackNumber == 0 ? null : 
				ti.TrackNumber.ToString());
			SetField("TrackCount", ti.TrackCount == 0 ? null :
				ti.TrackCount.ToString());
				
			((Gtk.Label)glade["LabelMimeType"]).Text = ti.MimeType;
			((Gtk.Entry)glade["EntryUri"]).Text = StringUtil.UriEscape(ti.Uri);
		}
		
		private void SetField(string field, string val)
		{
			bool visible = val != null;
			
			if(val != null)
				((Gtk.Label)glade["Label" + field]).Text = val;
				
			glade["Label" + field].Visible = visible;
			glade["Title" + field].Visible = visible;
		}
		
		private void OnButtonCloseClicked(object o, EventArgs args)
		{
			WindowTrackInfo.Destroy();
		}
	}
}
