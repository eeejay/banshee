/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Dialogs.cs
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

namespace Banshee
{
	public class SimpleMessageDialogs
	{
		static public ResponseType Error(string message)
		{
			return SimpleMessageDialogs.Message(message, MessageType.Error);
		}
		
		static public ResponseType Message(string message, MessageType type)
		{
			MessageDialog dialog = new MessageDialog(null, 
				DialogFlags.DestroyWithParent,
				type, ButtonsType.Close, message);
			ResponseType response = (ResponseType)dialog.Run();
			dialog.Destroy();
			return response;
		}
		
		static public ResponseType Message(string message)
		{
			return Message(message, MessageType.Info);
		}
		
		static public ResponseType YesNo(string message)
		{
			return SimpleMessageDialogs.YesNo(message, MessageType.Question);
		}
		
		static public ResponseType YesNo(string message, MessageType type)
		{
			MessageDialog dialog = new MessageDialog(null, 
				DialogFlags.DestroyWithParent,
				type, ButtonsType.YesNo, message);
			ResponseType response = (ResponseType)dialog.Run();
			dialog.Destroy();
			return response;
		}
	}
	
	public class ErrorDialog
	{
		static public void Run(string message)
		{
			MessageDialog dialog = new MessageDialog(null, 
				DialogFlags.DestroyWithParent,
				MessageType.Error, ButtonsType.Close, message);
			dialog.Run();
			dialog.Destroy();
		}
	}
	
	public class InputDialog
	{
		private Dialog dialog;
		private string title, message, icon, text;
		
		public static string Run(string title, string message, 
			string icon, string text)
		{
			InputDialog d = new InputDialog(title, message, icon, text);
			return d.Execute();
		}
		
		public InputDialog(string title, string message, 
			string icon, string text)
		{
			this.title = title;
			this.message = message;
			this.icon = icon;
			this.text = text;
		}
		
		public string Execute()
		{
  			Dialog dialog = new Dialog();
 			dialog.Title = title;
 			dialog.Resizable = false;

			VBox vbox = dialog.VBox;
			vbox.Show();
			
			Table table = new Table(2, 2, false);
			table.Show();
			
			vbox.PackStart(table, false, false, 0);		
			table.ColumnSpacing = 10;
			table.RowSpacing = 10;	
			table.BorderWidth = 10;
			
			Entry entry = new Entry();
			if(text != null)
				entry.Text = text;
			entry.Show();
			table.Attach(entry, 1, 2, 1, 2, 
				AttachOptions.Expand | AttachOptions.Fill,
				0, 0, 0);
				
			Image image = new Image();
			image.Pixbuf = Gdk.Pixbuf.LoadFromResource(icon);
			image.Show();
			table.Attach(image, 0, 1, 0, 2,
				AttachOptions.Expand | AttachOptions.Fill,
				0, 0, 0);
				
			Label label = new Label(message);
			label.Show();
			label.SetAlignment(0.0f, 0.0f);
			table.Attach(label, 1, 2, 0, 1,
				AttachOptions.Expand | AttachOptions.Fill,
					0, 0, 0);
	
			HButtonBox actionArea = dialog.ActionArea;
			actionArea.Show();
			actionArea.Layout = ButtonBoxStyle.End;	
			
			Button cancelButton = new Button("gtk-cancel");
			cancelButton.Show();
			
			Button saveButton = new Button("gtk-save");
			saveButton.Show();
			
			dialog.AddActionWidget(cancelButton, ResponseType.Cancel);
			dialog.AddActionWidget(saveButton, ResponseType.Ok);

			saveButton.CanDefault = true;
			saveButton.HasDefault = true;
			entry.ActivatesDefault = true;

			ResponseType result = (ResponseType)dialog.Run();
			string input = entry.Text;
			
			dialog.Destroy();
			
			if(result != ResponseType.Ok)
				return null;

			return input;
		}
	}
	
public class HigMessageDialog : Gtk.Dialog
{	
	Gtk.AccelGroup accel_group;

