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

namespace Sonance
{
	public class AboutBox
	{
		[Widget] private Window WindowAbout;
		[Widget] private Image ImageSplash;
		[Widget] private TreeView VersionTreeView;
		[Widget] private Label AboutLabel;
		[Widget] private Label LabelQuickInfo;
		[Widget] private Label LabelCredits;
		
		private TreeStore VersionStore;

		public AboutBox()
		{
			Glade.XML glade = new Glade.XML(null, 
				"about.glade", "WindowAbout", null);
			glade.Autoconnect(this);
			ImageSplash.Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("sonance-splash.png");
			WindowAbout.Icon = Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			LoadAboutInfo();
			BuildVersionInfo();
			LoadCredits();
		}
		
		private void OnButtonCloseClicked(object o, EventArgs args)
		{
			WindowAbout.Destroy();
		}
		
		private void LoadAboutInfo()
		{
			AssemblyName selAsm = Assembly.GetEntryAssembly().GetName();

			AboutLabel.Markup = String.Format(
				"<span weight=\"bold\">Version</span>\n" + 
				"   {0} v{1}.{2}.{3}\n\n" + 
				"<span weight=\"bold\">License</span>\n" + 
				"   Released under the GNU General Public License\n\n" + 
				"<span weight=\"bold\">Author</span>\n" + 
				"   Written by Aaron Bockover\n" + 
				"   <span color=\"blue\">http://sonance.aaronbock.net</span>\n" + 
				"   <span color=\"blue\">aaron@aaronbock.net</span>\n\n"  +
				"<span weight=\"bold\">Copyright</span>\n" + 
				"   Copyright (C) 2005 Aaron Bockover\n",
				StringUtil.UcFirst(selAsm.Name), selAsm.Version.Major, 
				selAsm.Version.Minor, selAsm.Version.Build
			);
			
			LabelQuickInfo.Markup = String.Format(
				"<span size=\"small\"><b>{0} v{1}.{2}.{3}</b> " + 
				"<span color=\"blue\">(http://sonance.aaronbock.net/)</span>" + 
				"\nMuch thanks to the Mono and GStreamer projects!</span>",
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
		
		void LoadCredits()
		{
			LabelCredits.Markup = 
			"<b>Lead Developer</b>\n\n" +
			"\tAaron Bockover &lt;aaron@aaronbock.net&gt;\n\n" +
			"<b>Thanks to the following people</b> for bug\n" +
			"reports, feature requests, help on formulating\n" +
			"implementation ideas and getting Sonance to\n" + 
			"work on different distributions:\n\n" + 
			"\tJeff Tickle\n" +
			"\tChristoph Trassl\n" +
			"\tG. Vamsee Krishna\n" + 
			"\tMike Sears\n" +
			"\tMarko Sosic\n" +
			"\tAdam Bellinson (Thread)\n" +
			"\tMaciek (etoare)\n" + 
			"\tDerek Buranen\n" +
			"\tEvan Bockover\n";
		}
	}
}
