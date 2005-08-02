/***************************************************************************
 *  BurnCore.cs
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
using System.Threading;
using System.IO;
using Gtk;
using Nautilus;

namespace Sonance
{
	public class BurnCore
	{
		public enum DiskType : uint {
			Audio,
			Mp3,
			Data
		};
	
		private ArrayList encodeQueue = new ArrayList();
		private ArrayList burnQueue = new ArrayList();
		private DiskType diskType;
		private bool canceled;
		
		public BurnCore(DiskType diskType)
		{
			this.diskType = diskType;
		}
		
		public void AddTrack(TrackInfo track)
		{
			encodeQueue.Add(track);
		}
		
		public void Burn()
		{
			FileEncoder.EncodeFormat trackFormat;
			
			canceled = false;
			
			switch(diskType) {
				case DiskType.Audio:
					trackFormat = FileEncoder.EncodeFormat.Wav;
					break;
				case DiskType.Mp3:
					trackFormat = FileEncoder.EncodeFormat.Mp3;
					break;
				case DiskType.Data:
				default:
					foreach(TrackInfo ti in encodeQueue)
						burnQueue.Add(ti.Uri);
					DoBurn();
					return;
			}
		
			FileEncodeTransaction fet = new FileEncodeTransaction(trackFormat);
			fet.FileEncodeComplete += OnFileEncodeComplete;
			fet.Finished += OnFileEncodeTransactionFinished;
			fet.Canceled += OnFileEncodeTransactionCanceled;
			
			foreach(TrackInfo ti in encodeQueue)
				fet.AddTrack(ti);
				
			fet.Register();
		}
		
		private void OnFileEncodeComplete(object o, FileEncodeCompleteArgs args)
		{
			burnQueue.Add(args.EncodedFilePath);
		}
		
		private void OnFileEncodeTransactionFinished(object o, EventArgs args)
		{
			if(!canceled)
				DoBurn();
		}
		
		private void OnFileEncodeTransactionCanceled(object o, EventArgs args)
		{
			canceled = true;
		}
		
		private void DoBurn()
		{
			//BurnTransaction burner = new BurnTransaction(diskType, burnQueue);
			//burner.Register();
			
			new Burner(diskType, burnQueue);
		}
	}
	
	public class BurnWindow : Window
	{
		private ProgressBar progressBar;
		private Label statusLabel;
		private Button cancelButton;
		
		public event EventHandler Canceled;
		
		private uint timeoutId;
		private bool killPulse;
		private bool canCancel;
		
		public BurnWindow() : base("Sonance CD Burner")
		{
			VBox vboxWindow;
			VBox vboxInner;
			HBox hbox;
			Image image;
			
			HButtonBox buttonBox;
			
			BorderWidth = 12;
			SetSizeRequest(400, -1);
			Resizable = false;
			Icon =  Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			
			vboxWindow = new VBox();
			vboxWindow.Spacing = 12;
			
			vboxInner = new VBox();
			vboxInner.Spacing = 5;
			
			hbox = new HBox();
			hbox.Spacing = 10;
			vboxWindow.PackStart(hbox, true, true, 0);
			
			image = new Image(Gdk.Pixbuf.LoadFromResource("burn-icon.png"));
			image.Xalign = 0.5f;
			image.Yalign = 0.0f;
			hbox.PackStart(image, false, false, 0);
			
			statusLabel = new Label();
			statusLabel.Markup = "<big><b>Burning CD...</b></big>";
			statusLabel.Xalign = 0.0f;
			statusLabel.Yalign = 0.0f;
			vboxInner.PackStart(statusLabel, true, true, 0);
			
			progressBar = new ProgressBar();
			vboxInner.PackStart(progressBar, false, false, 0);	

			hbox.PackStart(vboxInner, true, true, 0);
			
			buttonBox = new HButtonBox();
			buttonBox.Layout = ButtonBoxStyle.End;
			vboxWindow.PackStart(buttonBox, false, false, 0);
			
			cancelButton = new Button("gtk-cancel");
			cancelButton.UseStock = true;
			cancelButton.Clicked += OnCancelClicked;
			buttonBox.Add(cancelButton);
			
			Add(vboxWindow);
			
			DeleteEvent += OnDeleteEvent;
			
			ShowAll();
			
			Fraction = -1.0;
		}
		
		public void Close()
		{
			killPulse = true;
			Destroy();
		}
		
		public void Cancel()
		{
			EventHandler handler = Canceled;
			if(handler != null)
				handler(this, new EventArgs());
				
			Close();
		}
		
		private void OnDeleteEvent(object o, DeleteEventArgs args) 
		{
			if(!canCancel) {
				args.RetVal = true;
				return;
			} else {
				args.RetVal = false;
				Cancel();
			}
		}
		
		private void OnCancelClicked(object o, EventArgs args)
		{
			ResponseType response;
			
			HigMessageDialog dialog = new HigMessageDialog(null,
				DialogFlags.Modal, 
				MessageType.Question,
				ButtonsType.YesNo,
				"Cancel CD Burn?",
				"Are you sure you want to cancel the CD Burn?");
				
			dialog.Title = "Cancel CD Burn?";
			dialog.Icon =  Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			dialog.DefaultResponse = ResponseType.No;
			response = (ResponseType)dialog.Run();
			dialog.Destroy();
			
			if(response == ResponseType.Yes)
				Cancel();
		}
		
		public string Header
		{
			set {
				statusLabel.Markup = "<big><b>" + value + "</b></big>";
			}
		}
		
		public string Message
		{
			set {
			
			}
		}
		
		public double Fraction
		{
			set {
 				if(value < 0) {
					if(timeoutId == 0) {
						killPulse = false;
						timeoutId = GLib.Timeout.Add(50, Pulseate);
					} 
				} else {
					if(timeoutId != 0) {
						killPulse = true;
						timeoutId = 0;
					}

					progressBar.Fraction = value;
				}
			}
		}
		
		private bool Pulseate()
		{
			if(killPulse)
				return false;
				
			if(progressBar.GetType() == typeof(ProgressBar))
				progressBar.Pulse();
				
			return true;
		}
		
		public bool CanCancel
		{
			set {
				canCancel = value;
				cancelButton.Sensitive = value;
			}
		}
	}
	
	public class Burner
	{
		private ArrayList burnQueue;
		private BurnCore.DiskType diskType;
		private BurnDrive drive;
		private BurnRecorder recorder;
		private BurnRecorderActions currentAction;
		private bool haveMedia;
		private BurnWindow window;
		private bool canceled;
		private Thread burnThread;
		private long TotalDuration;
		
		public Burner(BurnCore.DiskType diskType, ArrayList burnQueue)
		{
			this.diskType = diskType;
			this.burnQueue = burnQueue;
			
			window = new BurnWindow();
			window.Header = "Initializing Burner...";
			window.Canceled += OnCanceled;
			
			burnThread = new Thread(new ThreadStart(BurnThread));
			burnThread.Start();
		}
		
		private bool GetBoolPref(string key, bool def)
		{
			try {
				return (bool)Core.GconfClient.Get(key);
			} catch(Exception) {
				return def;
			}
		}
		
		private void Cancel()
		{
			canceled = true;
			if(recorder != null && haveMedia)
				recorder.Cancel(false);
		
			Core.ThreadEnter();
			window.Close();
			Core.ThreadLeave();	
			
			if(burnThread == null)
				return;
			
			burnThread.Join(new TimeSpan(0, 0, 1));
			
			if(burnThread.IsAlive) {
				try {
					burnThread.Abort();
				} catch(Exception) {}
			}
		}
		
		private void OnCanceled(object o, EventArgs args) 
		{
			Cancel();
		}
		
		private bool HaveRequiredSpace() 
		{
			long available = (long)(((drive.MediaSize 
				/ 1024 / 1024) - 1) * 48 / 7);
			long remaining = (long)(available - TotalDuration);

			if(remaining < 0) {
				Core.ThreadEnter();
				HigMessageDialog dialog = new HigMessageDialog(null,
					DialogFlags.Modal, 
					MessageType.Error,
					ButtonsType.Ok,
					"Not Enough Space",
					String.Format("The inserted media is not large enough " +
						"to hold your selected music. {0} more minutes " + 
						"are needed on the media.", -remaining / 60));

				dialog.Title = "Not Enough Space";
				dialog.Icon =  Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
				dialog.DefaultResponse = ResponseType.Ok;
				dialog.Run();
				dialog.Destroy();
			
				Core.ThreadLeave();
				return false;
			}

			return true;
		}
		
		private void BurnThread()
		{
			ArrayList tracks = new ArrayList();
	
			try {
				if(recorder == null) {
					recorder = new BurnRecorder();
					recorder.ProgressChanged += OnProgressChanged;
					recorder.ActionChanged += OnActionChanged;
					recorder.InsertMediaRequest += OnInsertMediaRequest;
					recorder.WarnDataLoss += OnWarnDataLoss;
				}
							
				haveMedia = true;
				
				string selectedBurnerId = (string)Core.GconfClient.Get(
						GConfKeys.CDBurnerId);
				string burnKeyParent = GConfKeys.CDBurnerRoot 
					+ selectedBurnerId + "/";
					
				drive = BurnUtil.GetDriveByIdOrDefault(selectedBurnerId);
				
				foreach(string file in burnQueue)
					tracks.Add(new BurnRecorderTrack(file, 
						diskType == BurnCore.DiskType.Audio ?
							BurnRecorderTrackType.Audio :
							BurnRecorderTrackType.Data));

				BurnRecorderWriteFlags flags = BurnRecorderWriteFlags.Debug;
				
				if(GetBoolPref(burnKeyParent + "Eject", true))
					flags |= BurnRecorderWriteFlags.Eject;
				
				if(GetBoolPref(burnKeyParent + "DAO", true))
					flags |= BurnRecorderWriteFlags.DiscAtOnce;
				
				if(GetBoolPref(burnKeyParent + "Overburn", false)) 
					flags |= BurnRecorderWriteFlags.Overburn;
				
				if(GetBoolPref(burnKeyParent + "Simulate", false))
					flags |= BurnRecorderWriteFlags.DummyWrite;
				
				if(GetBoolPref(burnKeyParent + "Burnproof", true))
					flags |= BurnRecorderWriteFlags.Burnproof;

				BurnRecorderResult result = (BurnRecorderResult)
					recorder.WriteTracks(drive,
					tracks.ToArray(typeof(BurnRecorderTrack)) 
						as BurnRecorderTrack [],
					drive.MaxSpeedWrite, flags);
				
				if(result == BurnRecorderResult.Error) {
					string header = recorder.ErrorMessage;
					string message = recorder.ErrorMessageDetails;
					if(header == null || header.Equals(String.Empty))
						header = "Error Burning CD";
					if(message == null || message.Equals(String.Empty))
						message = "An unknown error occurred when " + 
							"attempting to write the CD";
					ShowError(header, message);
				} else if(result != BurnRecorderResult.Cancel) {
					ShowSuccess();
				}
			} catch(Exception e) {
				if(e.GetType() == typeof(ThreadAbortException))
					return;
					
				ShowError("Error Burning CD", e.Message);	
			} finally {
				foreach(string file in Directory.GetFiles(Paths.TempDir))
					File.Delete(file); 
			}
		}
		
		private void OnWarnDataLoss(object o, WarnDataLossArgs args)
		{
			ResponseType response;
			
			HigMessageDialog dialog = new HigMessageDialog(null,
				DialogFlags.Modal, 
				MessageType.Info,
				ButtonsType.OkCancel,
				"Data Loss Warning",
				"Attempting to burn this collection may result in data " + 
				"loss. The selected collection may not fit on the media.\n\n" +
				"Would you like to continue?");
				
			dialog.Title = "Data Loss Warning";
			dialog.Icon =  Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			dialog.DefaultResponse = ResponseType.Ok;
			response = (ResponseType)dialog.Run();
			
			if(response == ResponseType.Cancel)
				Cancel();
		}
		
		private void OnProgressChanged(object o, ProgressChangedArgs args) 
		{
			if(currentAction == BurnRecorderActions.Writing) {
				Core.ThreadEnter();
				window.Fraction = args.Fraction;
				Core.ThreadLeave();
			}
		}
		
		private void OnActionChanged(object o, ActionChangedArgs args) 
		{
			currentAction = args.Action;

			Core.ThreadEnter();
			switch(currentAction) {
				case BurnRecorderActions.PreparingWrite:
					window.Header = "Preparing to write...";
					window.CanCancel = true;
					break;
				case BurnRecorderActions.Writing:
					window.Header = "Writing disk...";
					window.CanCancel = true;
					break;
				case BurnRecorderActions.Fixating:
					window.Header = "Fixating disk...";
					window.CanCancel = false;
					break;
			}
			Core.ThreadLeave();
		}

		private void OnInsertMediaRequest(object o, 
			InsertMediaRequestArgs args)
		{
			ResponseType response;
			
			if(canceled)
				return;
			
			Core.ThreadEnter();
			window.Header = "Waiting for media...";
			Core.ThreadLeave();
			
			haveMedia = false;
			window.CanCancel = false;
			
			do {
				Core.ThreadEnter();
			
				HigMessageDialog dialog = new HigMessageDialog(null,
					DialogFlags.Modal, 
					MessageType.Info,
					ButtonsType.OkCancel,
					"Insert Blank CD",
					"Please insert a blank CD disk for the burn process.");
				
				dialog.Title = "Insert Blank CD";
				dialog.Icon =  Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
				dialog.DefaultResponse = ResponseType.Ok;
				response = (ResponseType)dialog.Run();
				dialog.Destroy();

				while(Application.EventsPending())
					Application.RunIteration();
				
				Core.ThreadLeave();
			} while(drive.MediaSize <= 0 && response == ResponseType.Ok);

			if(response != ResponseType.Ok)
				Cancel();
			else 
				haveMedia = true;
		}
		
		private void ShowError(string header, string message)
		{
			Core.ThreadEnter();
			
			if(window != null) {
				window.Close();
				window = null;
			}
		
			HigMessageDialog dialog = new HigMessageDialog(null,
				DialogFlags.Modal, 
				MessageType.Error,
				ButtonsType.Ok,
				header,
				message);

			dialog.Title = "Error Burning Disk";
			dialog.Icon =  Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			dialog.DefaultResponse = ResponseType.Ok;
			dialog.Run();
			dialog.Destroy();
			
			Core.ThreadLeave();
		}
		
		private void ShowSuccess()
		{
			Core.ThreadEnter();
			
			if(window != null) {
				window.Close();
				window = null;
			}
			
			HigMessageDialog dialog = new HigMessageDialog(null,
				DialogFlags.Modal, 
				MessageType.Info,
				ButtonsType.Ok,
				"CD Burning Complete",
				"The selected audio was successfully written to the CD.");

			dialog.Title = "CD Burning Complete";
			dialog.Icon =  Gdk.Pixbuf.LoadFromResource("sonance-icon.png");
			dialog.DefaultResponse = ResponseType.Ok;
			dialog.Run();
			dialog.Destroy();
			
			Core.ThreadLeave();
		}
	}
}

