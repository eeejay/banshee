/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PlaylistSource.cs
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
using System.Data;
using System.Collections;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.Sources
{
    public class AudioCdSource : Source
    {
        private AudioCdDisk disk;
        
        public AudioCdSource(AudioCdDisk disk) : base(disk.Title, 200)
        {
            this.disk = disk;
            disk.Updated += OnDiskUpdated;
        }
        
        public override int Count {
            get {
                return disk.TrackCount;
            }
        }
        
        public AudioCdDisk Disk {
            get {
                return disk;
            }
        }
        
        public override bool Eject()
        {
            disk.Eject();
            SourceManager.RemoveSource(this);
            return true;
        }
        
        private void OnDiskUpdated(object o, EventArgs args)
        {
            ThreadAssist.ProxyToMain(delegate {
                Name = disk.Title;
            });
        }
                
        public override Gdk.Pixbuf Icon {
            get {
                return IconThemeUtils.LoadIcon(22, "media-cdrom", "gnome-dev-cdrom-audio", "source-cd-audio");
            }
        }
        
        public override ICollection Tracks {
            get {
                return disk.Tracks;
            }
        }
    }
}
