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
using Mono.Unix;

using Banshee.Base;
using Banshee.Hardware;
using Banshee.Library;
using Banshee.Collection;
using Banshee.Collection.Database;

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

        private static string [] playlist_formats = new string [] {
            "audio/x-mpegurl"
        };

        private static string playlists_path = "Playlists/";
        
        private static string [] icon_names = new string [] {
            "phone-htc-g1-white", DapSource.FallbackIcon
        };
        
        private AmazonMp3GroupSource amazon_source;
        private string amazon_base_dir;
        
        public override void SourceInitialize ()
        {
            amazon_base_dir = System.IO.Path.Combine (Source.Volume.MountPoint, audio_folders[1]);
            
            amazon_source = new AmazonMp3GroupSource (this, Source);
            amazon_source.AutoHide = true;
        }
        
        public override bool LoadDeviceConfiguration ()
        {
            return true;
        }
        
        public override string Name {
            get { return VendorProductInfo.ProductName; }
        }
        
        public override string [] AudioFolders {
            get { return audio_folders; }
        }

        public override string [] PlaylistFormats {
            get { return playlist_formats; }
        }
        
        public override string PlaylistPath {
            get { return playlists_path; }
        }

        public override string [] PlaybackMimeTypes {
            get { return playback_mime_types; }
        }
        
        // Cover art information gleaned from Android's Music application
        // packages/apps/Music/src/com/android/music/MusicUtils.java
        // <3 open source
        
        public override int FolderDepth {
            get { return 2; }
        }
        
        public override string CoverArtFileName {
            get { return "AlbumArt.jpg"; }
        }
        
        public override string CoverArtFileType {
            get { return "jpeg"; }
        }
        
        public override int CoverArtSize {
            get { return 320; }
        }
        
        public override string [] GetIconNames () 
        {
            return icon_names;
        }
        
        public override bool GetTrackPath (TrackInfo track, out string path)
        {
            path = FileNamePattern.CreateFromTrackInfo (
                "%artist%%path_sep%%album%%path_sep%%track_number%. %title%",
                track);
            return true;
        }
        
#region Amazon MP3 Store Purchased Tracks Management

        public override bool DeleteTrackHook (DatabaseTrackInfo track)
        {
            // Do not allow removing purchased tracks if not in the
            // Amazon Purchased Music source; this should prevent
            // accidental deletion of purchased music that may not 
            // have been copied from the device yet.
            //
            // TODO: Provide some feedback when a purchased track is
            // skipped from deletion
            //
            // FIXME: unfortunately this does not work due to 
            // the cache models being potentially different
            // even though they will always reference the same tracks
            // amazon_source.TrackModel.IndexOf (track) >= 0
            
            if (!amazon_source.Active && amazon_source.Count > 0 && track.Uri.LocalPath.StartsWith (amazon_base_dir)) {
                return false;
            }
            
            return true;
        }
        
        private class AmazonMp3GroupSource : MediaGroupSource, IPurchasedMusicSource
        {
            private AndroidDevice android;
            
            public AmazonMp3GroupSource (AndroidDevice android, DapSource parent) 
                : base (parent, Catalog.GetString ("Purchased Music"))
            {
                this.android = android;
                
                Properties.SetString ("Icon.Name", "amazon-mp3-source");
                ConditionSql = "(CoreTracks.Uri LIKE \"amazonmp3/%\")";
            }
            
            public override void Activate ()
            {
                base.Activate ();
                active = true;
            }

            public override void Deactivate ()
            {
                base.Deactivate ();
                active = false;
            }
            
            public void Import ()
            {
                LibraryImportManager importer = new LibraryImportManager (true);
                importer.Enqueue (android.amazon_base_dir);
            }
            
            private bool active;
            public bool Active {
                get { return active; }
            }
        }

#endregion

    }
}
