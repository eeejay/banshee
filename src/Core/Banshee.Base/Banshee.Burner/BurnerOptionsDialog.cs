/***************************************************************************
 *  BurnerOptionsDialog.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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

using Mono.Unix;
using Gtk;
using Glade;

using Banshee.Gui;
using Banshee.Cdrom;
using Banshee.Cdrom.Gui;

namespace Banshee.Burner
{
    public class BurnerOptionsDialog : GladeDialog
    {
        private BurnerSource source;
        private DriveComboBox drive_combo;
        private RecorderSpeedComboBox recorder_speed_combo;
        private BurnerFormatList format_list;
        
        [Widget] private Table session_table;
        [Widget] private Table write_options_table;
        [Widget] private Entry disc_name_entry;
        [Widget] private Label disc_name_label;
        [Widget] private CheckButton eject_checkbutton;
        [Widget] private Label disc_drive_label;
        
        public BurnerOptionsDialog(BurnerSource burnerSource) : base("BurnerOptionsDialog")
        {
            source = burnerSource;
            
            BurnerCore.DriveFactory.DriveAdded += OnDriveEvent;
            BurnerCore.DriveFactory.DriveRemoved += OnDriveEvent;
            
            drive_combo = new DriveComboBox(BurnerCore.DriveFactory);
            
            if(source.Session.Recorder == null) {
                foreach(IDrive drive in BurnerCore.DriveFactory) {
                    if(drive is IRecorder) {
                        source.Session.Recorder = drive as IRecorder;
                        break;
                    }
                }
            }
            
            source.Session.RecorderMediaChanged += OnRecorderMediaChanged;
            
            drive_combo.SelectedDrive = source.Session.Recorder;
            
            drive_combo.Changed += delegate {
                source.Session.RecorderMediaChanged -= OnRecorderMediaChanged;
                source.Session.Recorder = drive_combo.SelectedDrive as IRecorder;
                recorder_speed_combo.Recorder = source.Session.Recorder;
                source.Session.RecorderMediaChanged += OnRecorderMediaChanged;
            };
            
            HBox drive_box = new HBox();
            drive_box.PackStart(drive_combo, false, false, 0);
            session_table.Attach(drive_box, 1, 2, 0, 1,
                AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
            drive_box.ShowAll();
            
            format_list = new BurnerFormatList();
            format_list.SelectedFormat = source.Session.DiscFormat;
            disc_name_entry.Sensitive = format_list.SelectedFormat != "audio";
            disc_name_label.Sensitive = disc_name_entry.Sensitive;
            
            format_list.FormatChanged += delegate {
                disc_name_entry.Sensitive = format_list.SelectedFormat != "audio";
                disc_name_label.Sensitive = disc_name_entry.Sensitive;
                source.Session.DiscFormat = format_list.SelectedFormat;
            };
            
            source.Session.DiscFormat = format_list.SelectedFormat;
            
            session_table.Attach(format_list, 1, 2, 1, 2,
                AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
            format_list.Show();
            
            recorder_speed_combo = new RecorderSpeedComboBox(source.Session.Recorder);
            recorder_speed_combo.Speed = source.Session.WriteSpeed;
            recorder_speed_combo.Changed += delegate {
                source.Session.WriteSpeed = recorder_speed_combo.Speed;
            };
            
            HBox recorder_speed_box = new HBox();
            recorder_speed_box.PackStart(recorder_speed_combo, false, false, 0);
            write_options_table.Attach(recorder_speed_box, 1, 2, 0, 1, 
                AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
            recorder_speed_box.ShowAll();
            
            if(source.Session.DiscName != null) {
                disc_name_entry.Text = source.Session.DiscName;
            }
            
            disc_name_entry.Changed += delegate { 
                source.Rename(disc_name_entry.Text);
            };
            
            eject_checkbutton.Active = source.Session.EjectWhenFinished;
            eject_checkbutton.Toggled += delegate {
                source.Session.EjectWhenFinished = eject_checkbutton.Active;
            };
            
            OnDriveEvent(null, null);
        }
        
        private void OnDriveEvent(object o, EventArgs args)
        {
            bool show_selector = BurnerCore.DriveFactory.RecorderCount > 1;
            disc_drive_label.Visible = show_selector;
            drive_combo.Visible = show_selector;
        }
        
        private void OnRecorderMediaChanged(object o, EventArgs args)
        {
            drive_combo.SelectedDrive = source.Session.Recorder;
        }
    }
}
