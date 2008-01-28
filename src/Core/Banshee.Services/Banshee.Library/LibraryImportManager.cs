// 
// LibraryImportManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Streaming;

namespace Banshee.Library
{
    public class LibraryImportManager : ImportManager, IService
    {
        // This is a list of known media files that we may encounter. The extensions
        // in this list do not mean they are actually supported - this list is just
        // used to see if we should allow the file to be processed by TagLib. The
        // point is to rule out, at the path level, files that we won't support.
        
        private static string [] white_list_file_extensions = new string [] {
            "3g2",   "3gp",  "3gp2", "3gpp", "aac",  "ac3",  "aif",  "aifc", 
            "aiff",  "al",   "alaw", "ape",  "asf",  "asx",  "au",   "avi", 
            "cda",   "cdr",  "divx", "dv",   "flac", "flv",  "gvi",  "gvp", 
            "m1v",   "m21",  "m2p",  "m2v",  "m4a",  "m4b",  "m4e",  "m4p",  
            "m4u",   "m4v",  "mp+",  "mid",  "midi", "mjp",  "moov", "mov",  
            "movie", "mp1",  "mp2",  "mp21", "mp3",  "mp4",  "mpa",  "mpc",  
            "mpe",   "mpeg", "mpg",  "mpp",  "mpu",  "mpv",  "mpv2", "ogg",  
            "ogm",   "omf",  "qt",   "ra",   "ram",  "raw",  "rm",   "rmvb", 
            "rts",   "smil", "swf",  "tivo", "u",    "vfw",  "vob",  "wav",  
            "wave",  "wax",  "wm",   "wma",  "wmd",  "wmv",  "wmx",  "wv",   
            "wvc",   "wvx",  "yuv"
        };

        static LibraryImportManager ()
        {
            Array.Sort<string> (white_list_file_extensions);
        }
        
        public static bool IsWhiteListedFile (string path)
        {
            if (String.IsNullOrEmpty (path)) {
                return false;
            }
            
            int index = path.LastIndexOf ('.');
            if (index < 0 || index == path.Length || index == path.Length - 1) {
                return false;
            }
            
            return Array.BinarySearch<string> (white_list_file_extensions, 
                path.Substring (index + 1).ToLower ()) >= 0;
        }
    
        public LibraryImportManager ()
        {
        }
        
        protected override void OnImportRequested (string path)
        {
            if (!IsWhiteListedFile (path)) {
                IncrementProcessedCount (null);
                return;
            }
            
            try {
                SafeUri uri = new SafeUri (path);

                LibraryTrackInfo track = null;
                
                /*if (LibraryTrackInfo.ContainsUri (uri)) {
                    IncrementProcessedCount (null);
                    return;
                }*/

                TagLib.File file = StreamTagger.ProcessUri (uri);
                track = new LibraryTrackInfo ();
                StreamTagger.TrackInfoMerge (track, file);

                ThreadAssist.ProxyToMain (delegate {
                    track.DateAdded = DateTime.Now;
                    LibraryArtistInfo artist = new LibraryArtistInfo (track.ArtistName);
                    track.ArtistId = artist.DbId;
                    track.AlbumId = new LibraryAlbumInfo (artist, track.AlbumTitle).DbId;

                    artist.Save ();
                    track.Save ();
                    (ServiceManager.SourceManager.DefaultSource as LibrarySource).Reload ();

                });
                
                if (track != null && track.DbId > 0) {
                    IncrementProcessedCount (String.Format ("{0} - {1}", track.DisplayArtistName, track.DisplayTrackTitle));
                }
            } catch (Exception e) {
                LogError (path, e);
                IncrementProcessedCount (null);
            }
        }

        private void LogError (string path, Exception e)
        {
            LogError (path, e.Message);
        }

        private void LogError (string path, string msg)
        {
            ErrorSource error_source = ((LibrarySource)ServiceManager.SourceManager.DefaultSource).ErrorSource;
            error_source.AddMessage (Path.GetFileName (path), msg);
            
            Log.Error (path, msg, false);
        }

        protected override void OnImportFinished ()
        {
            base.OnImportFinished ();
        }
        
        string IService.ServiceName {
            get { return "LibraryImportManager"; }
        }
    }
}
