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
        public FileTrackInfo(SafeUri uri)
        {
            LoadFromUri(uri);
            Uri = uri;
        }
		
        private void LoadFromUri(SafeUri uri)
        {
            ParsePath(uri.LocalPath);
   
            TagLib.File file = Banshee.IO.IOProxy.OpenFile(uri.LocalPath);
   
            ArtistName = Choose(file.Tag.JoinedAlbumArtists, ArtistName);
            AlbumTitle = Choose(file.Tag.Album, AlbumTitle);
            TrackTitle = Choose(file.Tag.Title, TrackTitle);
            Genre = Choose(file.Tag.FirstGenre, Genre);
            TrackNumber = file.Tag.Track == 0 ? (int)TrackNumber : (int)file.Tag.Track;
            TrackCount = file.Tag.TrackCount == 0 ? (int)TrackCount : (int)file.Tag.TrackCount;
            Duration = file.Properties.Duration;
            Year = (int)file.Tag.Year;

            DateAdded = DateTime.Now;
        }

        private void ParsePath(string path)
        {
            ArtistName = String.Empty;
            AlbumTitle = String.Empty;
            TrackTitle = String.Empty;
            TrackNumber = 0;
            Match match;

            SafeUri uri = new SafeUri(path);
            string fileName = path;
            if(uri.IsLocalPath) {
                fileName = uri.AbsolutePath;
            }
            
            match = Regex.Match(fileName, @"(\d+)\.? *(.*)$");
            if(match.Success) {
                TrackNumber = Convert.ToInt32(match.Groups[1].ToString());
                fileName = match.Groups[2].ToString().Trim();
            }

            /* Artist - Album - Title */
            match = Regex.Match(fileName, @"\s*(.*)-\s*(.*)-\s*(.*)$");
            if(match.Success) {
                ArtistName = match.Groups[1].ToString();
                AlbumTitle = match.Groups[2].ToString();
                TrackTitle = match.Groups[3].ToString();
            } else {
                /* Artist - Title */
                match = Regex.Match(fileName, @"\s*(.*)-\s*(.*)$");
                if(match.Success) {
                    ArtistName = match.Groups[1].ToString();
                    TrackTitle = match.Groups[2].ToString();
                } else {
                    /* Title */
                    TrackTitle = fileName;
                }
            }

            while(!String.IsNullOrEmpty(path)) {
                fileName = Path.GetFileName(path);
                path = Path.GetDirectoryName(path);
                if(AlbumTitle == String.Empty) {
                    AlbumTitle = fileName;
                    continue;
                }
                
                if(ArtistName == String.Empty) {
                    ArtistName = fileName;
                    continue;
                }
                break;
            }
            
            ArtistName = ArtistName.Trim();
            AlbumTitle = AlbumTitle.Trim();
            TrackTitle = TrackTitle.Trim();
            
            if(ArtistName.Length == 0) {
                ArtistName = /*"Unknown Artist"*/ null;
            }
            
            if(AlbumTitle.Length == 0) {
                AlbumTitle = /*"Unknown Album"*/ null;
            }
            
            if(TrackTitle.Length == 0) {
                TrackTitle = /*"Unknown Title"*/ null;
            }
        }
 
		private static string Choose(string priority, string fallback)
		{
			return (priority == null || priority.Length == 0) ? fallback : priority;
		}
    }
}
