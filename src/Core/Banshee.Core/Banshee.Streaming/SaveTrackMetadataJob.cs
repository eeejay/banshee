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
            get { return String.Format (Catalog.GetString ("Saving tags for {0}"), track.TrackTitle); }
        }
        
        public SaveTrackMetadataJob (TrackInfo track)
        {
            this.track = track;
        }
    
        public void Run ()
        {
            if (!LibrarySchema.WriteMetadata.Get ()) {
                Console.WriteLine ("Skipping scheduled metadata write, preference disabled after scheduling");
                return;
            }
            
            // FIXME taglib# does not seem to handle writing metadata to video files well at all atm
            // so not allowing
            if ((track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                Hyena.Log.DebugFormat ("Avoiding 100% cpu bug with taglib# by not writing metadata to video file {0}", track);
                return;
            }
        
            // Note: this should be kept in sync with the metadata read in StreamTagger.cs
            TagLib.File file = StreamTagger.ProcessUri (track.Uri);
            if (file == null) {
                return;
            }
            
            file.Tag.Performers = new string [] { track.ArtistName };
            file.Tag.PerformersSort = new string [] { track.ArtistNameSort };
            file.Tag.Album = track.AlbumTitle;
            file.Tag.AlbumSort = track.AlbumTitleSort;
            file.Tag.AlbumArtists = track.AlbumArtist == null ? new string [0] : new string [] {track.AlbumArtist};
            file.Tag.AlbumArtistsSort = (track.AlbumArtistSort == null ? new string [0] : new string [] {track.AlbumArtistSort});
            // Bug in taglib-sharp-2.0.3.0: Crash if you send it a genre of "{ null }"
            // on a song with both ID3v1 and ID3v2 metadata. It's happy with "{}", though.
            // (see http://forum.taglib-sharp.com/viewtopic.php?f=5&t=239 )
            file.Tag.Genres = (track.Genre == null) ? new string[] {} : new string [] { track.Genre };
            file.Tag.Title = track.TrackTitle;
            file.Tag.TitleSort = track.TrackTitleSort;
            file.Tag.Track = (uint)track.TrackNumber;
            file.Tag.TrackCount = (uint)track.TrackCount;
            file.Tag.Composers = new string [] { track.Composer };
            file.Tag.Conductor = track.Conductor;
            file.Tag.Grouping = track.Grouping;
            file.Tag.Copyright = track.Copyright;
            file.Tag.Comment = track.Comment;
            file.Tag.Disc = (uint)track.DiscNumber;
            file.Tag.DiscCount = (uint)track.DiscCount;
            file.Tag.Year = (uint)track.Year;
            file.Tag.BeatsPerMinute = (uint)track.Bpm;
            
            SaveIsCompilation (file.Tag, track.IsCompilation);
            file.Save ();
        }
        
        private static void SaveIsCompilation (TagLib.Tag tag, bool is_compilation)
        {
            TagLib.Id3v2.Tag id3v2_tag = tag as TagLib.Id3v2.Tag;
            if (id3v2_tag != null) {
                id3v2_tag.IsCompilation = is_compilation;
                return;
            }

            TagLib.Mpeg4.AppleTag apple_tag = tag as TagLib.Mpeg4.AppleTag;
            if (apple_tag != null) {
                apple_tag.IsCompilation = is_compilation;
                return;
            }
        }
    }
}
