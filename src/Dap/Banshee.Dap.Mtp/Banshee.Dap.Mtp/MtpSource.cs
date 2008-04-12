//
// MtpSource.cs
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
using System.Threading;
using Mono.Unix;

using Hyena;
using Hyena.Collections;
using Mtp;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Hardware;

namespace Banshee.Dap.Mtp
{
    public class MtpSource : DapSource
    {
        // libmtp only lets us have one device connected at a time
        private static MtpSource mtp_source;

		private MtpDevice mtp_device;
        //private bool supports_jpegs = false;
        private Dictionary<int, Track> track_map;

        public MtpSource () : base ()
        {
        }

        protected override bool Initialize (IDevice device)
        {
            if (MediaCapabilities == null || !MediaCapabilities.IsType ("mtp")) {
                return false;
            }

            // libmtp only allows us to have one MTP device active
            if (mtp_source != null) {
                Log.Information (
                    Catalog.GetString ("MTP Support Ignoring Device"),
                    Catalog.GetString ("Banshee's MTP audio player support can only handle one device at a time."),
                    true
                );
				return false;
            }

			List<MtpDevice> devices = null;
			try {
				devices = MtpDevice.Detect ();
			} catch (TypeInitializationException e) {
                Log.Exception (e);
				Log.Error (
                    Catalog.GetString ("Error Initializing MTP Device Support"),
                    Catalog.GetString ("There was an error intializing MTP device support.  See http://www.banshee-project.org/Guide/DAPs/MTP for more information.")
                );
				return false;
			} catch (Exception e) {
                Log.Exception (e);
                Log.Debug (e.ToString ());
				//ShowGeneralExceptionDialog (e);
				return false;
			}

            if (devices == null || devices.Count == 0) {
				Log.Error (
                    Catalog.GetString ("Error Finding MTP Device Support"),
                    Catalog.GetString ("An MTP device was detected, but Banshee was unable to load support for it.")
                );
            } else {
                string mtp_serial = devices[0].SerialNumber;
                if (!String.IsNullOrEmpty (mtp_serial)) {
                    if (mtp_serial.Contains (device.Uuid)) {
                        mtp_device = devices[0];
                        mtp_source = this;
                    }
                }

                if (mtp_device == null) {
                    Log.Information(
                        Catalog.GetString ("MTP Support Ignoring Device"),
                        Catalog.GetString ("Banshee's MTP audio player support can only handle one device at a time."),
                        true
                    );
                }
            }

            if (mtp_device == null) {
                return false;
            }

            Name = mtp_device.Name;
            Initialize ();

            //ServiceManager.DbConnection.Execute ("

            ThreadPool.QueueUserWorkItem (delegate {
                track_map = new Dictionary<int, Track> ();
                SetStatus (String.Format (Catalog.GetString ("Loading {0}"), Name), false);
                try {
                    List<Track> files = mtp_device.GetAllTracks (delegate (ulong current, ulong total, IntPtr data) {
                        //user_event.Progress = (double)current / total;
                        SetStatus (String.Format (Catalog.GetString ("Loading {0} - {1} of {2}"), Name, current, total), false);
                        return 0;
                    });
                    
                    /*if (user_event.IsCancelRequested) {
                        return;
                    }*/
                    
                    int [] source_ids = new int [] { DbId };
                    foreach (Track mtp_track in files) {
                        int track_id;
                        if ((track_id = DatabaseTrackInfo.GetTrackIdForUri (MtpTrackInfo.GetPathFromMtpTrack (mtp_track), source_ids)) > 0) {
                            track_map[track_id] = mtp_track;
                        } else {
                            MtpTrackInfo track = new MtpTrackInfo (mtp_track);
                            track.PrimarySource = this;
                            track.Save (false);
                            track_map[track.TrackId] = mtp_track;
                        }
                    }
                } catch (Exception e) {
                    Log.Exception (e);
                }
                OnTracksAdded ();
                HideStatus ();
            });

            return true;
        }

        public override void Import ()
        {
            Log.Information ("Import to Library is not implemented for MTP devices yet", true);
            //new LibraryImportManager (true).QueueSource (BaseDirectory);
        }

        public override bool CanRename {
            get { return !(IsAdding || IsDeleting); }
        }

        protected override void OnTracksDeleted ()
        {
            // Hack to get the disk usage indicate to be accurate, which seems to
            // only be updated when tracks are added, not removed.
            SafeUri empty_file = new SafeUri (Paths.Combine (Paths.ApplicationCache, "mtp.mp3"));
            try {
                using (System.IO.TextWriter writer = new System.IO.StreamWriter (Banshee.IO.File.OpenWrite (empty_file, true))) {
                    writer.Write ("foo");
                }
                Track mtp_track = new Track (System.IO.Path.GetFileName (empty_file.LocalPath), 3);
                mtp_device.UploadTrack (empty_file.AbsolutePath, mtp_track);
                mtp_device.Remove (mtp_track);
            } finally {
                Banshee.IO.File.Delete (empty_file);
            }

            base.OnTracksDeleted ();
        }

        public override void Rename (string newName)
        {
            base.Rename (newName);
            mtp_device.Name = newName;

            Console.WriteLine ("BytesUsed = {0}", BytesUsed);
        }

        public override long BytesUsed {
            get {
				long count = 0;
				foreach (DeviceStorage s in mtp_device.GetStorage ()) {
					count += (long) s.MaxCapacity - (long) s.FreeSpaceInBytes;
                }
				return count;
            }
        }
        
        public override long BytesCapacity {
            get {
				long count = 0;
				foreach (DeviceStorage s in mtp_device.GetStorage ()) {
					count += (long) s.MaxCapacity;
                }
				return count;
            }
        }

        protected override bool IsReadOnly {
            get { return false; }
        }

        protected override void AddTrack (DatabaseTrackInfo track)
        {
            if (track.PrimarySourceId == DbId)
                return;

            Track mtp_track = TrackInfoToMtpTrack (track);
            mtp_device.UploadTrack (track.Uri.AbsolutePath, mtp_track);

            MtpTrackInfo new_track = new MtpTrackInfo (mtp_track);
            new_track.PrimarySource = this;
            new_track.Save (false);
            track_map[new_track.TrackId] = mtp_track;
        }

        protected override void DeleteTrack (DatabaseTrackInfo track)
        {
            mtp_device.Remove (track_map [track.TrackId]);
            track_map.Remove (track.TrackId);
        }

        public Track TrackInfoToMtpTrack (TrackInfo track)
        {
			Track f = new Track (System.IO.Path.GetFileName (track.Uri.LocalPath), (ulong) track.FileSize);
			f.Album = track.AlbumTitle;
			f.Artist = track.ArtistName;
			f.Duration = (uint)track.Duration.TotalMilliseconds;
			f.Genre = track.Genre;
			f.Rating = (ushort)(track.Rating * 20);
			f.Title = track.TrackTitle;
			f.TrackNumber = (ushort)track.TrackNumber;
			f.UseCount = (uint)track.PlayCount;
            f.ReleaseDate = track.Year + "0101T0000.0";
            return f;
		}

        public override void Dispose ()
        {
			base.Dispose ();
			mtp_device.Dispose ();
            mtp_source = null;
        }

        protected override void Eject ()
        {
            Dispose ();
        }
    }
}
