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
using System.Runtime.InteropServices;
using System.Threading;
using Hal;
using LibGPhoto2;
using Banshee.Dap;
using Banshee.Base;
using Banshee.Widgets;
using Mono;
using Mono.Unix;
using Gtk;

namespace Banshee.Dap.Mtp
{
    [DapProperties(DapType = DapType.NonGeneric)]
    [SupportedCodec(CodecType.Mp3)]
    [SupportedCodec(CodecType.Wma)]

    public sealed class MtpDap : DapDevice
    {
        private static GPhotoDevice dev;
        private DeviceId device_id;
        private int sync_total;
        private int sync_finished;
        private Queue remove_queue = new Queue();
        //private bool firstDatabaseLoad = true;
        private ActiveUserEvent userEvent;
        private int GPhotoDeviceID;
        private Hal.Device hal_device;
        
        static MtpDap() {
            Console.WriteLine("MtpDap made");
        }

        public override InitializeResult Initialize(Hal.Device halDevice)
        {
            Console.WriteLine("MtpDap: initialize...");
            if(/*       !halDevice.PropertyExists("usb.bus_number") ||
                    !halDevice.PropertyExists("usb.linux.device_number") ||*/
                    !halDevice.PropertyExists("usb.vendor_id") ||
                    !halDevice.PropertyExists("usb.product_id")) {
                return InitializeResult.Invalid;
            }

            try {
                dev = new GPhotoDevice ();
                dev.Detect ();
            } catch (Exception e) {
                Console.WriteLine("Failed to run libgphoto2 DetectCameras.  Exception: {0}", e.ToString());
                return InitializeResult.Invalid;
            }

            short hal_product_id = (short) halDevice.GetPropertyInt("usb.product_id");
            short hal_vendor_id  = (short) halDevice.GetPropertyInt("usb.vendor_id");
            
            device_id = DeviceId.GetDeviceId(hal_vendor_id, hal_product_id);
            Console.WriteLine("Device: vendor={0}, prod={1}", hal_vendor_id, hal_product_id);

            if(device_id == null) {
                Console.WriteLine("Device id is null.  This can mean one of two things:\n" + 
                    "(1) Your device is not supported by this MTP driver.\n" +
                    "(2) You improperly installed libgphoto2_port by enabling disk support, which conflicts with this driver.\n" +
                    "Contact the MTP developers for help.");
                return InitializeResult.Invalid;
            }
            
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
            
            if(found > 1)
                Console.WriteLine("Found more than one matching device.  File a bug!  Do you have more than one MTP DAP plugged in?                    Multiple MTP DAPs are not supported.");
            
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
            userEvent.Header = Catalog.GetString(device_id.Name + ": Initializing");
            userEvent.Message = Catalog.GetString("Reading library information");
            try{
                Console.WriteLine("libgphoto2/MTP: Selecting device {0}", GPhotoDeviceID);
                dev.SelectCamera(GPhotoDeviceID);
                dev.InitializeCamera();
            } catch (Exception e){
                Console.WriteLine("failed with exception: {0}", e);
                userEvent.Dispose();
                Dispose();
            }

            userEvent.Message = Catalog.GetString("Loading device");
            base.Initialize(hal_device);
            ReloadDatabase();

            userEvent.Message = Catalog.GetString("Ready for use");;
            userEvent.Header = Catalog.GetString(device_id.Name + ": Ready");
            userEvent.Progress = 1;
            GLib.Timeout.Add(3000, delegate {
                userEvent.Dispose();
                userEvent = null;
                return false;
            });
        }

        public override void Dispose()
        {
            Console.WriteLine("MTP: Dispose()");
            dev.Dispose();
            base.Dispose();
        }

        private void ReloadDatabase()
        {
            //if(firstDatabaseLoad){
                //Console.WriteLine("ReloadDatabase...must be first time!");
                //firstDatabaseLoad = false;
                ClearTracks(false);
                
                foreach (GPhotoDeviceFile file in dev.FileList)
                {
                    MtpDapTrackInfo track = new MtpDapTrackInfo(file);
                    AddTrack(track);
                }
            //}else{
                //Console.WriteLine("ReloadDatabase called again...doing nothing");
            //}
        }

        protected override void OnTrackRemoved(TrackInfo track)
        {
            if (IsReadOnly || !(track is MtpDapTrackInfo))
                return;

            Console.WriteLine("mtp: will remove {0}", track.Title);
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
            Console.WriteLine("Made it to Mtp.Synchronize()");
            int remove_total = remove_queue.Count;
            
            while(remove_queue.Count > 0) {
                Console.WriteLine("Removing...queue count at {0}", remove_queue.Count);
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
                    Console.WriteLine("MtpDap: about to add file...");

                    /*  this appears to be the most logical path for my Zen Micro
                        LMK if your device traditionally uses something different and it'll be changed :)
                    */
                    
                    /*  FIXME: MtpDap shouldn't be aware of the store_x prefix.
                        per gphoto devels, it may be removed in the future
                        MtpDap never needs to know about it.
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

                    Console.WriteLine("MtpDap: file add finished.");

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

        public override void SetOwner (string owner) {
            //FIXME: SetOwner not implemented in libgphoto2-ptp
            Console.WriteLine("fixme: SetOwner to {0}", owner);
        }

        public override string Owner {
            get {
                return "Unknown";  //FIXME: Owner not implemented in libgphoto2-ptp
            }
        }

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
