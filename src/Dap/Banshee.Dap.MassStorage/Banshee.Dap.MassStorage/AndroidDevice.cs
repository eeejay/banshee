//
// AndroidDevice.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Banshee.Hardware;

namespace Banshee.Dap.MassStorage
{
    public class AndroidDevice : CustomMassStorageDevice
    {
        private static string [] playback_mime_types = new string [] {
            "audio/mpeg",
            "audio/mp4",
            "audio/aac",
            "audio/x-ms-wma",
            "application/ogg",
            "audio/x-wav",
            "audio/vnd.rn-realaudio",
            "audio/3gpp",
            "audio/x-midi"
        };
        
        private static string [] audio_folders = new string [] {
            "Music/",
            "amazonmp3/"
        };
        
        public AndroidDevice (VendorProductInfo productInfo, MassStorageSource source) 
            : base (productInfo, source)
        {
        }
        
        public override bool LoadDeviceConfiguration ()
        {
            return true;
        }
        
        private string name;
        public override string Name {
            get { 
                if (name == null) {
                    name = Source.UsbDevice.Name;
                    if (String.IsNullOrEmpty (Name)) {
                        name = base.Name ?? "Android Phone";
                    }
                }
                
                return name;
            }
        }
        
        public override string[] AudioFolders {
            get { return audio_folders; }
        }
        
        public override string [] PlaybackMimeTypes {
            get { return playback_mime_types; }
        }
        
        // Cover art information gleaned from Android's Music application
        // packages/apps/Music/src/com/android/music/MusicUtils.java
        // <3 open source
        
        public override string CoverArtFileName {
            get { return "AlbumArt.jpg"; }
        }
        
        public override string CoverArtFileType {
            get { return "jpeg"; }
        }
        
        public override int CoverArtSize {
            get { return 320; }
        }
    }
}
