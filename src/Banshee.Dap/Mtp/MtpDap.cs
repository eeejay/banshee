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
    [SupportedCodec(CodecType.Wav)]
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
                LogCore.Instance.PushWarning ("MTP: Found more than one matching device.  Something is seriously wrong.", "");
            if (found == 0 || GPhotoDeviceID == -1) {
                LogCore.Instance.PushDebug ("MTP: device was found in database, but libgphoto2 failed to detect it.", "");
                return InitializeResult.Invalid;
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

            Console.WriteLine("MTP: will remove {0}", track.Title);
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

        public void Import(IList<TrackInfo> tracks, PlaylistSource playlist) {
             LogCore.Instance.PushError("Operation Not Supported", "Copying tracks from MTP DAP's has not been implemented yet.");
        }
        
        public void Import(IList<TrackInfo> tracks)
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
