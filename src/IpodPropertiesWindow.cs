/***************************************************************************
 *  IpodPropertiesWindow.cs
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
using IPod;

namespace Banshee
{
	public class PropertyTable : Table
	{
		public PropertyTable() : base(1, 1, false)
		{
		
		}
		
		public void AddWidget(string key, Widget widget)
		{
			uint rows = NRows;
			Label keyLabel = new Label();
			keyLabel.Markup = "<b>" + key + "</b>:";
			keyLabel.Xalign = 0.0f;
			
			Attach(keyLabel, 0, 1, rows, rows + 1);
			Attach(widget, 1, 2, rows, rows + 1);
		}
		
		public void AddSeparator()
		{
			HSeparator sep = new HSeparator();
			Attach(sep, 0, 2, NRows, NRows + 1);
			sep.HeightRequest = 10;
		}
		
		public void AddLabel(string key, object value)
		{
			if(value == null)
				return;
				
			Label valLabel = new Label(value.ToString());
			valLabel.Xalign = 0.0f;
			valLabel.UseUnderline = false;
			valLabel.Selectable = true;
			
			AddWidget(key, valLabel);
		}
		
		public Entry AddEntry(string key, object value)
		{
			if(value == null)
				return null;
				
			Entry valEntry = new Entry();
			valEntry.Text = value.ToString();
			
			AddWidget(key, valEntry);
			
			return valEntry;
		}
	}

	public class IpodPropertiesDialog : Dialog
	{
		private Device device;
		private Entry nameEntry;
		private Entry userEntry;
		private Entry hostEntry;
		
		private bool edited;
		
		public IpodPropertiesDialog(Device device) : base(
			device.Name + " Properties",
			null,
			DialogFlags.Modal | DialogFlags.NoSeparator,
			Stock.Close,
			ResponseType.Close)
		{
			this.device = device;
			
			VBox box = new VBox();
			box.Spacing = 10;
			
			PropertyTable table = new PropertyTable();
			table.ColumnSpacing = 10;
			table.RowSpacing = 5;
			
			if(device.CanWrite) {
				nameEntry = table.AddEntry("iPod Name", device.Name);
				userEntry = table.AddEntry("Your Name", device.UserName);
				hostEntry = table.AddEntry("Computer Name", device.HostName);
				
				nameEntry.Changed += OnEntryChanged;
				userEntry.Changed += OnEntryChanged;
				hostEntry.Changed += OnEntryChanged;
			} else {
				table.AddLabel("iPod Name", device.Name);
				table.AddLabel("Your Name", device.UserName);
				table.AddLabel("Computer Name", device.HostName);
			}
			
			table.AddSeparator();
	
			table.AddLabel("Model", device.ModelNumber != null ? 
				device.ModelNumber + " (" + device.Model.ToString() + ")" : 
				device.Model.ToString());
			table.AddLabel("Capacity", device.AdvertisedCapacity);
			table.AddWidget("Volume Usage", UsedProgressBar);
	
			box.PackStart(table, true, true, 0);
			
			PropertyTable extTable = new PropertyTable();
			extTable.ColumnSpacing = 10;
			extTable.RowSpacing = 5;
			extTable.AddLabel("Mount Point", device.MountPoint);
			extTable.AddLabel("Device Node", device.DevicePath);
			extTable.AddLabel("Write Support", device.CanWrite ? "Yes" : "No");
			extTable.AddLabel("Volume UUID", device.VolumeUuid);
			extTable.AddLabel("Serial Number", device.SerialNumber);
			extTable.AddLabel("Firmware Version", device.FirmwareVersion);
			extTable.AddLabel("Database Version", device.SongDatabase.Version);
			
			Expander expander = new Expander("Advanced Details");
			expander.Add(extTable);
			box.PackStart(expander, false, false, 0);
			
			box.ShowAll();
			
			BorderWidth = 10;
			Resizable = false;
			VBox.Add(box);
		}
		
		private ProgressBar UsedProgressBar
		{
			get {
				ulong usedmb = device.VolumeUsed / (1024 * 1024);
				ulong availmb = device.VolumeAvailable / (1024 * 1024);
				ulong totalmb = device.VolumeSize / (1024 * 1024);

				string usedstr = usedmb >= 1024 ? (usedmb / 1024) + " GB" :
					usedmb + " MB";
				string availstr = availmb >= 1024 ? (availmb / 1024) + " GB" :
					availmb + " MB";
				string totalstr = totalmb >= 1024 ? (totalmb / 1024) + " GB" :
					totalmb + " MB";
					
				ProgressBar usedBar = new ProgressBar();
				double fraction = (double)device.VolumeUsed / 
					(double)device.VolumeSize;
					
				usedBar.Fraction = fraction;
				usedBar.Text = String.Format("{0} of {1} ({2} Available)",
					usedstr, totalstr, availstr);
					
				return usedBar;
			}
		}
		
		private void OnEntryChanged(object o, EventArgs args)
		{
			device.Name = nameEntry.Text;
			device.UserName = userEntry.Text;
			device.HostName = hostEntry.Text;
			edited = true;
		}
		
		public bool Edited
		{
			get {
				return edited;
			}
		}
	}
}
