//
// StreamTagger.cs
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

using Banshee.Base;
using Banshee.Collection;

namespace Banshee.Streaming
{
    public static class StreamTagger
    {
        public static TagLib.File ProcessUri(SafeUri uri)
        {
            string mimetype = null;
            
            try {
                mimetype = Banshee.IO.IOProxy.DetectMimeType(uri);
            } catch {
            }

            TagLib.File file = Banshee.IO.IOProxy.OpenFile(uri.IsLocalPath ? uri.LocalPath : uri.AbsoluteUri, 
                mimetype, TagLib.ReadStyle.Average);

            if (file.Properties.MediaTypes != TagLib.MediaTypes.Audio)
                throw new TagLib.UnsupportedFormatException ("File doesn't contain only audio");
            
            return file;
        }
    
        private static string Choose(string priority, string fallback)
        {
            return priority == null || priority.Length == 0 ? fallback : priority;
        }
        
        public static void TrackInfoMerge(TrackInfo track, TagLib.File file)
        {
            track.ArtistName = Choose(file.Tag.JoinedPerformers, track.ArtistName);
            track.AlbumTitle = Choose(file.Tag.Album, track.AlbumTitle);
            track.TrackTitle = Choose(file.Tag.Title, track.TrackTitle);
            track.Genre = Choose(file.Tag.FirstGenre, track.Genre);
            track.TrackNumber = file.Tag.Track == 0 ? track.TrackNumber : (int)file.Tag.Track;
            track.TrackCount = file.Tag.TrackCount == 0 ? track.TrackCount : (int)file.Tag.TrackCount;
            track.Year = (int)file.Tag.Year;
            track.MimeType = file.MimeType;
            
            track.Duration = file.Properties.Duration;
        }
    
        public static void TrackInfoMerge(TrackInfo track, StreamTag tag)
        {
            try {
                switch(tag.Name) {
                    case CommonTags.Artist:
                        track.ArtistName = Choose((string)tag.Value, track.ArtistName);
                        break;
                    case CommonTags.Title:
                        track.TrackTitle = Choose((string)tag.Value, track.TrackTitle);
                        break;
                    case CommonTags.Album:
                        track.AlbumTitle = Choose((string)tag.Value, track.AlbumTitle);
                        break;
                    case CommonTags.Genre:
                        track.Genre = Choose((string)tag.Value, track.Genre);
                        break;
                    case CommonTags.TrackNumber:
                        int track_number = (int)tag.Value;
                        track.TrackNumber = track_number == 0 ? track.TrackNumber : track_number;
                        break;
                    case CommonTags.TrackCount:
                        track.TrackCount = (int)tag.Value;
                        break;
                    case CommonTags.Duration:
                        if(tag.Value is TimeSpan) {
                            track.Duration = (TimeSpan)tag.Value;
                        } else {
                            track.Duration = new TimeSpan((uint)tag.Value * TimeSpan.TicksPerMillisecond);
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