	public HigMessageDialog (Gtk.Window parent,
				 Gtk.DialogFlags flags,
				 Gtk.MessageType type,
				 Gtk.ButtonsType buttons,
				 string          header,
				 string          msg)
		: base ()
	{
		HasSeparator = false;
		BorderWidth = 5;
		Resizable = false;
		Title = "";

		VBox.Spacing = 12;
		ActionArea.Layout = Gtk.ButtonBoxStyle.End;

		accel_group = new Gtk.AccelGroup ();
		AddAccelGroup (accel_group);

		Gtk.HBox hbox = new Gtk.HBox (false, 12);
		hbox.BorderWidth = 5;
		hbox.Show ();
		VBox.PackStart (hbox, false, false, 0);

		Gtk.Image image = null;

		switch (type) {
		case Gtk.MessageType.Error:
			image = new Gtk.Image (Gtk.Stock.DialogError, Gtk.IconSize.Dialog);
			break;
		case Gtk.MessageType.Question:
			image = new Gtk.Image (Gtk.Stock.DialogQuestion, Gtk.IconSize.Dialog);
			break;
		case Gtk.MessageType.Info:
			image = new Gtk.Image (Gtk.Stock.DialogInfo, Gtk.IconSize.Dialog);
			break;
		case Gtk.MessageType.Warning:
			image = new Gtk.Image (Gtk.Stock.DialogWarning, Gtk.IconSize.Dialog);
			break;
		}

		image.Show ();
		hbox.PackStart (image, false, false, 0);
		
		Gtk.VBox label_vbox = new Gtk.VBox (false, 0);
		label_vbox.Show ();
		hbox.PackStart (label_vbox, true, true, 0);

		string title = String.Format ("<span weight='bold' size='larger'>{0}" +
					      "</span>\n",
					      header);

		Gtk.Label label;

		label = new Gtk.Label (title);
		label.UseMarkup = true;
		label.Justify = Gtk.Justification.Left;
		label.LineWrap = true;
		label.SetAlignment (0.0f, 0.5f);
		label.Show ();
		label_vbox.PackStart (label, false, false, 0);

		label = new Gtk.Label (msg);
		label.UseMarkup = true;
		label.Justify = Gtk.Justification.Left;
		label.LineWrap = true;
		label.SetAlignment (0.0f, 0.5f);
		label.Show ();
		label_vbox.PackStart (label, false, false, 0);
		
		switch (buttons) {
		case Gtk.ButtonsType.None:
			break;
		case Gtk.ButtonsType.Ok:
			AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok, true);
			break;
		case Gtk.ButtonsType.Close:
			AddButton (Gtk.Stock.Close, Gtk.ResponseType.Close, true);
			break;
		case Gtk.ButtonsType.Cancel:
			AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, true);
			break;
		case Gtk.ButtonsType.YesNo:
			AddButton (Gtk.Stock.No, Gtk.ResponseType.No, false);
			AddButton (Gtk.Stock.Yes, Gtk.ResponseType.Yes, true);
			break;
		case Gtk.ButtonsType.OkCancel:
			AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, false);
			AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok, true);
			break;
		}

		if (parent != null)
			TransientFor = parent;

		if ((int) (flags & Gtk.DialogFlags.Modal) != 0)
			Modal = true;

		if ((int) (flags & Gtk.DialogFlags.DestroyWithParent) != 0)
			DestroyWithParent = true;
	}

	// constructor for a HIG confirmation alert with two buttons
	public HigMessageDialog (Gtk.Window parent,
				 Gtk.DialogFlags flags,
				 Gtk.MessageType type,
				 string          header,
				 string          msg,
				 string          ok_caption)
		: this (parent, flags, type, Gtk.ButtonsType.Cancel, header, msg)
	{
		AddButton (ok_caption, Gtk.ResponseType.Ok, false);
	}
	
	public void AddButton (string stock_id, Gtk.ResponseType response, bool is_default)
	{
		Gtk.Button button = new Gtk.Button (stock_id);
		button.CanDefault = true;
		button.Show ();

		AddActionWidget (button, response);

		if (is_default) {
			DefaultResponse = response;
			button.AddAccelerator ("activate",
					       accel_group,
					       (uint) Gdk.Key.Escape, 
					       0,
					       Gtk.AccelFlags.Visible);
		}
	}

	//run and destroy a standard dialog
	public static Gtk.ResponseType RunHigMessageDialog(Gtk.Window parent,
				 Gtk.DialogFlags flags,
				 Gtk.MessageType type,
				 Gtk.ButtonsType buttons,
				 string          header,
				 string          msg)
	{
		HigMessageDialog hmd = new HigMessageDialog(parent, flags, type, buttons, header, msg);
 		try {
 			return (Gtk.ResponseType)hmd.Run();
 		} finally {
 			hmd.Destroy();
 		}	
	}

	//Run and destroy a standard confirmation dialog
	public static Gtk.ResponseType RunHigConfirmation(Gtk.Window parent,
				 Gtk.DialogFlags flags,
				 Gtk.MessageType type,
				 string          header,
				 string          msg,
				 string          ok_caption)
	{
		HigMessageDialog hmd = new HigMessageDialog(parent, flags, type, header, msg, ok_caption);
 		try {
 			return (Gtk.ResponseType)hmd.Run();
 		} finally {
 			hmd.Destroy();
 		}	
 	}
 }
 
 }
