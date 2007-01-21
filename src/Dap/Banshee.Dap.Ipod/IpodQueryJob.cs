/***************************************************************************
 *  IpodQueryJob.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
 *  Written by James Willcox <snorp@novell.com>
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
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Metadata;
using Banshee.Kernel;

namespace Banshee.Dap.Ipod
{
    public class IpodQueryJob : MetadataServiceJob
    {
        private IpodDapTrackInfo track;
        
        public IpodQueryJob(IBasicTrackInfo track)
        {
            Track = track;
            this.track = track as IpodDapTrackInfo; 
        }
        
        public override void Run()
        {
            if(track == null || track.CoverArtFileName != null) {
                return;
            }
            
            string album_artist_id = TrackInfo.CreateArtistAlbumID(track.Artist, track.Album, false);
            if(album_artist_id == null) {
                return;
            }

            IPod.ArtworkFormat format = null;

            foreach (IPod.ArtworkFormat f in track.Track.TrackDatabase.Device.LookupArtworkFormats (IPod.ArtworkUsage.Cover)) {
                if (format == null || f.Width > format.Width) {
                    format = f;
                }
            }

            if (format == null || !track.Track.HasCoverArt (format)) {
                return;
            }

            Gdk.Pixbuf pixbuf = IPod.ArtworkHelpers.ToPixbuf (format, track.Track.GetCoverArt (format));
            if (pixbuf.Save (Paths.GetCoverArtPath (album_artist_id), "jpeg")) {
                StreamTag tag = new StreamTag();
                tag.Name = CommonTags.AlbumCoverID;
                tag.Value = album_artist_id;
                
                AddTag(tag);
            }
        }
    }
}
