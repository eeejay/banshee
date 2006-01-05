/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DapSource.cs
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
using Mono.Unix;

using Banshee.Base;
using Banshee.Dap;

namespace Banshee.Sources
{
    public class DapSource : Source
    {
        private Banshee.Dap.DapDevice device;
        
        public bool NeedSync;
        
        public DapSource(Banshee.Dap.DapDevice device) : base(device.Name, 400)
        {
            this.device = device;
            CanRename = device.CanSetName;
        }
        
        protected override bool UpdateName(string oldName, string newName)
        {
            if(!device.CanSetName) {
                return true;
            }
        
            if(oldName == null || !oldName.Equals(newName)) {
                device.SetName(newName);
                Name = newName;
            }
            
            return true;
        }
        
        public override void AddTrack(TrackInfo track)
        {
            if(track is LibraryTrackInfo) {
                device.AddTrack(track);
            }
        }
        
        public override void RemoveTrack(TrackInfo track)
        {
            device.RemoveTrack(track);
        }
        
        public void SetSourceName(string name)
        {
            Name = name;
        }
        
        public override int Count {
            get {
                return device.TrackCount;
            }
        }        
        
        public Banshee.Dap.DapDevice Device {
            get {
                return device;
            }
        }
        
        public override ICollection Tracks {
            get {
                return device.Tracks;
            }
        }
        
        public override bool Eject()
        {
            device.Eject();
            return true;
        }

        public double DiskUsageFraction {
            get {
                return (double)device.StorageUsed / (double)device.StorageCapacity;
            }
        }

        public string DiskUsageString {
            get {
                // Translators: iPod disk usage. Each {N} is something like "100 MB"
                return String.Format(
                    Catalog.GetString("{0} of {1}"),
                    Utilities.BytesToString(device.StorageUsed),
                    Utilities.BytesToString(device.StorageCapacity));
            }
        }

        public string DiskAvailableString {
            get {
                // Translators: iPod disk usage. {0} is something like "100 MB"
                return String.Format(
                    Catalog.GetString("({0} Remaining)"),
                    Utilities.BytesToString(device.StorageCapacity - device.StorageUsed));
            }
        }
        
        public bool IsSyncing {
            get {
                return device.IsSyncing;
            }
        }

        public override void ShowPropertiesDialog()
        {
            DapPropertiesDialog properties_dialog = new DapPropertiesDialog(this);
            IconThemeUtils.SetWindowIcon(properties_dialog);
            properties_dialog.Run();
            properties_dialog.Destroy();
              
            if(properties_dialog.Edited && !Device.IsReadOnly) {
                if(properties_dialog.UpdatedName != null) {
                    Device.SetName(properties_dialog.UpdatedName);
                    SetSourceName(Device.Name);
                }
                  
                if(properties_dialog.UpdatedOwner != null) {
                    Device.SetOwner(properties_dialog.UpdatedOwner);
                }
            }
        }
        
        public override Gdk.Pixbuf Icon {
            get {
                return device.GetIcon(22);
            }
        }
    }
}
