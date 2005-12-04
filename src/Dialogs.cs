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
using Mono.Unix;

using Banshee.Base;
using Banshee.Widgets;

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
    
    public class MessageDialogs
    {
        public static void CannotRenamePlaylist()
        {
            HigMessageDialog.RunHigMessageDialog(null,
            DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
            Catalog.GetString("Cannot Rename Playlist"),
            Catalog.GetString("A playlist with this name already exists. " + 
            "Please choose another name."));
        }
    }
    
    public class ErrorDialog
    {
        public static void Run(string message)
        {
            Run(Catalog.GetString("An Error Occurred"), message);
        }
    
        public static void Run(string header, string message)
        {            
            HigMessageDialog dialog = new HigMessageDialog(null, 
                DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, header, message);
            dialog.Title = Catalog.GetString("Error");
            IconThemeUtils.SetWindowIcon(dialog);
            
            dialog.Response += OnResponse;
            dialog.ShowAll();
        }
        
        private static void OnResponse(object o, ResponseArgs args)
        {
            (o as Dialog).Response -= OnResponse;
            (o as Dialog).Destroy();
        }
    }
    
    public class InputDialog
    {
        private Dialog dialog;
        private string title, message, text;
        private Gdk.Pixbuf icon;
        
        public static string Run(string title, string message, 
            Gdk.Pixbuf icon, string text)
        {
            InputDialog d = new InputDialog(title, message, icon, text);
            return d.Execute();
        }
        
        public InputDialog(string title, string message, 
            Gdk.Pixbuf icon, string text)
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
            image.Pixbuf = icon;
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
}
