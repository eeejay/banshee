/***************************************************************************
 *  AlbumSet.cs
 *
 *  Copyright (C) 2008 Novell, Inc.
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

using Mono.Unix;
using Gdk;

using Banshee.Dap;
using Banshee.Base;

using Mtp;

namespace Banshee.Dap.Mtp
{
    public class AlbumSet
    {
        private MtpDevice device;
        private List<Album> old_list;
        private List<Album> new_list = new List<Album> ();
        private Dictionary<string, Album> hash = new Dictionary<string, Album> ();
        private Dictionary<Album, TrackInfo> tracks = new Dictionary<Album, TrackInfo> ();

        public AlbumSet (MtpDevice device)
        {
            this.device = device;
            old_list = device.GetAlbums ();
            hash.Clear ();
        }

        public void Ref (MtpDapTrackInfo track)
        {
            string key = track.Album + track.Artist;
            if (!hash.ContainsKey (key)) {
                Album album = new Album (device, track.Album, track.Artist, track.Genre);
                new_list.Add (album);
                hash[key] = album;
            }

            hash [key].AddTrack (track.OriginalFile);
            tracks [hash[key]] = track;
        }

        /*public void Unref (MtpDapTrackInfo track)
        {
            Unref (track.OriginalFile);
        }

        public void Unref (Track track)
        {
            string key = track.Album + track.Artist;
            if (hash.ContainsKey (key)) {
                hash [key].RemoveTrack (track);
            }
        }*/

        public IEnumerable<double> Save ()
        {
            int MAX_THUMB_WIDTH = MtpDap.AlbumArtWidthSchema.Get ();
            double total = old_list.Count + new_list.Count;
            double current = 0;
            foreach (Album album in old_list) {
                album.Remove ();
                yield return (current++ / total);
            }

            foreach (Album album in new_list) {
                if (tracks.ContainsKey (album)) {
                    string cover_art_file = tracks[album].CoverArtFileName;
                    if (cover_art_file != null) {
                        Gdk.Pixbuf pic = new Gdk.Pixbuf (cover_art_file);

                        int new_h = pic.Height * MAX_THUMB_WIDTH / pic.Width;
                        Gdk.Pixbuf scaled = pic.ScaleSimple (MAX_THUMB_WIDTH, new_h, InterpType.Bilinear);

                        byte [] bytes = scaled.SaveToBuffer ("jpeg");
                        album.Save (bytes, (uint)scaled.Width, (uint)scaled.Height);
                        scaled.Dispose ();
                        pic.Dispose ();
                    }
                }
                yield return (current++ / total);
            }

            tracks.Clear ();
        }
    }
}
