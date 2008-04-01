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
using DAAP;

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
            
            PrimarySource = source;
            
            string sessionid = "";
            if (source.Database.Client.Fetcher.SessionId != 0) {
                sessionid = String.Format ("?session-id={0}", source.Database.Client.Fetcher.SessionId);
            }
            string uri = String.Format ("http://{0}:{1}/databases/{2}/items/{3}.{4}{5}",
                                        source.Database.Client.Address.ToString (),
                                        source.Database.Client.Port,
                                        source.Database.Id,
                                        track.Id,
                                        track.Format,
                                        sessionid);
            
            Uri = new SafeUri (uri);
            
            //this.IsLive = true;
            this.CanSaveToDatabase = false;
        }
    }
}
