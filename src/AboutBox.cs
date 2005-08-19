/***************************************************************************
 *  AboutBox.cs
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
using System.Reflection;	
using Gtk;
using Glade;
using Gdk;
using GLib;
using Pango;
using System.Text;

namespace Banshee
{
	public class AboutBox
	{
		[Widget] private Gtk.Window WindowAbout;
		[Widget] private TreeView VersionTreeView;
		[Widget] private Label LabelQuickInfo;
		
		private TreeStore VersionStore;

		public AboutBox()
		{
			Glade.XML glade = new Glade.XML(null, 
				"about.glade", "WindowAbout", null);
			glade.Autoconnect(this);
			WindowAbout.Icon = Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			(glade["CreditsContainer"] as Container).Add(new ScrollBox());
			glade["CreditsContainer"].ShowAll();
			(glade["AboutHeader"] as Gtk.Image).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("about-header.png");
			LoadAboutInfo();
			BuildVersionInfo();
		}
		
		private void OnButtonCloseClicked(object o, EventArgs args)
		{
			WindowAbout.Destroy();
		}
		
		private void LoadAboutInfo()
		{
			AssemblyName selAsm = Assembly.GetEntryAssembly().GetName();
			
			LabelQuickInfo.Markup = String.Format(
				"<span size=\"small\"><b>{0} v{1}.{2}.{3}</b> " + 
				"<span color=\"blue\">(http://banshee-project.org)</span></span>",
				StringUtil.UcFirst(selAsm.Name), selAsm.Version.Major, 
				selAsm.Version.Minor, selAsm.Version.Build
			);
		}
		
		private void BuildVersionInfo()
		{
			VersionTreeView.RulesHint = true;
			VersionTreeView.AppendColumn("Assembly Name", 
				new CellRendererText(), "text", 0);
			VersionTreeView.AppendColumn("Version", 
				new CellRendererText(), "text", 1);
			VersionTreeView.AppendColumn("Path", 
				new CellRendererText(), "text", 2);

			FillListView();

			VersionTreeView.Model = VersionStore;
        }

		void FillListView()
		{
			VersionStore = new TreeStore(typeof(string), 
				typeof(string), typeof(string));

			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
				string loc;
				AssemblyName name = asm.GetName();

				try {
					loc = System.IO.Path.GetFullPath(asm.Location);
				} catch(Exception) {
					loc = "dynamic";
				}

				VersionStore.AppendValues(name.Name, 
					name.Version.ToString(), loc);
			}

			VersionStore.SetSortColumnId(0, SortType.Ascending);
		}
	}
	
	public class ScrollBox : DrawingArea
	{
		private Pixbuf image;
		private Pixbuf gradTop;
		private Pixbuf gradBottom;
		private int scroll = 0;
		private int textHeight = 0;
		private int textWidth = 0;
		private int offsetX = 20;
		private Pango.Layout layout;
		private Pango.Layout shadowLayout;
		private AssemblyName selAsm;

		private uint TimerHandle;
		private bool unrealize;
	
		public ScrollBox()
		{
			image = Gdk.Pixbuf.LoadFromResource("about-footer.png");
			gradTop = Gdk.Pixbuf.LoadFromResource("about-grad-top.png");
			gradBottom = Gdk.Pixbuf.LoadFromResource("about-grad-bottom.png");
			
			scroll = -image.Height;
			
			SetSizeRequest(image.Width, image.Height);
			
			Realized += OnRealized;
			Unrealized += OnUnrealized;
			ExposeEvent += OnExposed;
			
			TimerHandle = GLib.Timeout.Add(50, new TimeoutHandler(ScrollDown));
			
			selAsm = Assembly.GetEntryAssembly().GetName();
		}

		private string ScrollText 
		{
			get {
				return 
					String.Format(
						"<b><big>Version</big>\n" + 
						"   {0} v{1}.{2}.{3}\n\n" + 
						"<big>License</big>\n" + 
						"   Released under the MIT License\n\n" + 
						"<big>Author</big>\n" + 
						"   Written by Aaron Bockover\n" + 
						"   www.banshee-project.org\n" + 
						"   aaron@aaronbock.net\n\n"  +
						"<big>Copyright</big>\n" +
						"   Copyright (C) 2005 Novell\n" + 
						"   Copyright (C) 2005 Aaron Bockover\n\n",
						StringUtil.UcFirst(selAsm.Name), selAsm.Version.Major, 
						selAsm.Version.Minor, selAsm.Version.Build
					) + 
					
					"<big>Development</big>\n" +
					"    Aaron Bockover (core)\n" +
					"    James Willcox (ipod-sharp)\n\n" +
					"<big>Graphics</big>\n" + 
					"    Garrett LeSage\n" +
					"    Aaron Bockover\n\n" +
					"<big>Thanks</big>\n" + 
					"    Larry Ewing\n" +
					"    Raphael Slinckx\n" +
					"    Evan Bockover\n" + 
					"    Jeff Tickle</b>";	
			}
		}
		
		private bool ScrollDown()
		{
			scroll++;
			QueueDrawArea(offsetX, 0, textWidth, image.Height);
			return !unrealize;
		}
		
		protected void OnExposed(object o, ExposeEventArgs args)
		{
			if(GdkWindow == null || unrealize || image == null || 
				gradTop == null || gradBottom == null)
				return;
			
			GdkWindow.DrawPixbuf(Style.BackgroundGC(StateType.Normal), 
				image, 0, 0, 0, 0, -1, -1, RgbDither.Normal,  0,  0);
			
			GdkWindow.DrawLayout(Style.TextGC(StateType.Normal), offsetX, 
				0 - scroll - 1, shadowLayout);
			
			GdkWindow.DrawLayout(Style.TextGC(StateType.Normal), offsetX + 2, 
				0 - scroll + 1, shadowLayout);
				
			GdkWindow.DrawLayout(Style.TextGC(StateType.Normal), offsetX + 1, 
				0 - scroll, layout);
					
			GdkWindow.DrawPixbuf(Style.BackgroundGC(StateType.Normal),
				gradTop, 0, 0, 0, 0, -1, -1, RgbDither.Normal, 0, 0);
				
			GdkWindow.DrawPixbuf(Style.BackgroundGC(StateType.Normal),
				gradBottom, 0, 0, 0, image.Height - gradBottom.Height, 
				-1, -1, RgbDither.Normal, 0, 0);
				
			if(scroll > textHeight)
				scroll = -scroll;
		}

		protected void OnRealized(object o, EventArgs args)
		{
			layout = new Pango.Layout(PangoContext);
			layout.SetMarkup("<span color=\"white\">" 
				+ ScrollText + "</span>");
			layout.GetPixelSize(out textWidth, out textHeight);
			
			shadowLayout = new Pango.Layout(PangoContext);
			shadowLayout.SetMarkup("<span color=\"black\">" 
				+ ScrollText + "</span>");
			shadowLayout.GetPixelSize(out textWidth, out textHeight);
		}
		
		protected void OnUnrealized(object o, EventArgs args)
		{
			unrealize = true;
		}
	}
}
