/***************************************************************************
 *  StationGroupEditor.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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

namespace Banshee.Plugins.Radio
{
    public class StationGroupEditor : Gtk.Dialog
    {
        private StationGroup station_group;
        private Entry entry;
        private HBox exists_message;
        
        public StationGroupEditor(StationGroup group) : base()
        {
            station_group = group;
            
            AccelGroup accel_group = new AccelGroup();
            AddAccelGroup(accel_group);       
            
            Title = group == null
                ? Catalog.GetString("Add new station group")
                : Catalog.GetString("Edit station group");
            
            BorderWidth = 5;
            HasSeparator = false;
            DefaultResponse = ResponseType.Ok;
            Modal = true;
            
            VBox.Spacing = 10;
            
            VBox main_box = new VBox();
            main_box.BorderWidth = 5;
            main_box.Spacing = 10;
            
            Label header = new Label();
            header.Markup = String.Format("<big><b>{0}</b></big>", GLib.Markup.EscapeText(Title));
            header.Xalign = 0.0f;
            header.Show();
            
            HBox entry_box = new HBox();
            entry_box.Spacing = 6;
            
            Label label = new Label(Catalog.GetString("Title:"));
            label.Show();
            
            entry = new Entry();
            entry.Show();
            
            entry_box.PackStart(label, false, false, 0);
            entry_box.PackStart(entry, true, true, 0);
            entry_box.Show();
            
            main_box.PackStart(header, false, false, 0);
            main_box.PackStart(entry_box, false, false, 0);
            main_box.Show();
            
            VBox.PackStart(main_box, true, true, 0);
            
            Button cancel_button = new Button(Stock.Cancel);
            cancel_button.CanDefault = false;
            cancel_button.UseStock = true;
            cancel_button.Show();
            AddActionWidget(cancel_button, ResponseType.Close);
            
            cancel_button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Escape, 
                0, Gtk.AccelFlags.Visible);
            
            Button save_button = new Button(Stock.Save);
            save_button.CanDefault = true;
            save_button.UseStock = true;
            save_button.Sensitive = false;
            save_button.Show();
            AddActionWidget(save_button, ResponseType.Ok);
            
            save_button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Return, 
                0, Gtk.AccelFlags.Visible);
                
            entry.HasFocus = true;
            entry.Changed += delegate { save_button.Sensitive = entry.Text.Trim().Length > 0; };
            
            if(group != null) {
                entry.Text = group.Title;
                entry.SelectRegion(0, entry.Text.Length);
            }
            
            exists_message = new HBox();
            exists_message.Spacing = 4;
            
            Image image = new Image();
            image.Stock = Stock.DialogError;
            image.IconSize = (int)IconSize.Menu;
            image.Show();
            
            Label error = new Label(Catalog.GetString("The station group already exists"));
            error.Xalign = 0.0f;
            error.Show();
            
            exists_message.PackStart(image, false, false, 0);
            exists_message.PackStart(error, true, true, 0);
            
            VBox.PackStart(exists_message, false, false, 0);
        }

        public void FocusEntry()
        {
            entry.HasFocus = true;
        }
        
        public void ShowExistsMessage()
        {
            exists_message.Show();
        }

        public string Value { 
            get { return entry.Text; }
        }
    }
}
