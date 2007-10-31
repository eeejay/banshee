/***************************************************************************
 *  StreamTagger.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using Mono.Unix;

using Banshee.Configuration.Schema;
using Banshee.Collection;

namespace Banshee.Base
{
    public struct StreamTag
    {
        public static readonly StreamTag Zero;
  
        public string Name;
        public object Value;
        
        public override string ToString()
        {
            return String.Format("{0} = {1}", Name, Value);
        }
    }
        
    public static class StreamTagger
    {
        public static TagLib.File ProcessUri(SafeUri uri)
        {
            string mimetype = null;
            bool process = true;
            
            try {
                mimetype = Banshee.IO.IOProxy.DetectMimeType(uri);
            } catch {
            }

            TagLib.File file = Banshee.IO.IOProxy.OpenFile(uri.IsLocalPath ? uri.LocalPath : uri.AbsoluteUri, mimetype, TagLib.ReadStyle.Average);

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
            track.ArtistName = Choose(file.Tag.JoinedArtists, track.ArtistName);
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
                    case CommonTags.AlbumCoverId:
                        foreach(string ext in TrackInfo.CoverExtensions) {
                            string path = Paths.GetCoverArtPath((string) tag.Value, "." + ext);
                            if(System.IO.File.Exists(path)) {
                                track.CoverArtFileName = path;
                                break;
                            }
                        }
                        break;
                }
            } catch {
            }
        }
    }
    
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
    
    public sealed class CommonTags 
    {
        public const string Title             = "title";
        public const string Artist            = "artist";
        public const string Album             = "album";
        public const string Date              = "date";
        public const string Genre             = "genre";
        public const string Comment           = "comment";
        public const string TrackNumber       = "track-number";
        public const string TrackCount        = "track-count";
        public const string AlbumVolumeNumber = "album-disc-number";
        public const string AlbumVolumeCount  = "album-disc-count";
        public const string Location          = "location";
        public const string Description       = "description";
        public const string Version           = "version";
        public const string Isrc              = "isrc";
        public const string Organization      = "organization";
        public const string Copyright         = "copyright";
        public const string Contact           = "contact";
        public const string License           = "license";
        public const string Performer         = "performer";
        public const string Duration          = "duration";
        public const string Codec             = "codec";
        public const string VideoCodec        = "video-codec";
        public const string AudioCodec        = "audio-codec";
        public const string Bitrate           = "bitrate";
        public const string NominalBitrate    = "nominal-bitrate";
        public const string MinimumBitrate    = "minimum-bitrate";
        public const string MaximumBitrate    = "maximum-bitrate";
        public const string Serial            = "serial";
        public const string Encoder           = "encoder";
        public const string EncoderVersion    = "encoder-version";
        public const string TrackGain         = "replaygain-track-gain";
        public const string TrackPeak         = "replaygain-track-peak";
        public const string AlbumGain         = "replaygain-album-gain";
        public const string AlbumPeak         = "replaygain-album-peak";
        public const string StreamType        = "stream-type";
        public const string AlbumCoverId      = "album-cover-id"; 
        public const string MoreInfoUri       = "more-info-uri";
    }
}
