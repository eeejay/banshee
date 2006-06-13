/***************************************************************************
 *  MassStorageDap.cs
 *
 *  Copyright (C) 2006 Novell and Gabriel Burt
 *  Written by Gabriel Burt (gabriel.burt@gmail.com)
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
using System.IO;
using Hal;
using Mono.Unix;
using Banshee.Dap;
using Banshee.Base;

namespace Banshee.Dap.MassStorage
{
    // FIXME the codecs shouldn't be hard coded here, they should be set
    // by looking at the hal device's accepted formats
    [DapProperties(DapType = DapType.Generic)]
    [SupportedCodec(CodecType.Mp3)]
    [SupportedCodec(CodecType.Ogg)]
    public class MassStorageDap : DapDevice
    {
        private static Gnome.Vfs.VolumeMonitor monitor;

        private bool mounted = false, ui_initialized = false;

        static MassStorageDap () {
            if (!Gnome.Vfs.Vfs.Initialized)
                Gnome.Vfs.Vfs.Initialize();

            monitor = Gnome.Vfs.VolumeMonitor.Get ();
        }

        protected Hal.Device usb_device = null;
        protected Hal.Device player_device = null;
        protected Hal.Device volume_device = null;

        protected Gnome.Vfs.Volume volume = null;

        public override InitializeResult Initialize(Hal.Device halDevice)
        {
            volume_device = halDevice;

            try {
                player_device = Hal.Device.UdisToDevices (volume_device.Context, new string [] {volume_device ["info.parent"]}) [0];
                usb_device = Hal.Device.UdisToDevices (player_device.Context, new string [] {player_device ["storage.physical_device"]}) [0];
            } catch (Exception e) {
                return InitializeResult.Invalid;
            }

            if (!player_device.PropertyExists ("portable_audio_player.access_method") ||
                player_device ["portable_audio_player.access_method"] != "storage" ||
                !usb_device.PropertyExists("usb.vendor_id") ||
                !usb_device.PropertyExists("usb.product_id") ||
                !volume_device.PropertyExists("block.device")) {
                return InitializeResult.Invalid;
            }

            if (!volume_device.PropertyExists("block.device"))
                return InitializeResult.Invalid;
            
            if (!volume_device.PropertyExists ("volume.is_mounted") ||
                !volume_device.GetPropertyBool("volume.is_mounted"))
                 return InitializeResult.WaitForPropertyChange;

            // Detect player via HAL property or presence of .is_audo_player file in root
            if (player_device["portable_audio_player.access_method"] != "storage" &&
                !File.Exists(Path.Combine(MountPoint, ".is_audio_player"))) {                
                 return InitializeResult.Invalid;
            }

            volume = monitor.GetVolumeForPath(MountPoint);
            if (volume == null) {
                // Gnome VFS doesn't know volume is mounted yet
                monitor.VolumeMounted += OnVolumeMounted;
                is_read_only = true;
            } else {
                mounted = true;
                is_read_only = volume.IsReadOnly;
            }

            base.Initialize (usb_device);
 
            if (usb_device.PropertyExists("usb.vendor"))
                InstallProperty(Catalog.GetString("Vendor"), usb_device["usb.vendor"]);
            else if (player_device.PropertyExists("info.vendor"))
                InstallProperty(Catalog.GetString("Vendor"), player_device["info.vendor"]);

            if(!Globals.UIManager.IsInitialized) {
                Globals.UIManager.Initialized += OnUIManagerInitialized;
            } else {
                ui_initialized = true;
            }

            if (ui_initialized && mounted)
                ReloadDatabase ();

            // FIXME probably should be able to cancel at some point when you can actually sync
            CanCancelSave = false;
            return InitializeResult.Valid;
        }

        public void OnVolumeMounted(object o, Gnome.Vfs.VolumeMountedArgs args)
        {
            if(args.Volume.DevicePath == volume_device["block.device"]) {
                monitor.VolumeMounted -= OnVolumeMounted;
            
                volume = args.Volume;
                is_read_only = volume.IsReadOnly;

                mounted = true;

                if (ui_initialized)
                    ReloadDatabase ();
            }
        }

        public override void Dispose()
        {
            // FIXME anything else to do here?
            volume = null;
            base.Dispose();
        }

        private void OnUIManagerInitialized(object o, EventArgs args)
        {
            ui_initialized = true;
            if (mounted)
                ReloadDatabase ();
        }
 
        private void ReloadDatabase()
        {
            ClearTracks (false);

            ImportManager importer = new ImportManager ();
            importer.ImportRequested += HandleImportRequested;

            foreach (string music_dir in AudioFolders)
                importer.QueueSource (System.IO.Path.Combine (MountPoint, music_dir));
        }

        private void HandleImportRequested (object o, ImportEventArgs args)
        {
            try {
                TrackInfo track = new MassStorageTrackInfo (new SafeUri (args.FileName));
                args.ReturnMessage = String.Format("{0} - {1}", track.Artist, track.Title);

                AddTrack (track);
            } catch(Entagged.Audioformats.Exceptions.CannotReadException) {
                //Console.WriteLine(Catalog.GetString("Cannot Import") + ": {0}", args.FileName);
                args.ReturnMessage = Catalog.GetString("Scanning") + "...";
            } catch(Exception e) {
                //Console.WriteLine(Catalog.GetString("Cannot Import: {0} ({1}, {2})"), 
                    //args.FileName, e.GetType(), e.Message);
                args.ReturnMessage = Catalog.GetString("Scanning") + "...";
            }
        }

        public override void Synchronize()
        {
            Console.WriteLine ("Error: Synchronize called on MassStorage DAP");
        }

        public override void Eject ()
        {
            if(volume != null)
                volume.Unmount (UnmountCallback);
        }

        private void UnmountCallback (bool succeeded, string error, string detailed_error)
        {
            if (succeeded)
                volume.Eject (EjectCallback);
            else
                Console.WriteLine ("Failed to unmount.  {1} {2}", error, detailed_error);
        }

        private void EjectCallback (bool succeeded, string error, string detailed_error)
        {
            if (succeeded)
                base.Eject ();
            else
                Console.WriteLine ("Failed to eject.  {1} {2}", error, detailed_error);
        }

        public override void AddTrack(TrackInfo track)
        {
            if (track == null || IsReadOnly)
                return;

            if (track is MassStorageTrackInfo) {
                // If we're "adding" it when it's already on the device, then
                // we don't need to copy it
                tracks.Add(track);
                OnTrackAdded(track);
            } else {
                // Otherwise queue it up to be transferred over to the device
                Copier.Enqueue (track);
            }
        }
        
        private void HandleCopyRequested (object o, QueuedOperationArgs args)
        {
            TrackInfo track = args.Object as TrackInfo;

            if (track == null)
                return;
            
            try {
                string new_path = GetTrackPath (track);

                // If it already is on the device but it's out of date, remove it
                if (File.Exists (new_path) && File.GetLastWriteTime(track.Uri.LocalPath) > File.GetLastWriteTime(new_path))
                    RemoveTrack(new MassStorageTrackInfo(new SafeUri(new_path)));

                if (!File.Exists (new_path)) {
                        Directory.CreateDirectory (Path.GetDirectoryName (new_path));
                        File.Copy (track.Uri.LocalPath, new_path);
                }

                TrackInfo new_track = new MassStorageTrackInfo (new SafeUri (new_path));
                tracks.Add(new_track);
                OnTrackAdded(new_track);

                args.ReturnMessage = String.Format("{0} - {1}", track.Artist, track.Title);
            } catch (Exception e) {
                args.ReturnMessage = String.Format("Skipping Song", track.Artist, track.Title);
            }
        }

        protected override void OnTrackRemoved(TrackInfo track)
        {
            if (IsReadOnly)
                return;

            // FIXME shouldn't need to check for this
            // Make sure it's on the drive
            if (track.Uri.LocalPath.IndexOf (MountPoint) == -1)
                return;
            
            try {
                File.Delete(track.Uri.LocalPath);
                //Console.WriteLine ("Deleted {0}", track.Uri.LocalPath);
            } catch(Exception) {
                Console.WriteLine("Could not delete file: " + track.Uri.LocalPath);
            }

            // trim empty parent directories
            try {
                string old_dir = Path.GetDirectoryName(track.Uri.LocalPath);
                while(old_dir != null && old_dir != String.Empty) {
                    Directory.Delete(old_dir);
                    old_dir = Path.GetDirectoryName(old_dir);
                }
            } catch(Exception) {}
        }

        public override Gdk.Pixbuf GetIcon(int size)
        {
            string prefix = "multimedia-player";
            Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(prefix + ((IconId == null) ? "" : "-" + IconId), size);
            return icon == null ? base.GetIcon(size) : icon;
        }

        private string GetTrackPath (TrackInfo track)
        {
            string file_path = "";

            string artist = FileNamePattern.Escape (track.Artist);
            string album = FileNamePattern.Escape (track.Album);
            string number_title = FileNamePattern.Escape (track.TrackNumberTitle);

            // TODO the following needs to be updated to the most recent version of the HAL spec
            if (player_device.PropertyExists ("portable_audio_player.filepath_format")) {
                file_path = player_device.GetPropertyString ("portable_audio_player.filepath_format");
                file_path = file_path.Replace ("%Artist", artist);
                file_path = file_path.Replace ("%Album", album);

                if (file_path.IndexOf ("%Track") == -1) {
                    file_path = System.IO.Path.Combine (file_path, number_title);
                } else {
                    file_path = file_path.Replace ("%Track", number_title);
                }
            } else {
                file_path = System.IO.Path.Combine (artist, album);
                file_path = System.IO.Path.Combine (file_path, number_title);
            }

            file_path += Path.GetExtension (track.Uri.LocalPath);

            //Console.WriteLine ("for track {0} outpath is {1}", track.Uri.LocalPath, System.IO.Path.Combine (MountPoint, file_path));
            return System.IO.Path.Combine (MountPoint, file_path);
        }

        private QueuedOperationManager copier;
        public QueuedOperationManager Copier {
            get {
                if (copier == null) {
                    copier = new QueuedOperationManager ();
                    copier.ActionMessage = Catalog.GetString ("Copying Songs");
                    copier.ProgressMessage = Catalog.GetString ("Copying {0} of {1}");
                    copier.OperationRequested += HandleCopyRequested;
                }

                return copier;
            }
            set { copier = value; }
        }

        public virtual string IconId {
            get {
                return null;
            }
        }
 
        public override string Name {
            get {
                if (player_device.PropertyExists("info.product"))
                    return player_device["info.product"];

                if (volume_device.PropertyExists("volume.label") &&
                    volume_device["volume.label"].Length > 0)
                    return volume_device["volume.label"];


                return GenericName;
            }
        }
        
        public override string GenericName {
            get {
                return Catalog.GetString ("Audio Device");
            }
        }
        
        public override ulong StorageCapacity {
            get {
                return volume_device.GetPropertyUint64 ("volume.size");
            }
        }
        
        public ulong StorageFree {
            get {
                Mono.Unix.Native.Statvfs info;
                Mono.Unix.Native.Syscall.statvfs (MountPoint, out info);

                return (ulong) (info.f_bavail * info.f_bsize);
            }
        }
        
        public override ulong StorageUsed {
            get {
                return StorageCapacity - StorageFree;
            }
        }

        
        private bool is_read_only;
        public override bool IsReadOnly {
            get {
                return is_read_only;
            }
        }
        
        public override bool IsPlaybackSupported {
            get {
                return true;
            }
        }
        
        public override bool CanSynchronize {
            get { return false; }
        }

        public string MountPoint {
            get {
                return volume_device ["volume.mount_point"];
            }
        }

        // The path relative to the mount point where music is stored
        public string [] AudioFolders {
            get {
                if (! player_device.PropertyExists ("portable_audio_player.audio_folders"))
                    return new string [] {""};

                return player_device.GetPropertyStringList ("portable_audio_player.audio_folders");
            }
        }
    }

    //[DapProperties(DapType = DapType.NonGeneric)]
    //[SupportedCodec(CodecType.Mp3)]
    //[SupportedCodec(CodecType.Mp4)]
}
