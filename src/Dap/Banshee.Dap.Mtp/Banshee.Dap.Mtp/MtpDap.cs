/***************************************************************************
 *  MtpDap.cs
 *
 *  Copyright (C) 2006-2008 Novell and Patrick van Staveren
 *  Authors:
 *  Patrick van Staveren (trick@vanstaveren.us)
 *  Alan McGovern (alan.mcgovern@gmail.com)
 *  Gabriel Burt (gburt@novell.com)
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
using System.Text;
using System.Threading;

using Hal;
using Mono;
using Mono.Unix;
using Gtk;
using Gdk;

using Banshee.Dap;
using Banshee.Base;
using Banshee.Widgets;
using Banshee.Sources;
using Banshee.Configuration;

using Mtp;

public static class PluginModuleEntry
{
	public static Type [] GetTypes ()
	{
		return new Type [] { typeof (Banshee.Dap.Mtp.MtpDap) };
	}
}

namespace Banshee.Dap.Mtp
{
	[DapProperties (DapType = DapType.NonGeneric)]
	public sealed class MtpDap : DapDevice, IImportable//, IPlaylistCapable
	{
        private static MtpDap mtp_dap;

		private MtpDevice device;
        private Hal.Device hal_device;
		private List<MtpDapTrackInfo> metadataChangedQueue;
		private Queue<MtpDapTrackInfo> removeQueue;
		private List<MtpDapTrackInfo> all_tracks;

        private bool supports_jpegs = false;
        
        private string hal_name = String.Empty;

		public MtpDap ()
		{
			all_tracks = new List<MtpDapTrackInfo> ();
			metadataChangedQueue = new List<MtpDapTrackInfo> ();
			removeQueue = new Queue<MtpDapTrackInfo> ();			
		}
		
		public override InitializeResult Initialize (Hal.Device halDevice)
		{
            hal_device = halDevice;

            // Make sure it's an MTP device.
            if (!IsPortableAudioPlayerType (hal_device, "mtp")) {
				return InitializeResult.Invalid;
            }

            // libmtp only allows us to have one MTP device active
            if (mtp_dap != null) {
                LogCore.Instance.PushInformation(
                    Catalog.GetString ("MTP Support Ignoring Device"),
                    Catalog.GetString ("Banshee's MTP audio player support can only handle one device at a time."),
                    true
                );
				return InitializeResult.Invalid;
            }

            try {
                hal_name = hal_device.Parent ["info.product"];
            } catch {}

            int product_id = hal_device.GetPropertyInteger ("usb.product_id");
            int vendor_id = hal_device.GetPropertyInteger ("usb.vendor_id");
            string serial = hal_device ["usb.serial"];
			
			List<MtpDevice> devices = null;
			try {
				devices = MtpDevice.Detect ();
			} catch (TypeInitializationException ex) {
				LogCore.Instance.PushError (
                    Catalog.GetString ("Error Initializing MTP Device Support"),
                    Catalog.GetString ("There was an error intializing MTP device support.  See http://www.banshee-project.org/Guide/DAPs/MTP for more information.")
                );
				return InitializeResult.Invalid;
			} catch (Exception ex) {
				ShowGeneralExceptionDialog (ex);
				return InitializeResult.Invalid;
			}

            bool device_found = false;

            if (devices == null || devices.Count == 0) {
				LogCore.Instance.PushError (
                    Catalog.GetString ("Error Finding MTP Device Support"),
                    Catalog.GetString ("An MTP device was detected, but Banshee was unable to load support for it.")
                );
            } else {
                string mtp_serial = devices[0].SerialNumber;
                if (!String.IsNullOrEmpty (mtp_serial) && !String.IsNullOrEmpty (serial)) {
                    if (mtp_serial.Contains (serial)) {
                        device_found = true;
                        device = devices[0];
                        mtp_dap = this;
                    }
                }

                if (!device_found) {
                    LogCore.Instance.PushInformation(
                        Catalog.GetString ("MTP Support Ignoring Device"),
                        Catalog.GetString ("Banshee's MTP audio player support can only handle one device at a time."),
                        true
                    );
                }
            }

            if (!device_found) {
                return InitializeResult.Invalid;
            }

			LogCore.Instance.PushDebug ("Loading MTP Device",
                String.Format ("Name: {0}, ProductID: {1}, VendorID: {2}, Serial: {3}",
                    hal_name, product_id, vendor_id, serial
                )
            );

			base.Initialize (hal_device);

            List<string> extensions = new List<string>();
            List<string> mimetypes = new List<string>();

            FileType [] file_types = device.GetFileTypes ();
            StringBuilder format_sb = new StringBuilder ();
            bool first_format = true;
            foreach (FileType format in file_types) {
                if (format == FileType.JPEG) {
                    supports_jpegs = true;
                    continue;
                }

                string codec = Banshee.Dap.CodecType.GetCodec (format.ToString ().ToLower ());
                
                if(codec != null) {
                    extensions.AddRange (CodecType.GetExtensions(codec));
                    mimetypes.AddRange (CodecType.GetMimeTypes(codec));
                    if (first_format) {
                        first_format = false;
                    } else {
                        format_sb.Append (", ");
                    }
                    format_sb.Append (codec);
                }
            }

            SupportedExtensions = extensions.ToArray();
            SupportedPlaybackMimeTypes = mimetypes.ToArray();
			
			InstallProperty (Catalog.GetString ("Vendor"), hal_device["usb.vendor"]);
			InstallProperty (Catalog.GetString ("Model"), hal_name);
            InstallProperty (Catalog.GetString ("Audio Format(s)"), format_sb.ToString ());
            InstallProperty (Catalog.GetString ("Album Art"), supports_jpegs ? Catalog.GetString ("Yes") : Catalog.GetString ("No"));
			InstallProperty (Catalog.GetString ("Version"), device.Version);
			InstallProperty (Catalog.GetString ("Serial Number"), serial);

            // Don't continue until the UI is initialized
            if(!Globals.UIManager.IsInitialized) {
                Globals.UIManager.Initialized += OnUIManagerInitialized;
            } else {
                Reload ();
            }

			CanCancelSave = true;
			return InitializeResult.Valid;
		}

        private void OnUIManagerInitialized (object o, EventArgs args)
        {
            Globals.UIManager.Initialized -= OnUIManagerInitialized;
            Reload ();
        }

        private bool ejecting;
		public override void Eject ()
		{
            if (ejecting)
                return;
            ejecting = true;
            // TODO this isn't needed atm since we don't support playback directly off MTP devices
            UnmapPlayback(typeof(MtpDapTrackInfo));
            Dispose ();
			base.Eject ();
            ejecting = false;
		}

		public override void Dispose ()
        {
			device.Dispose ();
			base.Dispose ();
            mtp_dap = null;
		}
		
		private void OnMetadataChanged (object sender, EventArgs e)
		{
			MtpDapTrackInfo info = (MtpDapTrackInfo)sender;
			if (!metadataChangedQueue.Contains (info))
				metadataChangedQueue.Add (info);
		}



		protected override void Reload ()
		{
			// Clear the list of tracks that banshee keeps
            lock (Source.TracksMutex) {
                ClearTracks (false);
            }

			ActiveUserEvent user_event = new ActiveUserEvent (
                String.Format (Catalog.GetString ("Loading {0}"), Name)
            );

			try {
                List<Track> files = device.GetAllTracks (delegate (ulong current, ulong total, IntPtr data) {
                    user_event.Progress = (double)current / total;
                    return user_event.IsCancelRequested ? 1 : 0;
                });
                
                if (user_event.IsCancelRequested) {
                    return;
                }
                
                all_tracks = new List<MtpDapTrackInfo> (files.Count + 50);
                foreach (Track f in files) {
                    MtpDapTrackInfo track = new MtpDapTrackInfo (device, f);
                    track.Changed += OnMetadataChanged;
                    AddTrack (track);
                    all_tracks.Add (track);
                }
			} finally {
                user_event.Dispose ();
			}
		}
		
		protected override void OnTrackRemoved (TrackInfo track)
		{
			base.OnTrackRemoved (track);
			
			MtpDapTrackInfo t = track as MtpDapTrackInfo;
			if (IsReadOnly || t == null || !t.OnCamera (device)) {
				return;
            }

			// This means we have write access and the file is on the device.
			removeQueue.Enqueue ((MtpDapTrackInfo) track);
		}
		
		public override void AddTrack (TrackInfo track)
		{
			//FIXME: DO i need to check if i already have the track in the list?
			//if ((mtpTrack != null && mtpTrack.OnCamera (device)))
			//	return;
			
			base.AddTrack (track);
		}
		
		/*PL*
		private void AddDevicePlaylist (MtpDapPlaylistSource playlist) {
			this.Source.AddChildSource (playlist);
			playlists.Add (playlist);
		}

		public DapPlaylistSource AddPlaylist (Source source) {
			ArrayList playlist_tracks = new ArrayList ();

			foreach (TrackInfo track in source.Tracks) {
				if (!TrackExistsInList (track, Tracks)) {
					AddTrack (track);
					playlist_tracks.Add (track);
				} else {
					playlist_tracks.Add (find_existing_track (track) as TrackInfo);
				}
			}

			MtpDapPlaylistSource playlist = new MtpDapPlaylistSource (this, source.Name, playlist_tracks);
			playlists.Add (playlist);
			dev.Playlists.Add (playlist.GetDevicePlaylist ());
			
			this.Source.AddChildSource (playlist); // fixme: this should happen automatically in DapDevice or DapSource or something.
			return playlist;
		}
		*/

		private Track ToMusicFile (TrackInfo track, string name, ulong length)
		{
			// FIXME: Set the length properly
			// Fixme: update the reference i'm holding to the original music file?
			// Why am i holding it anyway?
			Track f = new Track (name, length);
            TrackInfoToMtpTrack (track, f);
            return f;
        }

        public void TrackInfoToMtpTrack (TrackInfo track, Track f)
        {
			f.Album = track.Album;
			f.Artist = track.Artist;
			f.Duration = (uint)track.Duration.TotalMilliseconds;
			f.Genre = track.Genre;
			f.Rating = (ushort)(track.Rating * 20);
			f.Title = track.Title;
			f.TrackNumber = (ushort)track.TrackNumber;
			f.UseCount = (uint)track.PlayCount;
            f.Date = track.Year + "0101T0000.0";
		}
		
		private void RemoveTracks ()
		{
			int count = removeQueue.Count;
			while (removeQueue.Count > 0) {
				MtpDapTrackInfo track = removeQueue.Dequeue ();
				string message = string.Format ("Removing: {0} - {1}", track.DisplayArtist, track.DisplayTitle);
				
				// Quick check to see if the track is on this device - possibly needed in
				// case we have multiple MTP devices connected simultaenously
				if (!track.OnCamera (device)) {
					continue;
                }

				device.Remove (track.OriginalFile);
				all_tracks.Remove (track);
				
				if (metadataChangedQueue.Contains (track)) {
					metadataChangedQueue.Remove (track);
                }
				
				track.Changed -= OnMetadataChanged;

				UpdateSaveProgress (sync_title, message, ((double) count - removeQueue.Count) / count);
				
				// Optimisation - Delete the folder if it's empty
			}
		}
		
		private void UploadTracks ()
		{
			// For all the tracks that are listed to upload, find only the ones
			// which exist and can be read.
			// FIXME: I can upload 'MtpDapTrackInfo' types. Just make sure they dont
			// exist on *this* device already
			List<TrackInfo> tracks = new List<TrackInfo> (Tracks);
			tracks = tracks.FindAll (delegate (TrackInfo t) {
				if (t == null || t is MtpDapTrackInfo || t.Uri == null) {
					return false;
                }
				return System.IO.File.Exists (t.Uri.LocalPath);
			});
			
			for (int i = 0; i < tracks.Count; i++) {
				FileInfo info = new FileInfo (tracks[i].Uri.AbsolutePath);
				Track f = ToMusicFile (tracks[i], info.Name, (ulong)info.Length);
				
				device.UploadTrack (tracks[i].Uri.AbsolutePath, f);
				
				// Create an MtpDapTrackInfo for the new file and add it to our lists
				MtpDapTrackInfo newTrackInfo = new MtpDapTrackInfo (device, f);
				newTrackInfo.Changed += OnMetadataChanged;
				
				UpdateSaveProgress (sync_title,
                    String.Format ("Adding: {0} - {1}", f.Artist, f.Title),
                    (double) (i + 1) / tracks.Count
                );
				all_tracks.Add (newTrackInfo);
				AddTrack (newTrackInfo);
			}
		}
		
		private void UpdateMetadata ()
		{
			try {
				for (int i = 0; i < metadataChangedQueue.Count; i++) {
					MtpDapTrackInfo info = metadataChangedQueue[i];
					Track file = info.OriginalFile;
                    TrackInfoToMtpTrack (info, file);
					file.UpdateMetadata ();
				}
			} finally {
				metadataChangedQueue.Clear ();
			}
		}

        private string sync_title;
		public override void Synchronize ()
		{
			// 1. remove everything in the remove queue if it's on the device
			// 2. Add everything in the tracklist that isn't on the device
			// 3. Sync playlists?
		    sync_title = String.Format (Catalog.GetString ("Synchronizing {0}"), Name);
			try {
				RemoveTracks ();
				UpdateMetadata ();
				UploadTracks ();

                if (supports_jpegs && NeverSyncAlbumArtSchema.Get () == false) {
                    UpdateSaveProgress (sync_title, Catalog.GetString ("Syncing album art"), 0);
                    AlbumSet album_set = new AlbumSet (device);
                    foreach (MtpDapTrackInfo track in all_tracks) {
                        album_set.Ref (track);
                    }

                    foreach (double percent in album_set.Save ()) {
                        UpdateSaveProgress (sync_title, Catalog.GetString ("Syncing album art"), percent);
                    }
                }
			} catch (Exception e) {
				LogCore.Instance.PushWarning (String.Format (
                    Catalog.GetString ("There was an unknown error while synchronizing {0}."), Name
                ), String.Empty);
				LogCore.Instance.PushDebug ("MTP Sync Error", e.ToString ());
			} finally {
				ClearTracks (false);

				for (int i = 0; i < all_tracks.Count; i++) {
					AddTrack (all_tracks[i]);
                }

				FinishSave ();
			}
		}
				
		private void ShowGeneralExceptionDialog (Exception ex)
		{
			LogCore.Instance.PushError (Catalog.GetString ("MTP Device Error"), ex.ToString ());
		}

		public void Import (IEnumerable<TrackInfo> tracks, PlaylistSource playlist) 
		{
			LogCore.Instance.PushDebug ("MTP: importing tracks", String.Empty);
			if (playlist != null) {
				LogCore.Instance.PushDebug ("Playlist importing not supported",
				                           "Banshee does not support importing playlists from MTP devices yet...");
            }
			
			QueuedOperationManager importer = new QueuedOperationManager ();
			
			importer = new QueuedOperationManager ();
			importer.HandleActveUserEvent = false;
			importer.UserEvent.Icon = GetIcon (22);
			importer.UserEvent.Header = String.Format (Catalog.GetString ("Importing from {0}"), Name);
			importer.UserEvent.Message = Catalog.GetString ("Scanning...");
			importer.OperationRequested += OnImportOperationRequested;
			importer.Finished += delegate {
				importer.UserEvent.Dispose ();
			};
			
			// For each track in the list, check to make sure it is on this MTP
			// device and then add it to the import queue.
			foreach (TrackInfo track in tracks) {
				if (! (track is MtpDapTrackInfo)) {
					LogCore.Instance.PushDebug ("Not MTP track", "Tried to import a non-mtp track");
                }
				
				if (! ((MtpDapTrackInfo) track).OnCamera (this.device)) {
					LogCore.Instance.PushDebug ("Track not on this device", "The track to import did not come from this device");
                }
				
				importer.Enqueue (track);
			}
		}
		
		private void OnImportOperationRequested (object o, QueuedOperationArgs args) 
		{
			if (!(args.Object is MtpDapTrackInfo)) {
				LogCore.Instance.PushDebug ("Import failure",
                    String.Format ("An attempt to import a '{0}' was detected. Can only import MtpDapTrackInfo objects",
                        args.Object.GetType ().Name
                    )
                );
				return;
			}
			
			QueuedOperationManager importer = (QueuedOperationManager)o;

			if (importer.UserEvent.IsCancelRequested) {
				return;
			}

			MtpDapTrackInfo track = (MtpDapTrackInfo)args.Object;
			
			importer.UserEvent.Progress = importer.ProcessedCount / (double)importer.TotalCount;
			importer.UserEvent.Message = string.Format ("{0}/{1}: {2} - {3}", importer.ProcessedCount, importer.TotalCount, track.DisplayArtist, track.DisplayTitle);
			
			// This is the path where the file will be saved on-disk
			string destination = FileNamePattern.BuildFull (track, Path.GetExtension (track.OriginalFile.Filename));
			
			try {
				if (System.IO.File.Exists (destination)) {
					FileInfo to_info = new FileInfo (destination);
					
					// FIXME: Probably already the same file. Is this ok?
					if (track.OriginalFile.Filesize == (ulong)to_info.Length) {
						try {
							new LibraryTrackInfo (new SafeUri (destination, false), track);
						} catch {
							// was already in the library
						}
						LogCore.Instance.PushDebug ("Import warning", String.Format (
                            "Track {0} - {1} - {2} already exists in the library",
						    track.DisplayArtist, track.DisplayAlbum, track.DisplayTitle
                        ));
						return;
					}
				}
			} catch (Exception ex) {
				LogCore.Instance.PushDebug ("Import Warning", "Could not check if the file already exists, skipping");
				LogCore.Instance.PushDebug ("Exception", ex.ToString ());
				return;
			}
			
			try {
				LogCore.Instance.PushDebug ("Import Operation", String.Format ("Importing song to {0}", destination));
				// Copy the track from the device to the destination file
				track.OriginalFile.Download (destination);
				
				// Add the track to the library
				new LibraryTrackInfo (new SafeUri (destination, false), track);
			} catch (Exception e) {
				try {
					LogCore.Instance.PushDebug ("Critical error", "Could not import tracks");
					LogCore.Instance.PushDebug ("Exception", e.ToString ());
					// FIXME: Is this ok?
					System.IO.File.Delete (destination);
				} catch {
					// Do nothing
				}
			}
		}
		
		public void Import (IEnumerable<TrackInfo> tracks)
        {
			Import (tracks, null);
		}

        string icon_name = "multimedia-player-dell-pocket-dj";
		public override Gdk.Pixbuf GetIcon (int size)
        {
            return IconThemeUtils.HasIcon (icon_name) ? IconThemeUtils.LoadIcon (icon_name, size) : base.GetIcon (size);
		}
