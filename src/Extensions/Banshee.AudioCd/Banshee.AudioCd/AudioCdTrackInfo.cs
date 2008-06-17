//
// AudioCdTrackInfo.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.AudioCd
{
    public class AudioCdTrackInfo : DatabaseTrackInfo
    {
        public AudioCdTrackInfo (AudioCdDiscModel model, string deviceNode, int index)
        {
            this.model = model;
            this.index_on_disc = index;
            
            Uri = new SafeUri (String.Format ("cdda://{0}#{1}", index_on_disc + 1, deviceNode)); 
        }
        
        public override bool TrackEqual (TrackInfo track)
        {
            AudioCdTrackInfo cd_track = track as AudioCdTrackInfo;
            return cd_track == null ? false : (cd_track.Model == Model && cd_track.IndexOnDisc == IndexOnDisc);
        }
        
        private AudioCdDiscModel model;
        public AudioCdDiscModel Model {
            get { return model; }
        }
        
        private int index_on_disc;
        public int IndexOnDisc {
            get { return index_on_disc; }
        }
        
        private DatabaseAlbumInfo album_info;
        public new DatabaseAlbumInfo Album {
            get { return album_info; }
            set { album_info = value; }
        }
        
        public DatabaseArtistInfo artist_info;
        public new DatabaseArtistInfo Artist {
            get { return artist_info; }
            set { artist_info = value; }
        }
        
        public override string AlbumMusicBrainzId {
            get { return Album == null ? null : Album.MusicBrainzId; }
        }

        public override string ArtistMusicBrainzId {
            get { return Artist == null ? null : Artist.MusicBrainzId; }
        }
        
        public override DateTime ReleaseDate {
            get { return Album == null ? base.ReleaseDate : Album.ReleaseDate; }
            set { 
                if (Album == null) {
                    base.ReleaseDate = value;
                } else {
                    Album.ReleaseDate = value;
                }
            }
        }

        private bool rip_enabled = true;
        public bool RipEnabled {
            get { return rip_enabled; }
            set { 
                if (rip_enabled == value) {
                    return;
                }
                
                rip_enabled = value;
                model.EnabledCount += rip_enabled ? 1 : -1;
            }
        }
    }
}
