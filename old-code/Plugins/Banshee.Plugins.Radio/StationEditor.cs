/***************************************************************************
 *  StationEditor.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
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

using Banshee.Playlists.Formats.Xspf;

namespace Banshee.Plugins.Radio
{
    public class StationEditor : Gtk.Dialog
    {
        private Button save_button;
        private Entry name_entry;
        private Entry description_entry;
        private Entry stream_entry;
        private ComboBoxEntry group_entry;
        private Alignment error_container;
        private Label error;
        
        public StationEditor(StationModel station_model, string group, Track station) : base()
        {
            AccelGroup accel_group = new AccelGroup();
            AddAccelGroup(accel_group);       
            
            Title = String.Empty;
            SkipTaskbarHint = true;
            Modal = true;
            
            string title = station == null
                ? Catalog.GetString("Add new radio station")
                : Catalog.GetString("Edit radio station");
            
            BorderWidth = 6;
            HasSeparator = false;
            DefaultResponse = ResponseType.Ok;
            Modal = true;
            
            VBox.Spacing = 6;
            
            HBox split_box = new HBox();
            split_box.Spacing = 12;
            split_box.BorderWidth = 6;
            
            Image image = new Image();
            image.Pixbuf = Gdk.Pixbuf.LoadFromResource("dialog-radio.png");
            image.Yalign = 0.0f;
            image.Show();
            
            VBox main_box = new VBox();
            main_box.BorderWidth = 5;
            main_box.Spacing = 10;
            
            Label header = new Label();
            header.Markup = String.Format("<big><b>{0}</b></big>", GLib.Markup.EscapeText(title));
            header.Xalign = 0.0f;
            header.Show();

            Label message = new Label();
            message.Text = Catalog.GetString("Enter the Group, Title and URL of the radio station you wish to add. A description is optional.");
            message.Xalign = 0.0f;
            message.Wrap = true;
            message.Show();
            
            Table table = new Table(5, 2, false);
            table.RowSpacing = 6;
            table.ColumnSpacing = 6;
            
            Label label = new Label(Catalog.GetString("Station Group:"));
            label.Xalign = 0.0f;
            
            group_entry = ComboBoxEntry.NewText();
            
            foreach(string group_name in station_model.StationGroupNames) {
                group_entry.AppendText(group_name);
            }
            
            if(group != null) {
                group_entry.Entry.Text = group;
            }
            
            table.Attach(label, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            table.Attach(group_entry, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
            
            label = new Label(Catalog.GetString("Station Title:"));
            label.Xalign = 0.0f;
            
            name_entry = new Entry();
            
            table.Attach(label, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            table.Attach(name_entry, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);

            label = new Label(Catalog.GetString("Stream URL:"));
            label.Xalign = 0.0f;
            
            stream_entry = new Entry();
            
            table.Attach(label, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            table.Attach(stream_entry, 1, 2, 2, 3, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
            
            label = new Label(Catalog.GetString("Description:"));
            label.Xalign = 0.0f;
            
            description_entry = new Entry();
            
            table.Attach(label, 0, 1, 3, 4, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            table.Attach(description_entry, 1, 2, 3, 4, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            
            table.ShowAll();
            
            main_box.PackStart(header, false, false, 0);
            main_box.PackStart(message, false, false, 0);
            main_box.PackStart(table, false, false, 0);
            main_box.Show();
            
            split_box.PackStart(image, false, false, 0);
            split_box.PackStart(main_box, true, true, 0);
            split_box.Show();
            
            VBox.PackStart(split_box, true, true, 0);
            
            Button cancel_button = new Button(Stock.Cancel);
            cancel_button.CanDefault = false;
            cancel_button.UseStock = true;
            cancel_button.Show();
            AddActionWidget(cancel_button, ResponseType.Close);
            
            cancel_button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Escape, 
                0, Gtk.AccelFlags.Visible);
            
            save_button = new Button(Stock.Save);
            save_button.CanDefault = true;
            save_button.UseStock = true;
            save_button.Sensitive = false;
            save_button.Show();
            AddActionWidget(save_button, ResponseType.Ok);
            
            save_button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Return, 
                0, Gtk.AccelFlags.Visible);
                
            name_entry.HasFocus = true;
            
            if(station != null) {
                if(station.Title != null) {
                    name_entry.Text = station.Title;
                }
                
                if(station.LocationCount > 0) {
                    stream_entry.Text = station.GetLocationAt(0).AbsoluteUri;
                }
                
                if(station.Annotation != null) {
                    description_entry.Text = station.Annotation;
                }
            }
            
            error_container = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            error_container.TopPadding = 6;
            HBox error_box = new HBox();
            error_box.Spacing = 4;
            
            Image error_image = new Image();
            error_image.Stock = Stock.DialogError;
            error_image.IconSize = (int)IconSize.Menu;
            error_image.Show();
            
            error = new Label();
            error.Xalign = 0.0f;
            error.Show();
            
            error_box.PackStart(error_image, false, false, 0);
            error_box.PackStart(error, true, true, 0);
            error_box.Show();
            
            error_container.Add(error_box);
            
            table.Attach(error_container, 0, 2, 4, 5, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            
            group_entry.Entry.Changed += OnFieldsChanged;
            name_entry.Changed += OnFieldsChanged;
            stream_entry.Changed += OnFieldsChanged;
            
            OnFieldsChanged(this, EventArgs.Empty);
        }
        
        private void OnFieldsChanged(object o, EventArgs args)
        {
            save_button.Sensitive = group_entry.Entry.Text.Trim().Length > 0 && 
                name_entry.Text.Trim().Length > 0 && stream_entry.Text.Trim().Length > 0;
        }
        
        public void FocusUri()
        {
            stream_entry.HasFocus = true;
            stream_entry.SelectRegion(0, stream_entry.Text.Length);
        }
        
        public string Group {
            get { return group_entry.Entry.Text.Trim(); }
        }
        
        public string StationTitle {
            get { return name_entry.Text.Trim(); }
        }
        
        public string StreamUri {
            get { return stream_entry.Text.Trim(); }
        }
        
        public string Description {
            get { return description_entry.Text.Trim(); }
        }
        
        public string ErrorMessage {
            set { 
                if(value == null) {
                    error_container.Hide();
                } else {
                    error.Text = value; 
                    error_container.Show();
                }
            }
        }
    }
}
