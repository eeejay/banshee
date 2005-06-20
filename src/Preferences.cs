/***************************************************************************
 *  Preferences.cs
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
using System.Collections;
using Gtk;
using Glade;

namespace Sonance
{
	public class PreferencesWindow
	{
		[Widget] private Window WindowPreferences;
		[Widget] private TextView LibraryLocationEntry;
		[Widget] private CheckButton CopyOnImport;
		[Widget] private RadioButton RadioImport;
		[Widget] private RadioButton RadioAppend;
		[Widget] private RadioButton RadioAsk;
		[Widget] private TreeView TreeMimeSynonyms;
		[Widget] private TreeView TreeDecoders;
		
		string oldLibraryLocation;

		public PreferencesWindow()
		{
			Glade.XML glade = new Glade.XML(null, 
				"preferences.glade", "WindowPreferences", null);
			glade.Autoconnect(this);
			
			((Image)glade["ImageLibraryTab"]).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("library-icon-32.png");
					
			WindowPreferences.Icon = 
				Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
				
			LibraryLocationEntry.HasFocus = true;
			LoadPreferences();
		}
		
		private void OnButtonCancelClicked(object o, EventArgs args)
		{
			WindowPreferences.Destroy();
		}
		
		private void OnButtonOkClicked(object o, EventArgs args)
		{
			SavePreferences();
			WindowPreferences.Destroy();
		}
		
		private void OnButtonLibraryChangeClicked(object o, EventArgs args)
		{
			FileChooserDialog chooser = new FileChooserDialog(
				"Select Sonance Library Location",
				null,
				FileChooserAction.SelectFolder,
				"gnome-vfs"
			);
			
			chooser.AddButton(Stock.Open, ResponseType.Ok);
			chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
			chooser.DefaultResponse = ResponseType.Ok;
			
			if(chooser.Run() == (int)ResponseType.Ok) {
				LibraryLocationEntry.Buffer.Text = chooser.Filename;
			}
			
			chooser.Destroy();
		}
		
		private void OnButtonLibraryResetClicked(object o, EventArgs args)
		{
			LibraryLocationEntry.Buffer.Text = Paths.DefaultLibraryPath;
		}
		
		private void LoadPreferences()
		{
			oldLibraryLocation = Paths.DefaultLibraryPath;
			
			try {
				oldLibraryLocation = (string)Core.GconfClient.Get(
						GConfKeys.LibraryLocation);
			} catch(Exception) { }
			
			LibraryLocationEntry.Buffer.Text = oldLibraryLocation;
			
			try {
				CopyOnImport.Active = (bool)Core.GconfClient.Get(
					GConfKeys.CopyOnImport);
			} catch(Exception) {
				CopyOnImport.Active = true;
			}	
		}
		
		private void SavePreferences()
		{
			string newLibraryLocation = LibraryLocationEntry.Buffer.Text;
		
			if(!oldLibraryLocation.Trim().Equals(newLibraryLocation.Trim())) {
				Core.GconfClient.Set(GConfKeys.LibraryLocation,
					newLibraryLocation);
				// TODO: Move Library Directory?
			}
			
			Core.GconfClient.Set(GConfKeys.CopyOnImport,
				CopyOnImport.Active);
		}		
	}
}
