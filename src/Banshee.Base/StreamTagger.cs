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
        public static void TrackInfoMerge(TrackInfo track, StreamTag tag)
        {
            try {
                switch(tag.Name) {
                    case CommonTags.Artist:
                        track.Artist = (string)tag.Value;
                        break;
                    case CommonTags.Title:
                        track.Title = (string)tag.Value;
                        break;
                    case CommonTags.Album:
                        track.Album = (string)tag.Value;
                        break;
                    case CommonTags.Genre:
                        track.Genre = (string)tag.Value;
                        break;
                    case CommonTags.TrackNumber:
                        track.TrackNumber = (uint)tag.Value;
                        break;
                    case CommonTags.TrackCount:
                        track.TrackCount = (uint)tag.Value;
                        break;
                }
            } catch {
            }
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
    }
}