/*
		public DapPlaylistSource AddPlaylist (Source playlist)
		{
			IPodPlaylistSource ips = new IPodPlaylistSource (this, playlist.Name);       		
			
			LogCore.Instance.PushDebug ("In IPodDap.AddPlaylist" , "");
			
			foreach (TrackInfo ti in playlist.Tracks) {
				LogCore.Instance.PushDebug ("Adding track " + ti.ToString () , " to new playlist " + ips.Name);
				IpodDapTrackInfo idti = new IpodDapTrackInfo (ti, device.TrackDatabase);
				ips.AddTrack (idti);
				AddTrack (idti);                
			}
			
			return (DapPlaylistSource) ips;
		}
*/

        public override void SetName(string name)
        {
            if (device != null) {
                device.Name = name;
            }
        }

		public override bool CanSynchronize {
			get { return true; }
		}

		public override string Name {
			get {
				if (device == null) {
                    return hal_name;
                }
				
				return device.Name;
			}
		}

		public override ulong StorageCapacity {
			get {
				if (device == null)
					return 0;
				
				ulong count = 0;
				foreach (DeviceStorage s in device.GetStorage ()) {
					count += s.MaxCapacity;
                }
				return count;
			}
		}

		public override ulong StorageUsed {
			get {
				if (device == null)
					return 0;
				ulong count = 0;
				foreach (DeviceStorage s in device.GetStorage ()) {
					count += s.MaxCapacity - s.FreeSpace;
                }
				return count;
			}
		}
		
		public override bool IsReadOnly {
			get { return false; }
		}

		public override bool IsPlaybackSupported {
			get { return false; }
		}

        public static readonly SchemaEntry<bool> NeverSyncAlbumArtSchema = new SchemaEntry<bool>(
            "plugins.mtp", "never_sync_albumart",
            false,
            "Album art disabled",
            "Regardless of device's capabilities, do not sync album art"
        );

        public static readonly SchemaEntry<int> AlbumArtWidthSchema = new SchemaEntry<int>(
            "plugins.mtp", "albumart_max_width",
            170,
            "Album art max width",
            "The maximum width to allow for album art."
        );

        static MtpDap () {
            // Make sure these get created
            NeverSyncAlbumArtSchema.Set (NeverSyncAlbumArtSchema.Get ());
            AlbumArtWidthSchema.Set (AlbumArtWidthSchema.Get ());
        }
	}
}
