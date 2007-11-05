//
// SaveTrackMetadataJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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

using Banshee.Collection;
using Banshee.Configuration.Schema;

namespace Banshee.Streaming
{
    public class SaveTrackMetadataJob : Banshee.Kernel.IInstanceCriticalJob
    {
        private TrackInfo track;
        
        public string Name {
            get { return String.Format(Catalog.GetString("Saving tags for {0}"), track.TrackTitle); }
        }
        
        public SaveTrackMetadataJob(TrackInfo track)
        {
            this.track = track;
        }
    
        public void Run()
        {
            if(!LibrarySchema.WriteMetadata.Get()) {
                Console.WriteLine("Skipping scheduled metadata write, preference disabled after scheduling");
                return;
            }
        
            TagLib.File file = StreamTagger.ProcessUri(track.Uri);
            file.Tag.AlbumArtists = new string [] { track.ArtistName };
            file.Tag.Album = track.AlbumTitle;
            file.Tag.Genres = new string [] { track.Genre };
            file.Tag.Title = track.TrackTitle;
            file.Tag.Track = (uint)track.TrackNumber;
            file.Tag.TrackCount = (uint)track.TrackCount;
            file.Tag.Year = (uint)track.Year;
            file.Save();
        }
    }
}
