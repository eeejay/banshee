/***************************************************************************
 *  MtpDap.cs
 *
 *  Copyright (C) 2006 Novell and Patrick van Staveren
 *  Written by Patrick van Staveren (trick@vanstaveren.us)
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Hal;
using LibGPhoto2;
using Banshee.Dap;
using Banshee.Base;
using Banshee.Widgets;
using Banshee.Sources;
using Mono;
using Mono.Unix;
using Gtk;

namespace Banshee.Dap.Mtp
{
    [DapProperties(DapType = DapType.NonGeneric)]
    [SupportedCodec(CodecType.Mp3)]
//    [SupportedCodec(CodecType.Wav)]
    [SupportedCodec(CodecType.Wma)]

    public sealed class MtpDap : DapDevice, IImportable
    {
        private static GPhotoDevice dev;
        private DeviceId device_id;
        private int sync_total;
        private int sync_finished;
        private Queue remove_queue = new Queue();
        private ActiveUserEvent userEvent;
        private int GPhotoDeviceID;
        private Hal.Device hal_device;
        private QueuedOperationManager import_manager;
        
        static MtpDap() {
        }

        public override InitializeResult Initialize(Hal.Device halDevice)
        {
            if(!halDevice.PropertyExists("usb.vendor_id") ||
                    !halDevice.PropertyExists("usb.product_id")) {
                return InitializeResult.Invalid;
            }
            
            short hal_product_id = (short) halDevice.GetPropertyInt("usb.product_id");
            short hal_vendor_id  = (short) halDevice.GetPropertyInt("usb.vendor_id");
            
            device_id = DeviceId.GetDeviceId(hal_vendor_id, hal_product_id);

            if(device_id == null)
                return InitializeResult.Invalid;

            try {
                if (dev == null)
                    dev = new GPhotoDevice ();
                dev.Detect ();
            } catch (Exception e) {
                Console.WriteLine("Failed to run libgphoto2 DetectCameras.\nException: {0}", e.ToString());
                return InitializeResult.Invalid;
            }

            LogCore.Instance.PushDebug(String.Format("MTP: device found: vendor={0}, prod={1}", hal_vendor_id, hal_product_id), "");
            
            int found = 0;
            GPhotoDeviceID = -1;
            
            for (int i = 0; i < dev.CameraList.Count(); i++) {
                int abilities_index = dev.AbilitiesList.LookupModel(dev.CameraList.GetName(i));
                CameraAbilities a = dev.AbilitiesList.GetAbilities(abilities_index);
                if (a.usb_vendor != hal_vendor_id || a.usb_product != hal_product_id)
                    return InitializeResult.Invalid;
                found++;
                GPhotoDeviceID = i;
            }
            
            if (found > 1)
                LogCore.Instance.PushWarning (String.Format("MTP: Found more than one matching device.  Something is seriously wrong.  GPhotoDeviceID == {0}, # found = {1}", GPhotoDeviceID, found), "");
            if (found == 0 || GPhotoDeviceID == -1) {
                LogCore.Instance.PushDebug (String.Format("MTP: device was found in database, but libgphoto2 failed to detect it.  Waiting for it to come alive.  GPhotoDeviceID == {0}, # found = {1}", GPhotoDeviceID, found), "");
                return InitializeResult.WaitForPropertyChange;
            }
            
            InstallProperty("Model", device_id.Name);
            InstallProperty("Vendor", halDevice["usb.vendor"]);
            //InstallProperty("Firmware Revision", "FIXME: implement");
            //InstallProperty("Hardware Revision", "FIXME: implement");
            InstallProperty("Serial Number", halDevice["usb.serial"]);
            hal_device = halDevice;
            ThreadAssist.Spawn(InitializeBackgroundThread);

            CanCancelSave = false;
            return InitializeResult.Valid;
        }

        public void InitializeBackgroundThread()
        {
            userEvent = new ActiveUserEvent("MTP Initialization");
            userEvent.CanCancel = false;
            userEvent.Header = Catalog.GetString(device_id.Name + ": Found");
            userEvent.Message = Catalog.GetString("Reading library information");
            try{
                dev.SelectCamera(GPhotoDeviceID);
                dev.InitializeCamera();
            } catch (Exception e){
                Console.WriteLine("MTP: initialization failed with exception: {0}", e);
                LogCore.Instance.PushWarning(String.Format("Initialization of your {0} failed.  Run banshee from a terminal, and copy the debug output and file a new bug report on bugzilla.gnome.org", device_id.Name), "");
                userEvent.Dispose();
                Dispose();
            }

            userEvent.Message = Catalog.GetString("Loading device");
            base.Initialize(hal_device);
            ReloadDatabase();

            userEvent.Message = Catalog.GetString("Done");
            userEvent.Header = Catalog.GetString(device_id.Name + ": Ready for use");
            userEvent.Progress = 1;
            GLib.Timeout.Add(4000, delegate {
                userEvent.Dispose();
                userEvent = null;
                return false;
            });
        }

        public override void Dispose()
        {
            dev.Dispose();
            base.Dispose();
        }

        private void ReloadDatabase()
        {
            ClearTracks(false);
            
            foreach (GPhotoDeviceFile file in dev.FileList)
            {
                MtpDapTrackInfo track = new MtpDapTrackInfo(file);
                AddTrack(track);
            }
        }

        protected override void OnTrackRemoved(TrackInfo track)
        {
            if (IsReadOnly || !(track is MtpDapTrackInfo))
                return;

            remove_queue.Enqueue(track);
        }

        public override void AddTrack (TrackInfo track)
        {
            if (track is MtpDapTrackInfo || !TrackExistsInList(track, Tracks)) {
                tracks.Add (track);
                OnTrackAdded (track);
            }
        }

        public override void Synchronize()
        {
        try {
            int remove_total = remove_queue.Count;
            
            while(remove_queue.Count > 0) {
                MtpDapTrackInfo track = remove_queue.Dequeue() as MtpDapTrackInfo;
                UpdateSaveProgress(Catalog.GetString("Synchronizing Device"), Catalog.GetString(String.Format("Removing: {0} - {1}", track.Artist, track.Title)),
                    (double)(remove_total - remove_queue.Count) / (double)remove_total);
                dev.DeleteFile(track.DeviceFile);
            }
            
            sync_finished = 0;
            sync_total = 0;

            foreach(TrackInfo track in Tracks) {
                if(track is MtpDapTrackInfo) {
                    continue;
                }
                
                sync_total++;
            }
            
            foreach(TrackInfo track in Tracks) {
                if(track == null ||  track is MtpDapTrackInfo || track.Uri == null) {
                    continue;
                }
                
                FileInfo file;
                
                try {
                    file = new FileInfo(track.Uri.LocalPath);
                    if(!file.Exists) {
                        continue;
                    }
                } catch {
                    continue;
                }
                
                try {
                    UpdateSaveProgress(Catalog.GetString("Synchronizing Device"),
                        Catalog.GetString(String.Format("Adding: {0} - {1}", track.Artist, track.Title)),
                        (double) sync_finished / (double) sync_total);

                    /*  this appears to be the most logical path for my Zen Micro
                        LMK if your device traditionally uses something different and it'll be changed :)
                    */
                    
                    GPhotoDeviceFile newfile = new GPhotoDeviceFile(track.Uri, dev);
                    
                    newfile.Duration = track.Duration.TotalMilliseconds;
                    newfile.Artist = track.Artist;
                    newfile.AlbumName = track.Album;
                    newfile.Name = track.Title;
                    newfile.Track = track.TrackNumber;
                    newfile.Genre = track.Genre;
                    newfile.UseCount = track.PlayCount;
                    newfile.Year = track.Year;

                    dev.PutFile(newfile);

                    sync_finished++;
                } catch (Exception e){
                    Console.WriteLine("Could not sync song: Exception: {0}", e.ToString());   
                }
            }
            
        } catch(Exception e) {
            Console.WriteLine(e);
        } finally {
            FinishSave();
            ReloadDatabase();
        }   
        }

	public void Import(IEnumerable<TrackInfo> tracks, PlaylistSource playlist)
	{
            ArrayList temp_files = new ArrayList();
            
            if(playlist != null && playlist.Count == 0) {
                playlist.Rename(PlaylistUtil.GoodUniqueName(tracks));
                playlist.Commit();
            }
        
            if(import_manager == null) {
                import_manager = new QueuedOperationManager();
                import_manager.HandleActveUserEvent = false;
                //import_manager.UserEvent.Icon = GetIcon;
                import_manager.UserEvent.Header = String.Format(Catalog.GetString("Copying from {0}"), Name);
                import_manager.UserEvent.Message = Catalog.GetString("Scanning...");
                import_manager.OperationRequested += OnImportOperationRequested;
                import_manager.Finished += delegate {
                    foreach(string cur in temp_files)
                        File.Delete(cur);
                    import_manager = null;
                };
            }
            
            foreach(TrackInfo track in tracks) {
                (track as MtpDapTrackInfo).MakeFileUri();
                /*if(!track.Uri.IsLocalPath) {
                    tracks.Remove(track);
                } else {
                    temp_files.Add(track.Uri.LocalPath);
                }*/
            }
            
            foreach(TrackInfo track in tracks) {
                if(playlist == null) {
                    if(track.Uri.IsLocalPath){
                        import_manager.Enqueue(track);
                        temp_files.Add(track.Uri.LocalPath);
                    }
                } else {
                    import_manager.Enqueue(new KeyValuePair<TrackInfo, PlaylistSource>(track, playlist));
                }
            }
        }
        
        private void OnImportOperationRequested(object o, QueuedOperationArgs args)
        {
            TrackInfo track = null;
            PlaylistSource playlist = null;
            
            if(args.Object is TrackInfo) {
                track = args.Object as TrackInfo;
            } else if(args.Object is KeyValuePair<TrackInfo, PlaylistSource>) {
                KeyValuePair<TrackInfo, PlaylistSource> container = 
                    (KeyValuePair<TrackInfo, PlaylistSource>)args.Object;
                track = container.Key;
                playlist = container.Value;
            }
            
            import_manager.UserEvent.Progress = import_manager.ProcessedCount / (double)import_manager.TotalCount;
            import_manager.UserEvent.Message = String.Format("{0} - {1}", track.Artist, track.Title);
            
            string from = track.Uri.LocalPath;
            string to = FileNamePattern.BuildFull(track, Path.GetExtension(from));
            
            try {
                if(File.Exists(to)) {
                    FileInfo from_info = new FileInfo(from);
                    FileInfo to_info = new FileInfo(to);
                    
                    // probably already the same file
                    if(from_info.Length == to_info.Length) {
                        try {
                            new LibraryTrackInfo(new SafeUri(to, false), track);
                        } catch {
                            // was already in the library
                        }
                        
                        return;
                    }
                }
            
                using(FileStream from_stream = new FileStream(from, FileMode.Open, FileAccess.Read)) {
                    long total_bytes = from_stream.Length;
                    long bytes_read = 0;
                    
                    using(FileStream to_stream = new FileStream(to, FileMode.Create, FileAccess.ReadWrite)) {
                        byte [] buffer = new byte[8192];
                        int chunk_bytes_read = 0;
                        
                        DateTime last_message_pump = DateTime.MinValue;
                        TimeSpan message_pump_delay = TimeSpan.FromMilliseconds(500);
                        
                        while((chunk_bytes_read = from_stream.Read(buffer, 0, buffer.Length)) > 0) {
                            to_stream.Write(buffer, 0, chunk_bytes_read);
                            bytes_read += chunk_bytes_read;
                            
                            if(DateTime.Now - last_message_pump < message_pump_delay) {
                                continue;
                            }
                            
                            double tracks_processed = (double)import_manager.ProcessedCount;
                            double tracks_total = (double)import_manager.TotalCount;
                            
                            import_manager.UserEvent.Progress = (tracks_processed / tracks_total) +
                                ((bytes_read / (double)total_bytes) / tracks_total);
                                
                            if(import_manager.UserEvent.IsCancelRequested) {
                                throw new QueuedOperationManager.OperationCanceledException();
                            }
                            
                            last_message_pump = DateTime.Now;
                        }
                    }
                }
                
                LibraryTrackInfo library_track = new LibraryTrackInfo(new SafeUri(to, false), track);
                if(playlist != null) {
                    playlist.AddTrack(library_track);
                    playlist.Commit();
                }
            } catch(Exception e) {
                try {
                    File.Delete(to);
                } catch {
                }
                
                if(e is QueuedOperationManager.OperationCanceledException) {
                    return;
                }
                
                args.Abort = true;
                
                LogCore.Instance.PushError(String.Format(Catalog.GetString("Cannot import track from {0}"), 
                    Name), e.Message);
                Console.Error.WriteLine(e);
            } 
        }
        
        public void Import(IEnumerable<TrackInfo> tracks)
        {
            Import(tracks, null);
        }

        public override Gdk.Pixbuf GetIcon(int size)
        {
            string prefix = "multimedia-player-";
            string id = "dell-pocket-dj";
            Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(prefix + id, size);
            return icon == null? base.GetIcon(size) : icon;
        }

        public override string Name {
            get {
                return device_id.DisplayName;
            }
        }

        /* FIXME: SetOwner not implemented in libgphoto2-ptp

        public override void SetOwner (string owner) {
            
            Console.WriteLine("fixme: SetOwner to {0}", owner);
        }*/

        /* FIXME: Owner not implemented in libgphoto2-ptp
        
        public override string Owner {
            get {
                return "Unknown";  
            }
        }*/

        public override string GenericName {
            get {
                return device_id.DisplayName;
            }
        }

        public override ulong StorageCapacity {
            get {
                return (ulong) dev.DiskTotal;
            }
        }

        public override ulong StorageUsed {
            get {
                return (ulong) dev.DiskTotal - (ulong) dev.DiskFree;
            }
        }

        public override bool IsReadOnly {
            get {
                return false;
            }
        }

        public override bool IsPlaybackSupported {
            get {
                return false;
            }
        }
    }
}
