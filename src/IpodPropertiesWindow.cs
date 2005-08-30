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
using Mono.Unix;
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
		private IpodSource source;
		private Device device;
		private Entry nameEntry;
		private Entry userEntry;
		private Entry hostEntry;
		
		private bool edited;
		
		public IpodPropertiesDialog(IpodSource source) : base(
			// Translators: {0} is the name assigned to an iPod by its owner
			String.Format(Catalog.GetString("{0} Properties"), source.Device.Name),
			null,
			DialogFlags.Modal | DialogFlags.NoSeparator,
			Stock.Close,
			ResponseType.Close)
		{
			this.source = source;
			this.device = source.Device;
			
			VBox box = new VBox();
			box.Spacing = 10;
			
			PropertyTable table = new PropertyTable();
			table.ColumnSpacing = 10;
			table.RowSpacing = 5;
			
			if(device.CanWrite) {
				nameEntry = table.AddEntry(Catalog.GetString("iPod Name"), device.Name);
				userEntry = table.AddEntry(Catalog.GetString("Your Name"), device.UserName);
				hostEntry = table.AddEntry(Catalog.GetString("Computer Name"), device.HostName);
				
				nameEntry.Changed += OnEntryChanged;
				userEntry.Changed += OnEntryChanged;
				hostEntry.Changed += OnEntryChanged;
			} else {
				table.AddLabel(Catalog.GetString("iPod Name"), device.Name);
				table.AddLabel(Catalog.GetString("Your Name"), device.UserName);
				table.AddLabel(Catalog.GetString("Computer Name"), device.HostName);
			}
			
			table.AddSeparator();
	
			table.AddLabel(Catalog.GetString("Model"), device.ModelNumber != null ? 
				device.ModelNumber + " (" + device.Model.ToString() + ")" : 
				device.Model.ToString());
			table.AddLabel(Catalog.GetString("Capacity"), device.AdvertisedCapacity);
			table.AddWidget(Catalog.GetString("Volume Usage"), UsedProgressBar);
	
			box.PackStart(table, true, true, 0);
			
			PropertyTable extTable = new PropertyTable();
			extTable.ColumnSpacing = 10;
			extTable.RowSpacing = 5;
			extTable.AddLabel(Catalog.GetString("Mount Point"), device.MountPoint);
			extTable.AddLabel(Catalog.GetString("Device Node"), device.DevicePath);
			extTable.AddLabel(Catalog.GetString("Write Support"),
					  device.CanWrite ? Catalog.GetString("Yes") : Catalog.GetString("No"));
			extTable.AddLabel(Catalog.GetString("Volume UUID"), device.VolumeUuid);
			extTable.AddLabel(Catalog.GetString("Serial Number"), device.SerialNumber);
			extTable.AddLabel(Catalog.GetString("Firmware Version"), device.FirmwareVersion);
			extTable.AddLabel(Catalog.GetString("Database Version"), device.SongDatabase.Version);
			
			Expander expander = new Expander(Catalog.GetString("Advanced Details"));
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
				ProgressBar usedBar = new ProgressBar();
				usedBar.Fraction = source.DiskUsageFraction;
				usedBar.Text = source.DiskUsageString + " (" +
					source.DiskAvailableString + ")";
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
