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
using System.Threading;
using Gtk;
using Glade;

using Nautilus;

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
		
		private string oldLibraryLocation;
		
		private string selectedBurnerId;
		private string burnKeyParent;
		private BurnDrive selectedDrive;

		private Glade.XML glade;
		
		private Label driveLoadingLabel;
		private ComboBox burnerDrivesCombo;
		private ComboBox writeSpeedCombo;
		private HBox driveContainer;
		private HBox speedContainer;
		
		private BurnDrive [] burnDevices;

		public PreferencesWindow()
		{
			glade = new Glade.XML(null, 
				"preferences.glade", "WindowPreferences", null);
			glade.Autoconnect(this);
			
			((Image)glade["ImageLibraryTab"]).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("library-icon-32.png");
				
			((Image)glade["ImageBurningTab"]).Pixbuf = 
				Gdk.Pixbuf.LoadFromResource("burn-icon-32.png");
					
			WindowPreferences.Icon = 
				Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
				
			driveContainer = glade["DriveComboContainer"] as HBox;
			speedContainer = glade["SpeedComboContainer"] as HBox;
				
			writeSpeedCombo = ComboBox.NewText();
			speedContainer.PackStart(writeSpeedCombo, false, false, 0);
			speedContainer.ShowAll();
				
			LibraryLocationEntry.HasFocus = true;
			LoadPreferences();
			LoadBurnerDrives();
		}
		
		private bool GetBoolPref(string key, bool def)
		{
			try {
				return (bool)Core.GconfClient.Get(key);
			} catch(Exception) {
				return def;
			}
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
			
			try {
				selectedBurnerId = (string)Core.GconfClient.Get(
					GConfKeys.CDBurnerId);
			} catch(Exception) {}
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
				
			SaveBurnSettings();
		}
		
		private void ThreadLoadBurnerDrives()
		{
			burnDevices = BurnUtil.GetDrives();

			if(burnDevices == null || burnDevices.Length == 0) {
				Core.ThreadEnter();
				ShowBurnerWidgets(false);
				driveLoadingLabel.Markup = "<i>No CD Burners Detected</i>";
				Core.ThreadLeave();
				return;
			} 
			
			Core.ThreadEnter();
			
			ShowBurnerWidgets(true);
			
			burnerDrivesCombo = new ComboBox();
			burnerDrivesCombo.Changed += OnBurnerDriveComboChanged;
			ListStore burnerDrivesModel = new ListStore(typeof(string), 
				typeof(string), typeof(string));
			CellRendererText rendererName = new CellRendererText();
			CellRendererText rendererDevice = new CellRendererText();
			burnerDrivesCombo.Model = burnerDrivesModel;
			burnerDrivesCombo.PackStart(rendererName, true);
			burnerDrivesCombo.PackEnd(rendererDevice, false);
			burnerDrivesCombo.SetAttributes(rendererName, "text", 0);
			burnerDrivesCombo.SetAttributes(rendererDevice, "text", 1);
			driveContainer.Remove(driveLoadingLabel);
			driveLoadingLabel = null;
			driveContainer.PackStart(burnerDrivesCombo, true, true, 0);
			driveContainer.ShowAll();
			
			TreeIter activeIter = TreeIter.Zero;
			
			for(int i = 0; i < burnDevices.Length; i++) {
				BurnDrive drive = burnDevices[i];
				string uid = BurnUtil.GetDriveUniqueId(drive);
				burnerDrivesModel.AppendValues(drive.DisplayName, 
					drive.Device + " ", uid);
				
				if(selectedBurnerId != null && 
					uid.Equals(selectedBurnerId)) {
					burnerDrivesCombo.Model.IterNthChild(
						out activeIter, i);	
				}
			}
			
			if(activeIter.Equals(TreeIter.Zero)) 
				burnerDrivesCombo.Model.GetIterFirst(out activeIter);
				
			SensitizeBurnerWidgets(true);
			burnerDrivesCombo.SetActiveIter(activeIter);
			
			Core.ThreadLeave();
		}
		
		private void LoadBurnerDrives()
		{	
			if(burnerDrivesCombo != null) {
				driveContainer.Remove(burnerDrivesCombo);
				burnerDrivesCombo = null;
			}

			while(writeSpeedCombo.Model.IterNChildren() > 0)
				writeSpeedCombo.RemoveText(0);
	
			writeSpeedCombo.AppendText("Unavailable");
			writeSpeedCombo.Active = 0;
			writeSpeedCombo.Sensitive = false;

			driveLoadingLabel = new Label();
			driveLoadingLabel.Ypad = 7;
			driveLoadingLabel.Markup = 
				"<i>Loading Drive List...</i>";
			driveLoadingLabel.Xalign = 0.0f;
			driveContainer.PackStart(driveLoadingLabel, false, false, 0);
			driveContainer.ShowAll();
			
			SensitizeBurnerWidgets(false);
			
			Thread th = new Thread(new ThreadStart(ThreadLoadBurnerDrives));
			th.Start();
		}
		
		private void SensitizeBurnerWidgets(bool sensitive)
		{
			glade["AdvancedFrame"].Sensitive = sensitive;
			glade["AudioRadio"].Sensitive = sensitive;
			glade["Mp3Radio"].Sensitive = sensitive;
			glade["DataRadio"].Sensitive = sensitive;
			glade["DiskFormatLabel"].Sensitive = sensitive;
			glade["WriteSpeedLabel"].Sensitive = sensitive;
		}
		
		private void ShowBurnerWidgets(bool visible)
		{
			glade["AdvancedFrame"].Visible = visible;
			glade["AudioRadio"].Visible = visible;
			glade["Mp3Radio"].Visible = visible;
			glade["DataRadio"].Visible = visible;
			glade["DiskFormatLabel"].Visible = visible;
			glade["WriteSpeedLabel"].Visible = visible;
			writeSpeedCombo.Visible = visible;
		}
		
		private void ChangeBurnerDrive(TreeIter iter)
		{
			ListStore drivesModel = burnerDrivesCombo.Model as ListStore;
			string driveId = drivesModel.GetValue(iter, 2) as string;
			BurnDrive drive = BurnDrive.Zero;	
		
			if(burnDevices == null || burnDevices.Length == 0)
				return;
				
			foreach(BurnDrive cdr in burnDevices) {
				if(BurnUtil.GetDriveUniqueId(cdr).Equals(driveId)) {
					drive = cdr.Copy();
					break;
				}
			}
					
			if(drive.Equals(BurnDrive.Zero)) {
				selectedDrive = BurnDrive.Zero;
				burnKeyParent = null;
				selectedBurnerId = null;
				return;
			}
				
			selectedBurnerId = BurnUtil.GetDriveUniqueId(drive);
			burnKeyParent = GConfKeys.CDBurnerRoot + selectedBurnerId + "/";
			selectedDrive = drive;
			
			writeSpeedCombo.Sensitive = true;
			while(writeSpeedCombo.Model.IterNChildren() > 0)
				writeSpeedCombo.RemoveText(0);
			
			writeSpeedCombo.AppendText("Fastest Possible");
			
			for(int speed = drive.MaxSpeedWrite; speed >= 2; speed -= 2) 
				writeSpeedCombo.AppendText(Convert.ToString(speed) + "x");
				
			writeSpeedCombo.Active = 0;
			
			Core.GconfClient.Set(GConfKeys.CDBurnerId, selectedBurnerId);

			(glade["AudioRadio"] as RadioButton).Active = GetBoolPref(
				burnKeyParent + "FormatAudio", true);
			(glade["Mp3Radio"] as RadioButton).Active = GetBoolPref(
				burnKeyParent + "FormatMp3", false);
			(glade["DataRadio"] as RadioButton).Active = GetBoolPref(
				burnKeyParent + "FormatData", false);
			
			(glade["EjectCheck"] as CheckButton).Active = GetBoolPref(
				burnKeyParent + "Eject", true);
			(glade["DAOCheck"] as CheckButton).Active = GetBoolPref(
				burnKeyParent + "DAO", false);
			(glade["OverburnCheck"] as CheckButton).Active = GetBoolPref(
				burnKeyParent + "Overburn", false);
			(glade["SimulateCheck"] as CheckButton).Active = GetBoolPref(
				burnKeyParent + "Simulate", false);
			(glade["BurnproofCheck"] as CheckButton).Active = GetBoolPref(
				burnKeyParent + "Burnproof", true);
				
			try {
				SetComboFromSpeed((int)Core.GconfClient.Get(
					burnKeyParent + "Speed"));
			} catch(Exception) {}
		}
		
		private void OnBurnerDriveComboChanged(object o, EventArgs args)
		{
			TreeIter iter;
			
			SaveBurnSettings();
			
			if(!burnerDrivesCombo.GetActiveIter(out iter))
				return;
			
			ChangeBurnerDrive(iter);
		}
		
		private int GetSpeedFromCombo()
		{
			if(selectedDrive.Equals(BurnDrive.Zero))
				return 0;
				
			int max = selectedDrive.MaxSpeedWrite;
			int index = writeSpeedCombo.Active;
			
			if(index-- == 0)
				return max;
				
			return max - (index * 2);
		}
		
		private void SetComboFromSpeed(int speed)
		{
			if(selectedDrive.Equals(BurnDrive.Zero))
				return;
		
			int max = selectedDrive.MaxSpeedWrite;
			
			if(speed % 2 != 0)
				speed--;
				
			if(speed <= 0)
				speed = 2;
			else if(speed > max)
				speed = max;
				
			writeSpeedCombo.Active = speed == max ? 0 : 
				-((speed - max) / 2) + 1;
		}
		
		private void SaveBurnSettings()
		{
			if(selectedBurnerId != null && burnKeyParent != null) {
				Core.GconfClient.Set(burnKeyParent + "FormatAudio", 
					(glade["AudioRadio"] as RadioButton).Active);
				Core.GconfClient.Set(burnKeyParent + "FormatMp3", 
					(glade["Mp3Radio"] as RadioButton).Active);
				Core.GconfClient.Set(burnKeyParent + "FormatData", 
					(glade["DataRadio"] as RadioButton).Active);
					
				Core.GconfClient.Set(burnKeyParent + "Eject",
					(glade["EjectCheck"] as CheckButton).Active);
				Core.GconfClient.Set(burnKeyParent + "DAO",
					(glade["DAOCheck"] as CheckButton).Active);
				Core.GconfClient.Set(burnKeyParent + "Overburn",
					(glade["OverburnCheck"] as CheckButton).Active);
				Core.GconfClient.Set(burnKeyParent + "Simulate",
					(glade["SimulateCheck"] as CheckButton).Active);
				Core.GconfClient.Set(burnKeyParent + "Burnproof",
					(glade["BurnproofCheck"] as CheckButton).Active);
					
				Core.GconfClient.Set(burnKeyParent + "Speed",
					GetSpeedFromCombo());
			}
		}
	}
}
