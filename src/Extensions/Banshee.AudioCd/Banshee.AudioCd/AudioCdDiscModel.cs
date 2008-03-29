//
// AudioCdDisc.cs
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
using Mono.Unix;

using MusicBrainz;
using Banshee.Hardware;
using Banshee.Collection;

namespace Banshee.AudioCd
{
    public class AudioCdDiscModel : MemoryTrackListModel
    {
        private IDiscVolume volume;
        
        public AudioCdDiscModel (IDiscVolume volume)
        {
            this.volume = volume;
            disc_title = Catalog.GetString ("Audio CD");
        }
        
        public void LoadModelFromDisc ()
        {
            LocalDisc mb_disc = LocalDisc.GetFromDevice (volume.DeviceNode);
            if (mb_disc == null) {
                throw new ApplicationException ("Could not read contents of the disc. Platform may not be supported.");
            }
            
            for (int i = 0, n = mb_disc.TrackDurations.Length; i < n; i++) {
                AudioCdTrackInfo track = new AudioCdTrackInfo (volume.DeviceNode, i);
                track.TrackNumber = i + 1;
                track.TrackCount = n;
                track.Duration = TimeSpan.FromSeconds (mb_disc.TrackDurations[i]);
                track.ArtistName = Catalog.GetString ("Unknown Artist");
                track.AlbumTitle = Catalog.GetString ("Unknown Album");
                track.TrackTitle = String.Format(Catalog.GetString ("Track {0}"), track.TrackNumber);
                Add (track);
            }
        }
        
        public IDiscVolume Volume {
            get { return volume; }
        }
        
        private string disc_title;
        public string Title {
            get { return disc_title; }
        }
        
        public int TrackCount {
            get { return 0; }
        }
    }
}
