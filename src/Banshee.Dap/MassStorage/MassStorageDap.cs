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
    public class MassStorageDap : DapDevice
    {
		private static Gnome.Vfs.VolumeMonitor monitor;

		static MassStorageDap () {
			Gnome.Vfs.Vfs.Initialize ();
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

            if (player_device ["portable_audio_player.access_method"] != "storage" ||
				!usb_device.PropertyExists("usb.vendor_id") ||
                !usb_device.PropertyExists("usb.product_id") ||
				!volume_device.PropertyExists("block.device") ||
				!CheckDeviceMatches (usb_device)) {
				Console.WriteLine ("failed in first check: {0} {1} {2} {3} {4}", 
					player_device ["portable_audio_player.access_method"] != "storage",
					!usb_device.PropertyExists("usb.vendor_id"),
					!usb_device.PropertyExists("usb.product_id"),
					!volume_device.PropertyExists("block.device"),
					!CheckDeviceMatches (usb_device));
                return InitializeResult.Invalid;
            }

			if(!volume_device.GetPropertyBool("volume.is_mounted"))
                return InitializeResult.WaitForPropertyChange;

			string block_device = volume_device ["block_device"];
			foreach (Gnome.Vfs.Volume vol in monitor.MountedVolumes) {
				if (vol.DevicePath == block_device) {
					this.volume = vol;
					break;
				}
			}

			if (volume == null)
                return InitializeResult.Invalid;
            
			base.Initialize(usb_device);
 
            InstallProperty("Vendor", usb_device["usb.vendor"]);

            ReloadDatabase();
            
			// FIXME probably should be able to cancel at some point when you can actually sync
            CanCancelSave = false;
            return InitializeResult.Valid;
        }

		public virtual bool CheckDeviceMatches (Hal.Device device)
		{
			return true;
		}
        
        public override void Dispose()
        {
			// FIXME anything else to do here?
            volume = null;
            base.Dispose();
        }
 
        private void ReloadDatabase()
        {
            ClearTracks (false);

			string music_dir = System.IO.Path.Combine (MountPoint, MusicPath);
			
			ImportManager importer = new ImportManager ();
			importer.ImportRequested += HandleImportRequested;
			importer.QueueSource (music_dir);
		}

		private void HandleImportRequested (object o, ImportEventArgs args)
		{
            try {
                TrackInfo ti = new FileTrackInfo (new Uri (args.FileName));
                args.ReturnMessage = String.Format("{0} - {1}", ti.Artist, ti.Title);

				AddTrack (ti);
            } catch(Entagged.Audioformats.Exceptions.CannotReadException) {
                //Console.WriteLine(Catalog.GetString("Cannot Import") + ": {0}", args.FileName);
                args.ReturnMessage = Catalog.GetString("Scanning") + "...";
            } catch(Exception e) {
                //Console.WriteLine(Catalog.GetString("Cannot Import: {0} ({1}, {2})"), 
                    //args.FileName, e.GetType(), e.Message);
                args.ReturnMessage = Catalog.GetString("Scanning") + "...";
            }
		}

		// FIXME not implemented
        public override void Synchronize()
        {
            UpdateSaveProgress (
                String.Format (Catalog.GetString ("Synchronizing {0}"), Name),
                Catalog.GetString("Pre-processing tracks"),
                0.0);
        }

		public override void Eject ()
		{
			volume.Eject (EjectCallback);

			base.Eject ();
		}

		private void EjectCallback (bool succeeded, string error, string detailed_error)
		{
			Console.WriteLine ("bool succeeded = {0}, string error = {1}, string detailed_error = {2}", succeeded, error, detailed_error);
		}
        
		// FIXME handle this
		protected override TrackInfo OnTrackAdded(TrackInfo track)
        {
			return track;
		}
        
		// FIXME handle this
		protected override void OnTrackRemoved(TrackInfo track)
        {
		}

        public override Gdk.Pixbuf GetIcon(int size)
        {
            string prefix = "multimedia-player";
            Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(prefix + ((IconId == null) ? "" : "-" + IconId), size);
            return icon == null ? base.GetIcon(size) : icon;
        }

		public virtual string IconId {
			get {
				return null;
			}
		}
 
        public override string Name {
            get {
                if (volume_device.PropertyExists("volume.label"))
                    return volume_device["volume.label"];
				
				if (player_device.PropertyExists("info.product"))
                    return player_device["info.product"];

				return GenericName;
            }
        }
        
        public override string GenericName {
            get {
				return Catalog.GetString ("USB Audio Player");
            }
        }
        
        public override ulong StorageCapacity {
            get {
				return volume_device.GetPropertyUint64 ("volume.size");
            }
        }
        
        public override ulong StorageUsed {
            get {
				// FIXME this is wrong, it returns the same things as volume.size (eg capacity)
                return (ulong) volume_device.GetPropertyInt ("volume.num_blocks") * (ulong) volume_device.GetPropertyInt ("volume.block_size");
            }
        }
        
        public override bool IsReadOnly {
            get {
				return volume.IsReadOnly;
            }
        }
        
        public override bool IsPlaybackSupported {
            get {
                return true;
            }
        }
		
		public virtual string MountPoint {
			get {
				return volume_device ["volume.mount_point"];
			}
		}

		// The path relative to the mount point where music is stored
		public virtual string MusicPath {
			get {
				return "";
			}
		}
    }

    //[DapProperties(DapType = DapType.NonGeneric)]
    //[SupportedCodec(CodecType.Mp3)]
    //[SupportedCodec(CodecType.Mp4)]
}
