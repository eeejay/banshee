//
// FileTrackInfo.cs
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
using System.IO;
using System.Threading;
using System.Collections;
using System.Text.RegularExpressions;

using Banshee.Base;

namespace Banshee.Collection
{
    public class FileTrackInfo : TrackInfo
    {
        public FileTrackInfo (SafeUri uri)
        {
            LoadFromUri(uri);
            Uri = uri;
        }
		
        private void LoadFromUri (SafeUri uri)
        {
            ParsePath (uri.LocalPath);
   
            TagLib.File file = Banshee.IO.DemuxVfs.OpenFile (uri.LocalPath);
   
            ArtistName = Choose (file.Tag.JoinedAlbumArtists, ArtistName);
            AlbumTitle = Choose (file.Tag.Album, AlbumTitle);
            TrackTitle = Choose (file.Tag.Title, TrackTitle);
            Genre = Choose (file.Tag.FirstGenre, Genre);
            DiscNumber = file.Tag.Disc == 0 ? (int)DiscNumber : (int)file.Tag.Disc;
            TrackNumber = file.Tag.Track == 0 ? (int)TrackNumber : (int)file.Tag.Track;
            TrackCount = file.Tag.TrackCount == 0 ? (int)TrackCount : (int)file.Tag.TrackCount;
            Duration = file.Properties.Duration;
            Year = (int)file.Tag.Year;

            DateAdded = DateTime.Now;
        }

        private void ParsePath (string path)
        {
            ArtistName = String.Empty;
            AlbumTitle = String.Empty;
            TrackTitle = String.Empty;
            TrackNumber = 0;
            Match match;

            SafeUri uri = new SafeUri (path);
            string filename = path;
            if (uri.IsLocalPath) {
                filename = uri.AbsolutePath;
            }
            
            match = Regex.Match (filename, @"(\d+)\.? *(.*)$");
            if (match.Success) {
                TrackNumber = Convert.ToInt32 (match.Groups[1].ToString ());
                filename = match.Groups[2].ToString ().Trim ();
            }

            // Artist - Album - Title
            match = Regex.Match (filename, @"\s*(.*)-\s*(.*)-\s*(.*)$");
            if (match.Success) {
                ArtistName = match.Groups[1].ToString ();
                AlbumTitle = match.Groups[2].ToString ();
                TrackTitle = match.Groups[3].ToString ();
            } else {
                // Artist - Title
                match = Regex.Match (filename, @"\s*(.*)-\s*(.*)$");
                if (match.Success) {
                    ArtistName = match.Groups[1].ToString ();
                    TrackTitle = match.Groups[2].ToString ();
                } else {
                    // Title
                    TrackTitle = filename;
                }
            }

            while (!String.IsNullOrEmpty (path)) {
                filename = Path.GetFileName (path);
                path = Path.GetDirectoryName (path);
                if (AlbumTitle == String.Empty) {
                    AlbumTitle = filename;
                    continue;
                }
                
                if (ArtistName == String.Empty) {
                    ArtistName = filename;
                    continue;
                }
                
                break;
            }
            
            ArtistName = ArtistName.Trim ();
            AlbumTitle = AlbumTitle.Trim ();
            TrackTitle = TrackTitle.Trim ();
            
            if (ArtistName.Length == 0) {
                ArtistName = /*"Unknown Artist"*/ null;
            }
            
            if (AlbumTitle.Length == 0) {
                AlbumTitle = /*"Unknown Album"*/ null;
            }
            
            if (TrackTitle.Length == 0) {
                TrackTitle = /*"Unknown Title"*/ null;
            }
        }
 
		private static string Choose (string priority, string fallback)
		{
			return String.IsNullOrEmpty (priority) ? fallback : priority;
		}
    }
}
