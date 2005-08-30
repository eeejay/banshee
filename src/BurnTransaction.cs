/***************************************************************************
 *  BurnTransaction.cs
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
using Nautilus;
using Gtk;
/*
namespace Banshee
{
	public class BurnTransaction : LibraryTransaction
	{
		private ArrayList burnQueue;
		private BurnCore.DiskType diskType;
		private BurnDrive drive;
		private BurnRecorder recorder;
		private BurnRecorderActions currentAction;
		private bool haveMedia;

		public override string Name {
			get {
				return Catalog.GetString("Song Burner");
			}
		}	
		
		public BurnTransaction(BurnCore.DiskType diskType, ArrayList burnQueue)
		{
			this.diskType = diskType;
			this.burnQueue = burnQueue;
			
			
			showCount = false;
			statusMessage = Catalog.GetString("Initializing Burner...");
		}

		private bool GetBoolPref(string key, bool def)
		{
			try {
				return (bool)Core.GconfClient.Get(key);
			} catch(Exception) {
				return def;
			}
		}
		
		public override void Run()
		{
			ArrayList tracks = new ArrayList();
	
			try {
				if(recorder == null) {
					recorder = new BurnRecorder();
					recorder.ProgressChanged += OnProgressChanged;
					recorder.ActionChanged += OnActionChanged;
					recorder.InsertMediaRequest += OnInsertMediaRequest;
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
				
				if(GetBoolPref(burnKeyParent + "Eject", true)) {
					Console.WriteLine("FLAG |= EJECT");
					flags |= BurnRecorderWriteFlags.Eject;
				}
				
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
				
				totalCount = 0;
				currentCount = 0;
				
				if(result == BurnRecorderResult.Error) {
					string header = recorder.ErrorMessage;
					string message = recorder.ErrorMessageDetails;
					if(header == null || header.Equals(String.Empty))
						header = Catalog.GetString("Error Burning CD");
					if(message == null || message.Equals(String.Empty)) {
						message = Catalog.GetString(
							"An unknown error occurred when " + 
							"attempting to write the CD");
					}
					ShowError(header, message);
				} else if(result == BurnRecorderResult.Cancel) {
					ShowError(Catalog.GetString("CD Burning Canceled"), 
						  Catalog.GetString("The CD Burning was canceled."));
				} else {
					ShowSuccess();
				}
			} catch(Exception e) {
				ShowError(Catalog.GetString("Error Burning CD"), e.Message);	
			} finally {
				foreach(string file in Directory.GetFiles(Paths.TempDir))
					File.Delete(file); 
			}
		}
		
		protected override void CancelAction()
		{
			if(recorder != null && haveMedia)
				recorder.Cancel(false);
		}
		
		private void OnProgressChanged(object o, ProgressChangedArgs args) 
		{
			if(currentAction == BurnRecorderActions.Writing) {
				totalCount = 1000;
				currentCount = (long)(args.Fraction * 1000.0);
			}
		}

		private void OnActionChanged(object o, ActionChangedArgs args) 
		{
			currentAction = args.Action;

			switch(currentAction) {
				case BurnRecorderActions.PreparingWrite:
					statusMessage = Catalog.GetString("Preparing to write...");
					break;
				case BurnRecorderActions.Writing:
					statusMessage = Catalog.GetString("Writing disk...");
					break;
				case BurnRecorderActions.Fixating:
					statusMessage = Catalog.GetString("Fixating disk...");
					break;
			}
		}

		private void OnInsertMediaRequest(object o, InsertMediaRequestArgs args)
		{
			ResponseType response;
			
			statusMessage = Catalog.GetString("Waiting for media...");
			
			haveMedia = false;
			
			do {
				Core.ThreadEnter();
			
				HigMessageDialog dialog = new HigMessageDialog(null,
					DialogFlags.Modal, 
					MessageType.Info,
					ButtonsType.OkCancel,
					Catalog.GetString("Insert Blank CD"),
					Catalog.GetString("Please insert a blank CD disk for the burn process."));
				
				dialog.Title = Catalog.GetString("Insert Blank CD");
				dialog.Icon =  Gdk.Pixbuf.LoadFromResource("banshee-icon.png");
				dialog.DefaultResponse = ResponseType.Ok;
				response = (ResponseType)dialog.Run();
				dialog.Destroy();

				while(Application.EventsPending())
					Application.RunIteration();
				
				Core.ThreadLeave();
			} while((drive.MediaSize <= 0 && response == ResponseType.Ok));

			if(response != ResponseType.Ok)
				Cancel();
			else 
				haveMedia = true;
		}
		
		private void ShowError(string header, string message)
		{
			if(cancelRequested)
				return;
				
			statusMessage = Catalog.GetString("Finished Burning: Error");
			totalCount = 1;
			currentCount = 1;
		
			Core.ThreadEnter();
			HigMessageDialog dialog = new HigMessageDialog(null,
				DialogFlags.Modal, 
				MessageType.Error,
				ButtonsType.Ok,
				header,
				message);

			dialog.Title = Catalog.GetString("Error Burning Disk");
			dialog.Icon =  Gdk.Pixbuf.LoadFromResource("banshee-icon.png");
			dialog.DefaultResponse = ResponseType.Ok;
			dialog.Run();
			dialog.Destroy();
			
			Core.ThreadLeave();
		}
		
		private void ShowSuccess()
		{
			if(cancelRequested)
				return;
			
			statusMessage = Catalog.GetString("Finished Burning: CD Written Successfully!");
			totalCount = 1;
			currentCount = 1;
		
			Core.ThreadEnter();
			HigMessageDialog dialog = new HigMessageDialog(null,
				DialogFlags.Modal, 
				MessageType.Info,
				ButtonsType.Ok,
				Catalog.GetString("CD Burning Complete"),
				Catalog.GetString("The selected audio was successfully written to the CD."));

			dialog.Title = Catalog.GetString("CD Burning Complete");
			dialog.Icon =  Gdk.Pixbuf.LoadFromResource("banshee-icon.png");
			dialog.DefaultResponse = ResponseType.Ok;
			dialog.Run();
			dialog.Destroy();
			
			Core.ThreadLeave();
		}
	}
}
*/
