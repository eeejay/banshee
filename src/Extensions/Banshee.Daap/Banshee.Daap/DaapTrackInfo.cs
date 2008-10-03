//
// DaapTrackInfo.cs
//
// Authors:
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2008 Alexander Hixon
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

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Daap;

namespace Banshee.Daap
{
    public class DaapTrackInfo : DatabaseTrackInfo
    {
        public DaapTrackInfo (Track track, DaapSource source) : base ()
        {
            TrackTitle = track.Title;
            AlbumTitle = track.Album;
            ArtistName = track.Artist;
            
            DateAdded = track.DateAdded;
            DateUpdated = track.DateModified;
            
            Genre = track.Genre;
            FileSize = track.Size;
            TrackCount = track.TrackCount;
            TrackNumber = track.TrackNumber;
            Year = track.Year;
            Duration = track.Duration;
            MimeType = track.Format;
            BitRate = (int)track.BitRate;
            ExternalId = track.Id;

            PrimarySource = source;
            
            Uri = new SafeUri (String.Format (
                "{0}{1}/{2}", DaapService.ProxyServer.HttpBaseAddress, source.Database.GetHashCode (), track.Id
            ));
            
            //this.IsLive = false;
            //this.CanSaveToDatabase = false;
        }
    }
}
