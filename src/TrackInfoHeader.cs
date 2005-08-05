/***************************************************************************
 *  TrackInfoHeader.cs
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
using System.Collections;
using Gtk;

namespace Banshee
{
	public class TrackInfoHeader : HBox
	{
		private Label artistLabel;
		private Label titleLabel;
		private Image imageAlbum;
	
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(TrackInfoHeader));
				return gtype;
			}
		}
		
		public TrackInfoHeader() : base()
		{
			ConstructWidget();
		}
		
		/*((Gtk.Image)gxml["ImageAlbum"]).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("album-cover-container.png");*/
		
		private void ConstructWidget()
		{
			Spacing = 5;
		
			// Metadata Table
			Table table = new Table(2, 2, false);
			table.Show();
			PackStart(table, true, true, 0);
			
			table.ColumnSpacing = 5;
			table.RowSpacing = 2;
			
			Image imageArtistIcon = new Image();
			imageArtistIcon.Show();
			imageArtistIcon.SetFromStock("icon-artist", 
				IconSize.Menu);
			imageArtistIcon.Xalign = 0.0f;
			imageArtistIcon.Yalign = 0.5f;
			
			Image imageTitleIcon = new Image();
			imageTitleIcon.Show();
			imageTitleIcon.SetFromStock("icon-title", 
				IconSize.Menu);
			imageTitleIcon.Xalign = 0.0f;
			imageTitleIcon.Yalign = 0.5f;
			
			artistLabel = new Label();
			artistLabel.Show();
			artistLabel.Xalign = 0.0f;
			artistLabel.Yalign = 0.5f;
			artistLabel.Selectable = true;
			
			titleLabel = new Label();
			titleLabel.Show();			
			titleLabel.Xalign = 0.0f;
			titleLabel.Yalign = 0.5f;
			titleLabel.Selectable = true;
			
			table.Attach(imageArtistIcon, 0, 1, 0, 1, 
				AttachOptions.Fill,
				AttachOptions.Expand | AttachOptions.Fill, 0, 0);
				
			table.Attach(imageTitleIcon, 0, 1, 1, 2, 
				AttachOptions.Fill,
				AttachOptions.Expand | AttachOptions.Fill, 0, 0);
			
			table.Attach(artistLabel, 1, 2, 0, 1,
				AttachOptions.Expand | AttachOptions.Fill, 
				(AttachOptions)0, 0, 0);
				
			table.Attach(titleLabel, 1, 2, 1, 2,
				AttachOptions.Expand | AttachOptions.Fill, 
				(AttachOptions)0, 0, 0);
			
			// Album Picture
			imageAlbum = new Image();
			imageAlbum.Show();
			imageAlbum.Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("album-cover-container.png");

			PackStart(imageAlbum, false, false, 5);
						
			System.Reflection.AssemblyName asm = 
				System.Reflection.Assembly.GetEntryAssembly().GetName();
			string version = String.Format("{0}.{1}", asm.Version.Major, 
				asm.Version.Minor);
						
			artistLabel.Markup = 
				"<span weight=\"bold\">Banshee Player</span>";
			titleLabel.Markup = String.Format(
				"<span size=\"small\">Version {0}</span>", version);
		}
		
		public string Artist 
		{
			set {
				artistLabel.Markup = "<b>" + 
					StringUtil.EntityEscape(value) + "</b>";
			}
		}
		
		public string Title
		{
			set {
				titleLabel.Text = value;
			}
		}
	}
}
