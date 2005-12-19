/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
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

using Banshee.Base;
using Banshee.Dap;

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
            keyLabel.Markup = "<b>" + GLib.Markup.EscapeText(key) + "</b>:";
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
            Entry valEntry = new Entry();
            valEntry.Text = value == null ? String.Empty : value.ToString();
            
            AddWidget(key, valEntry);
            
            return valEntry;
        }
    }

    public class DapPropertiesDialog : Dialog
    {
        private DapSource source;
        private DapDevice device;
        private Entry nameEntry;
        private Entry ownerEntry;
        
        private string name_update;
        private string owner_update;
        
        private bool edited;
        
        public DapPropertiesDialog(DapSource source) : base(
            // Translators: {0} is the name assigned to a Digital Audio Player by its owner
            String.Format(Catalog.GetString("{0} Properties"), source.Device.Name),
            null,
            DialogFlags.Modal | DialogFlags.NoSeparator,
            Stock.Close,
            ResponseType.Close)
        {
            this.source = source;
            this.device = source.Device;
            
            HBox iconbox = new HBox();
            iconbox.Spacing = 10;
            Image icon = new Image();
            icon.Yalign = 0.0f;
            icon.Pixbuf = device.GetIcon(48);
            iconbox.PackStart(icon, false, false, 0);
            
            VBox box = new VBox();
            box.Spacing = 10;
            
            PropertyTable table = new PropertyTable();
            table.ColumnSpacing = 10;
            table.RowSpacing = 5;
            
            if(!device.IsReadOnly && device.CanSetName) {
                nameEntry = table.AddEntry(Catalog.GetString("Device Name"), device.Name);
                nameEntry.Changed += OnEntryChanged;
            } else {
                table.AddLabel(Catalog.GetString("Device Name"), device.Name);
            }
                        
            if(!device.IsReadOnly && device.CanSetOwner) {
                ownerEntry = table.AddEntry(Catalog.GetString("Owner Name"), device.Owner);
                ownerEntry.Changed += OnEntryChanged;
            } else {
                table.AddLabel(Catalog.GetString("Owner Name"), device.Owner);
            }
            
            table.AddSeparator();
    
            table.AddWidget(Catalog.GetString("Volume Usage"), UsedProgressBar);
    
            box.PackStart(table, true, true, 0);
            
            PropertyTable extTable = new PropertyTable();
            extTable.ColumnSpacing = 10;
            extTable.RowSpacing = 5;
            
            foreach(DapDevice.Property property in device.Properties) {
                extTable.AddLabel(property.Name, property.Value);
            }
            
            Expander expander = new Expander(Catalog.GetString("Advanced Details"));
            expander.Add(extTable);
            box.PackStart(expander, false, false, 0);
            
            BorderWidth = 10;
            Resizable = false;
            iconbox.PackStart(box, true, true, 0);
            iconbox.ShowAll();
            VBox.Add(iconbox);
        }
        
        private ProgressBar UsedProgressBar {
            get {
                ProgressBar usedBar = new ProgressBar();
                usedBar.Fraction = source.DiskUsageFraction;
                usedBar.Text = source.DiskUsageString + " " + source.DiskAvailableString;
                return usedBar;
            }
        }
        
        private void OnEntryChanged(object o, EventArgs args)
        {
            if(ownerEntry != null) {
                owner_update = ownerEntry.Text;
            }
            
            if(nameEntry != null) {
                name_update = nameEntry.Text;
            }
            
            edited = true;
        }
        
        public bool Edited {
            get {
                return edited;
            }
        }
        
        public string UpdatedName {
            get {
                return name_update;
            }
        }
        
        public string UpdatedOwner {
            get {
                return owner_update;
            }
        }
    }
}
