/***************************************************************************
 *  Album.cs
 *
 *  Copyright (C) 2008 Novell
 *  Authors:
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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mtp
{
	public class Album
	{
		private AlbumStruct album;
		private MtpDevice device;
        private bool saved;
        private List<int> track_ids;

        public uint AlbumId {
            get { return saved ? album.album_id : 0; }
        }

        public List<int> TrackIds {
            get { return track_ids; }
        }

        public bool Saved {
            get { return saved; }
        }

        public string Name {
            get { return album.name; }
            set {
                album.name = value;
            }
        }

        public string Artist {
            get { return album.artist; }
            set {
                album.artist = value;
            }
        }

        public string Genre {
            get { return album.genre; }
            set {
                album.genre = value;
            }
        }

        public uint TrackCount {
            get { return album.no_tracks; }
            set {
                album.no_tracks = value;
            }
        }

        public Album (MtpDevice device, string name, string artist, string genre)
        {
            this.device = device;
            this.album = new AlbumStruct ();
            Name = name;
            Artist = artist;
            Genre = genre;
            TrackCount = 0;
            track_ids = new List<int> ();
        }

        internal Album (MtpDevice device, AlbumStruct album)
        {
            this.device = device;
            this.album = album;
            this.saved = true;

            if (album.tracks != IntPtr.Zero) {
                int [] vals = new int [TrackCount];
                Marshal.Copy ((IntPtr)album.tracks, (int[])vals, 0, (int)TrackCount);
                track_ids = new List<int> (vals);
            } else {
                track_ids = new List<int> ();
            }
        }
        
        public void Save ()
        {
            Save (null, 0, 0);
        }

        public void Save (byte [] cover_art, uint width, uint height)
        {
            TrackCount = (uint) track_ids.Count;

            if (album.tracks != IntPtr.Zero) {
                Marshal.FreeHGlobal (album.tracks);
            }

            if (TrackCount == 0) {
                album.tracks = IntPtr.Zero;
            } else {
                album.tracks = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (int)) * (int)TrackCount);
                Marshal.Copy (track_ids.ToArray (), 0, album.tracks, (int)TrackCount);
            }

            if (saved) {
                saved = LIBMTP_Update_Album (device.Handle, album) == 0;
            } else {
                saved = LIBMTP_Create_New_Album (device.Handle, ref album, 0) == 0;
            }

            if (album.tracks != IntPtr.Zero) {
                Marshal.FreeHGlobal (album.tracks);
            }

            if (!saved)
                return;

            if (cover_art == null) {
                return;
            }
            
            FileSampleData cover = new FileSampleData ();
            cover.data = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (byte)) * cover_art.Length);
            Marshal.Copy (cover_art, 0, cover.data, cover_art.Length);
            cover.size = (ulong)cover_art.Length;
            cover.width = width;
            cover.height = height;
            cover.filetype = FileType.JPEG;

            if (FileSample.LIBMTP_Send_Representative_Sample (device.Handle, AlbumId, ref cover) != 0) {
                //Console.WriteLine ("failed to send representative sample file");
            }
            Marshal.FreeHGlobal (cover.data);
        }

        public void AddTrack (Track track)
        {
            track_ids.Add ((int)track.FileId);
            TrackCount++;
        }

        public void RemoveTrack (Track track)
        {
            track_ids.Remove ((int)track.FileId);
            TrackCount--;
        }

        public void ClearTracks ()
        {
            track_ids.Clear ();
            TrackCount = 0;
        }

        public void Remove ()
        {
			MtpDevice.LIBMTP_Delete_Object(device.Handle, AlbumId);
        }

        public override string ToString ()
        {
            return String.Format ("Album < Id: {4}, '{0}' by '{1}', genre '{2}', tracks {3} >", Name, Artist, Genre, TrackCount, AlbumId);
        }

		[DllImport("libmtp.dll")]
		internal static extern IntPtr LIBMTP_new_album_t (); // LIBMTP_album_t*

		[DllImport("libmtp.dll")]
		internal static extern void LIBMTP_destroy_album_t (ref AlbumStruct album);

		[DllImport("libmtp.dll")]
		internal static extern IntPtr LIBMTP_Get_Album_List (MtpDeviceHandle handle); // LIBMTP_album_t*

		[DllImport("libmtp.dll")]
		internal static extern IntPtr LIBMTP_Get_Album (MtpDeviceHandle handle, uint albumId); // LIBMTP_album_t*

		[DllImport("libmtp.dll")]
		internal static extern int LIBMTP_Create_New_Album (MtpDeviceHandle handle, ref AlbumStruct album, uint parentId);

		[DllImport("libmtp.dll")]
		internal static extern int LIBMTP_Update_Album (MtpDeviceHandle handle, AlbumStruct album);
	}

    [StructLayout(LayoutKind.Sequential)]
    internal struct AlbumStruct
    {
        public uint album_id;

        [MarshalAs(UnmanagedType.LPStr)]
        public string name;

        [MarshalAs(UnmanagedType.LPStr)]
        public string artist;

        [MarshalAs(UnmanagedType.LPStr)]
        public string genre;

        public IntPtr tracks;
        public uint no_tracks;

        public IntPtr next; // LIBMTP_album_t*
    }
}
