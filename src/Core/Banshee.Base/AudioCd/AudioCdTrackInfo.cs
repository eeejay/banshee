/***************************************************************************
 *  AudioCdTrackInfo.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
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
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using MusicBrainz;

namespace Banshee.Base
{   
    public class AudioCdTrackInfo : TrackInfo
    {
        private int track_index;
        private bool do_rip;
        private bool is_ripped;
        private AudioCdDisk disk;
        
        public AudioCdTrackInfo(AudioCdDisk disk)
        {
            PreviousTrack = Gtk.TreeIter.Zero;
            CanSaveToDatabase = false;
            this.disk = disk;
            do_rip = true;
        }
        
        public override void Save()
        {       
        }
        
        public override void IncrementPlayCount()
        {
        }
        
        protected override void SaveRating()
        {
        }
        
        public int TrackIndex { 
            get { return track_index; } 
            
            set { 
                track_index = value;
                TrackNumber = (uint)value;
                uri = new SafeUri("cdda://" + track_index + "#" + disk.DeviceNode); 
            } 
        }
        
        public string Device { 
            get { return disk.DeviceNode; } 
        }
        
        public AudioCdDisk Disk {
            get { return disk; }
        }
        
        public bool CanRip {
            get { return do_rip; }
            set { do_rip = value; }
        }
        
        public bool IsRipped {
            get { return is_ripped; }
            set { is_ripped = value; }
        }
        
        public override bool CanPlay {
            get { return !disk.IsRipping || (disk.IsRipping && IsRipped); }
        }
    }
}
    