//
// MassStorageSource.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections.Generic;
using Mono.Unix;

using Hyena;
using Hyena.Collections;

using Banshee.IO;
using Banshee.Dap;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Hardware;

namespace Banshee.Dap.MassStorage
{
    public class MassStorageSource : DapSource
    {
        protected IVolume volume;

        public override void DeviceInitialize (IDevice device)
        {
            base.DeviceInitialize (device);
            
            this.volume = device as IVolume;
            if (volume == null)
                throw new InvalidDeviceException ();

            // TODO set up a ui for selecting volumes we want mounted/shown within Banshee
            // (so people don't have to touch .is_audio_player, and so we can give them a harddrive icon
            // instead of pretending they are DAPs).
            if (!HasMediaCapabilities)
                throw new InvalidDeviceException ();

            Name = volume.Name;
            mount_point = volume.MountPoint;

            Initialize ();

            // TODO differentiate between Audio Players and normal Disks, and include the size, eg "2GB Audio Player"?
            //GenericName = Catalog.GetString ("Audio Player");
        }

        // WARNING: This will be called from a thread!
        protected override void LoadFromDevice ()
        {
            DatabaseImportManager importer = new DatabaseImportManager (this);
            importer.KeepUserJobHidden = true;
            importer.Threaded = false; // We're already threaded
            importer.QueueSource (BaseDirectory);
        }

        public override void Import ()
        {
            new LibraryImportManager (true).QueueSource (BaseDirectory);
        }

        public IVolume Volume {
            get { return volume; }
        }

        private string mount_point;
        public override string BaseDirectory {
            get { return mount_point; }
        }

        protected override bool HasMediaCapabilities {
            get { return base.HasMediaCapabilities || File.Exists (new SafeUri (IsAudioPlayerPath)); }
        }

        protected override IDeviceMediaCapabilities MediaCapabilities {
            get { return (volume.Parent == null) ? base.MediaCapabilities : volume.Parent.MediaCapabilities ?? base.MediaCapabilities; }
        }

        protected string IsAudioPlayerPath {
            get { return System.IO.Path.Combine (volume.MountPoint, ".is_audio_player"); }
        }
        
        public override long BytesUsed {
            get { return BytesCapacity - volume.Available; }
        }
        
        public override long BytesCapacity {
            get { return (long) volume.Capacity; }
        }

        private bool had_write_error = false;
        public override bool IsReadOnly {
            get { return volume.IsReadOnly || had_write_error; }
        }

        private string write_path = null;
        protected string WritePath {
            get {
                if (write_path == null) {
                    write_path = BaseDirectory;
                    // According to the HAL spec, the first folder listed in the audio_folders property
                    // is the folder to write files to.
                    if (MediaCapabilities != null && MediaCapabilities.AudioFolders.Length > 0) {
                        write_path = System.IO.Path.Combine (write_path, MediaCapabilities.AudioFolders[0]);
                    }
                }
                return write_path;
            }

            set { write_path = value; }
        }

        protected override void AddTrackToDevice (DatabaseTrackInfo track, SafeUri fromUri)
        {
            if (track.PrimarySourceId == DbId)
                return;

            SafeUri new_uri = new SafeUri (GetTrackPath (track, System.IO.Path.GetExtension (fromUri.LocalPath)));
            // If it already is on the device but it's out of date, remove it
            //if (File.Exists(new_uri) && File.GetLastWriteTime(track.Uri.LocalPath) > File.GetLastWriteTime(new_uri))
                //RemoveTrack(new MassStorageTrackInfo(new SafeUri(new_uri)));

            if (!File.Exists (new_uri)) {
                Directory.Create (System.IO.Path.GetDirectoryName (new_uri.LocalPath));
                File.Copy (fromUri, new_uri, false);

                DatabaseTrackInfo copied_track = new DatabaseTrackInfo (track);
                copied_track.PrimarySource = this;
                copied_track.Uri = new_uri;
                copied_track.Save (false);
            }
        }

        protected override void DeleteTrack (DatabaseTrackInfo track)
        {
            try {
                Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (track.Uri);
            } catch (System.IO.FileNotFoundException) {
            } catch (System.IO.DirectoryNotFoundException) {
            }
        }

        protected override void Eject ()
        {
            if (volume.CanUnmount) {
                volume.Unmount ();
            }

            if (volume.CanEject) {
                volume.Eject ();
            }
        }

        protected int FolderDepth {
            get { return MediaCapabilities == null ? -1 : MediaCapabilities.FolderDepth; }
        }

        private string GetTrackPath (TrackInfo track, string ext)
        {
            string file_path = WritePath;

            /*string artist = FileNamePattern.Escape (track.ArtistName);
            string album = FileNamePattern.Escape (track.AlbumTitle);
            string number_title = FileNamePattern.Escape (track.TrackNumberTitle);

            // If the folder_depth property exists, we have to put the files in a hiearchy of
            // the exact given depth (not including the mount point/audio_folder).
            if (FolderDepth != -1) {
                int depth = FolderDepth;

                if (depth == 0) {
                    // Artist - Album - 01 - Title
                    file_path = System.IO.Path.Combine (file_path, String.Format ("{0} - {1} - {2}", artist, album, number_title));
                } else if (depth == 1) {
                    // Artist - Album/01 - Title
                    file_path = System.IO.Path.Combine (file_path, String.Format ("{0} - {1}", artist, album));
                    file_path = System.IO.Path.Combine (file_path, number_title);
                } else if (depth == 2) {
                    // Artist/Album/01 - Title
                    file_path = System.IO.Path.Combine (file_path, artist);
                    file_path = System.IO.Path.Combine (file_path, album);
                    file_path = System.IO.Path.Combine (file_path, number_title);
                } else {
                    // If the *required* depth is more than 2..go nuts!
                    for (int i = 0; i < depth - 2; i++) {
                        file_path = System.IO.Path.Combine (file_path, artist.Substring (0, Math.Min (i, artist.Length)).Trim ());
                    }

                    // Finally add on the Artist/Album/01 - Track
                    file_path = System.IO.Path.Combine (file_path, artist);
                    file_path = System.IO.Path.Combine (file_path, album);
                    file_path = System.IO.Path.Combine (file_path, number_title);
                }
            } else {
                file_path = System.IO.Path.Combine (file_path, FileNamePattern.CreateFromTrackInfo (track));
            }
            */

            file_path = System.IO.Path.Combine (file_path, FileNamePattern.CreateFromTrackInfo (track));
            file_path += ext;

            return file_path;
        }
    }
}
