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
	public class Album : AbstractTrackList
	{
		private AlbumStruct album;

        public uint AlbumId {
            get { return Saved ? album.album_id : 0; }
        }

        public override string Name {
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

        public override uint Count {
            get { return album.no_tracks; }
            protected set {
                album.no_tracks = value;
            }
        }

        protected override IntPtr TracksPtr {
            get { return album.tracks; }
            set { album.tracks = value; }
        }

        public Album (MtpDevice device, string name, string artist, string genre) : base (device, name)
        {
            this.album = new AlbumStruct ();
            TracksPtr = IntPtr.Zero;
            Name = name;
            Artist = artist;
            Genre = genre;
            Count = 0;
        }

        internal Album (MtpDevice device, AlbumStruct album) : base (device, album.tracks, album.no_tracks)
        {
            this.album = album;
        }

        public override void Save ()
        {
            Save (null, 0, 0);
        }

        public void Save (byte [] cover_art, uint width, uint height)
        {
            base.Save ();
            if (Saved) {
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

                if (FileSample.LIBMTP_Send_Representative_Sample (Device.Handle, AlbumId, ref cover) != 0) {
                    //Console.WriteLine ("failed to send representative sample file");
                }
                Marshal.FreeHGlobal (cover.data);
            }
        }

        protected override int Create ()
        {
#if LIBMTP8
            return LIBMTP_Create_New_Album (Device.Handle, ref album);
#else
            return LIBMTP_Create_New_Album (Device.Handle, ref album, 0);
#endif
        }

        protected override int Update ()
        {
            return LIBMTP_Update_Album (Device.Handle, ref album);
        }
        
        public void Remove ()
        {
			MtpDevice.LIBMTP_Delete_Object(Device.Handle, AlbumId);
        }

        public override string ToString ()
        {
            return String.Format ("Album < Id: {4}, '{0}' by '{1}', genre '{2}', tracks {3} >", Name, Artist, Genre, Count, AlbumId);
        }

        public static Album GetById (MtpDevice device, uint id)
        {
            IntPtr ptr = Album.LIBMTP_Get_Album (device.Handle, id);
            if (ptr == IntPtr.Zero) {
                return null;
            } else {
                return new Album (device, (AlbumStruct) Marshal.PtrToStructure(ptr, typeof (AlbumStruct)));
            }
        }

		[DllImport("libmtp.dll")]
		internal static extern IntPtr LIBMTP_new_album_t (); // LIBMTP_album_t*

		[DllImport("libmtp.dll")]
		internal static extern void LIBMTP_destroy_album_t (ref AlbumStruct album);

		[DllImport("libmtp.dll")]
		internal static extern IntPtr LIBMTP_Get_Album_List (MtpDeviceHandle handle); // LIBMTP_album_t*

		[DllImport("libmtp.dll")]
		internal static extern IntPtr LIBMTP_Get_Album (MtpDeviceHandle handle, uint albumId); // LIBMTP_album_t*

#if LIBMTP8
		[DllImport("libmtp.dll")]
		internal static extern int LIBMTP_Create_New_Album (MtpDeviceHandle handle, ref AlbumStruct album);
#else
		[DllImport("libmtp.dll")]
		internal static extern int LIBMTP_Create_New_Album (MtpDeviceHandle handle, ref AlbumStruct album, uint parentId);
#endif

		[DllImport("libmtp.dll")]
		internal static extern int LIBMTP_Update_Album (MtpDeviceHandle handle, ref AlbumStruct album);
	}

    [StructLayout(LayoutKind.Sequential)]
    internal struct AlbumStruct
    {
        public uint album_id;
#if LIBMTP8
        public uint parent_id;
        public uint storage_id;
#endif

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
