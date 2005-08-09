/***************************************************************************
 *  LibraryTransactionStatus.cs
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
using Glade;

namespace Banshee
{
	public class LibraryTransactionStatus : Table
	{
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(LibraryTransactionStatus));
				return gtype;
			}
		}
		
		private ImageAnimation spinner;
		private Label LabelStatus;
		private Label LabelProgress;
		private ProgressBar Progress;
		private Button CancelButton;
		private bool active;
		
		public event EventHandler Stopped;
		
		public LibraryTransactionStatus() : base(2, 3, false)
		{
			Create();
			Clear();
			Stop();
		}
		
		private void Create()
		{
			Image img = new Image("gtk-close", IconSize.Menu);
			img.Show();
			
			CancelButton = new Button();
			CancelButton.Relief = ReliefStyle.None;
			CancelButton.Clicked += OnButtonCancelClicked;
			CancelButton.Add(img);
			CancelButton.Show();
			Attach(CancelButton, 1, 2, 1, 2, 
				AttachOptions.Fill, 
				(AttachOptions)0, 0, 0);
				
			LabelStatus = new Label();
			LabelStatus.Xalign = 0.0f;
			LabelStatus.Show();
			Attach(LabelStatus, 0, 1, 0, 1, 
				AttachOptions.Fill | AttachOptions.Expand, 
				(AttachOptions)0, 0, 0);
				
			HBox box = new HBox();
			box.Spacing = 5;
			box.Show();
				
			LabelProgress = new Label();
			LabelProgress.Show();
			box.PackStart(LabelProgress, false, false, 0);
				
			Progress = new ProgressBar();
			Progress.Show();
			box.PackStart(Progress, true, true, 0);
			
			Attach(box, 0, 1, 1, 2, 
				AttachOptions.Fill | AttachOptions.Expand, 
				(AttachOptions)0, 0, 0);
				
			spinner = new ImageAnimation(
				Gdk.Pixbuf.LoadFromResource("busy-spinner-36.png"), 
				75, 36, 36, 35);
			spinner.Show();
			Attach(spinner, 2, 3, 0, 2,
				AttachOptions.Shrink, 
				AttachOptions.Fill | AttachOptions.Expand, 0, 0);
				
			ColumnSpacing = 5;
		}
		
		public void Clear()
		{
			LabelStatus.Markup = "<small> </small>";
			LabelProgress.Markup = "<small>0 / 0</small>";
			Progress.Fraction = 0.0;
		}
		
		public void Start()
		{
			if(active || Core.Library.TransactionManager.TopExecution == null)
				return;

			Clear();
			
			active = true;
			GLib.Timeout.Add(100, new GLib.TimeoutHandler(OnTimeout));
			
			spinner.SetActive();
		}
		
		public void Stop()
		{
			active = false;
			EventHandler handler = Stopped;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		private bool OnTimeout()
		{
			if(!active) {
				spinner.SetInactive();
				return false;
			}
				
			return UpdateStatus();
		}
		
		private bool UpdateStatus()
		{
			LibraryTransaction transaction = 
				Core.Library.TransactionManager.TopExecution;
				
			if(transaction == null) {
				Stop();
				return false;
			}

			if(!Visible) 
				return true;

			//int tableCount = Core.Library.TransactionManager.TableCount;
			
			//LabelTitle.Markup = "<b>" + transaction.Name + 
			//	(tableCount > 1 ? " (" + tableCount + ")" : "") + "</b>";
			
			LabelStatus.Markup = "<small><b>" 
				+ StringUtil.EntityEscape(transaction.StatusMessage) 
				+ "</b></small>";
			
			if(transaction.TotalCount == 0) {
				LabelProgress.Markup = "<small>Working</small>";
				Progress.Pulse();
			} else {
				/*int etaSeconds = (int)((double)(transaction.TotalCount - 
					transaction.CurrentCount) * 
					((double)(transaction.AverageDuration) / 10000000.0));*/
				
				if(transaction.ShowCount) {
					LabelProgress.Markup = "<small>" + 
						transaction.CurrentCount + " / " +
						transaction.TotalCount + "</small>";
					if(!LabelProgress.Visible)
						LabelProgress.Visible = true;
				} else {
					LabelProgress.Visible = false;
				}
				
				double fraction = (double)transaction.CurrentCount /
					(double)transaction.TotalCount;
					
				if(fraction > 1.0)
					fraction = 1.0;
				else if(fraction < 0.0)
					fraction = 0.0;
					
				Progress.Fraction = fraction;
				//ProgressBar.Text = String.Format("Time Remaining: {0}:{1}",
				//	etaSeconds / 60, (etaSeconds % 60).ToString("00"));
			}
			
			return true;
		}
		
		private void OnButtonCancelClicked(object o, EventArgs args)
		{
			int tableCount = Core.Library.TransactionManager.TableCount;
				
			MessageDialog md = new MessageDialog(null, 
				DialogFlags.DestroyWithParent, MessageType.Question, 
                ButtonsType.None, 
				tableCount > 1 ? 
					"There are multiple operations executing. " +
					"You may either cancel the current operation or all " + 
					"operations.\n\n" +
					"Are you sure you want to cancel these operation(s)?" :
					
					"Are you sure you want to cancel this operation?"
				);
				
				
			if(tableCount > 1) {
				md.AddButton("Yes, Cancel Current", 1);
				md.AddButton("Yes, Cancel All", 2);
			} else {
				md.AddButton("Yes", 1);
			}
			
			md.AddButton("No", 3); 
     
			int result = md.Run();
			md.Destroy();
			
			switch(result) {
				case 1: 
					LibraryTransaction transaction = 
						Core.Library.TransactionManager.TopExecution;
				
					if(transaction != null)
						transaction.Cancel();
						
					return;
				case 2:
					Core.Library.TransactionManager.CancelAll();
					return;	
			}
		}
		
		public bool AllowShow
		{
			get {
				return Core.Library.TransactionManager.TopExecution != null;
			}
		}
	}
}
