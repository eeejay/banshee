//
// StreamTagger.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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

namespace Banshee.Streaming
{
    public static class StreamTagger
    {
        public static TagLib.File ProcessUri (SafeUri uri)
        {
            TagLib.File file = Banshee.IO.DemuxVfs.OpenFile (uri.IsLocalPath ? uri.LocalPath : uri.AbsoluteUri, 
                null, TagLib.ReadStyle.Average);

            if ((file.Properties.MediaTypes & TagLib.MediaTypes.Audio) == 0 && 
                (file.Properties.MediaTypes & TagLib.MediaTypes.Video) == 0) {
                throw new TagLib.UnsupportedFormatException ("File does not contain video or audio");
            }
            
            return file;
        }

        private static string Choose (string priority, string fallback)
        {
            return Choose (priority, fallback, false);
        }
    
        private static string Choose (string priority, string fallback, bool flip)
        {
            return flip 
                ? String.IsNullOrEmpty (fallback) ? priority : fallback
                : String.IsNullOrEmpty (priority) ? fallback : priority;
        }

        #pragma warning disable 0169
        
        private static int Choose (int priority, int fallback)
        {
            return Choose (priority, fallback, false);
        }
        
        #pragma warning restore 0169
        
        private static int Choose (int priority, int fallback, bool flip)
        {
            return flip 
                ? (fallback <= 0 ? priority : fallback)
                : (priority <= 0 ? fallback : priority);
        }
        
        private static void FindTrackMediaAttributes (TrackInfo track, TagLib.File file)
        {
            track.MediaAttributes = TrackMediaAttributes.None;
            
            if ((file.Properties.MediaTypes & TagLib.MediaTypes.Audio) != 0) {
                track.MediaAttributes |= TrackMediaAttributes.AudioStream;
            }
            
            if ((file.Properties.MediaTypes & TagLib.MediaTypes.Video) != 0) {
                track.MediaAttributes |= TrackMediaAttributes.VideoStream;
            }
            
            // TODO: Actually figure out, if possible at the tag/file level, if
            // the file is actual music, podcast, audiobook, movie, tv show, etc.
            // For now just assume that if it's only audio, it's music, since that's
            // what we've just historically assumed on any media type
            if ((track.MediaAttributes & TrackMediaAttributes.VideoStream) == 0) {
                track.MediaAttributes |= TrackMediaAttributes.Music;
            }
        }
        
        public static void TrackInfoMerge (TrackInfo track, TagLib.File file)
        {
            TrackInfoMerge (track, file, false);
        }

        public static void TrackInfoMerge (TrackInfo track, TagLib.File file, bool preferTrackInfo)
        {
            // Note: this should be kept in sync with the metadata written in SaveTrackMetadataJob.cs
            track.Uri = new SafeUri (file.Name);
            track.MimeType = file.MimeType;
            track.FileSize = Banshee.IO.File.GetSize (track.Uri);
            track.Duration = file.Properties.Duration;
            
            FindTrackMediaAttributes (track, file);

            track.ArtistName = Choose (file.Tag.JoinedPerformers, track.ArtistName, preferTrackInfo);
            track.AlbumTitle = Choose (file.Tag.Album, track.AlbumTitle, preferTrackInfo);
            track.TrackTitle = Choose (file.Tag.Title, track.TrackTitle, preferTrackInfo);
            track.Genre = Choose (file.Tag.FirstGenre, track.Genre, preferTrackInfo);
            track.Composer = Choose (file.Tag.FirstComposer, track.Composer, preferTrackInfo);
            track.Copyright = Choose (file.Tag.Copyright, track.Copyright, preferTrackInfo);
            track.Comment = Choose (file.Tag.Comment, track.Comment, preferTrackInfo);

            track.TrackNumber = Choose ((int)file.Tag.Track, track.TrackNumber, preferTrackInfo);
            track.TrackCount = Choose ((int)file.Tag.TrackCount, track.TrackCount, preferTrackInfo);
            track.Disc = Choose ((int)file.Tag.Disc, track.Disc, preferTrackInfo);
            track.Year = Choose ((int)file.Tag.Year, track.Year, preferTrackInfo);

            if (String.IsNullOrEmpty (track.TrackTitle)) {
                try {
                    string filename = System.IO.Path.GetFileName (track.Uri.LocalPath);
                    if (!String.IsNullOrEmpty (filename)) {
                        filename = filename.Substring (0, filename.LastIndexOf ('.'));
                        track.TrackTitle = filename;
                    }
                } catch {}
            }
        }
    
        public static void TrackInfoMerge (TrackInfo track, StreamTag tag)
        {
            try {
                switch (tag.Name) {
                    case CommonTags.Artist:
                        track.ArtistName = Choose ((string)tag.Value, track.ArtistName);
                        break;
                    case CommonTags.Title:
                        track.TrackTitle = Choose ((string)tag.Value, track.TrackTitle);
                        break;
                    case CommonTags.Album:
                        track.AlbumTitle = Choose ((string)tag.Value, track.AlbumTitle);
                        break;
                    case CommonTags.Disc:
                        int disc = (int)tag.Value;
                        track.Disc = disc == 0 ? track.Disc : disc;
                        break;
                    case CommonTags.Genre:
                        track.Genre = Choose ((string)tag.Value, track.Genre);
                        break;
                    case CommonTags.Composer:
                        track.Composer = Choose ((string)tag.Value, track.Composer);
                        break;
                    case CommonTags.Copyright:
                        track.Copyright = Choose ((string)tag.Value, track.Copyright);
                        break;
                    case CommonTags.LicenseUri:
                        track.LicenseUri = Choose ((string)tag.Value, track.LicenseUri);
                        break;
                    case CommonTags.Comment:
                        track.Comment = Choose ((string)tag.Value, track.Comment);
                        break;
                    case CommonTags.TrackNumber:
                        int track_number = (int)tag.Value;
                        track.TrackNumber = track_number == 0 ? track.TrackNumber : track_number;
                        break;
                    case CommonTags.TrackCount:
                        track.TrackCount = (int)tag.Value;
                        break;
                    case CommonTags.Duration:
                        if (tag.Value is TimeSpan) {
                            track.Duration = (TimeSpan)tag.Value;
                        } else {
                            track.Duration = new TimeSpan ((uint)tag.Value * TimeSpan.TicksPerMillisecond);
                        }
                        break;
                    case CommonTags.MoreInfoUri:
                        track.MoreInfoUri = (SafeUri)tag.Value;
                        break;
                    /* No year tag in GST it seems 
                    case CommonTags.Year:
                        track.Year = (uint)tag.Value;
                        break;*/
                    case CommonTags.StreamType:
                        track.MimeType = (string)tag.Value;
                        break;
                    /*case CommonTags.AlbumCoverId:
                        foreach(string ext in TrackInfo.CoverExtensions) {
                            string path = Paths.GetCoverArtPath((string) tag.Value, "." + ext);
                            if(System.IO.File.Exists(path)) {
                                track.CoverArtFileName = path;
                                break;
                            }
                        }
                        break;*/
                }
            } catch {
            }
        }
    }
}
