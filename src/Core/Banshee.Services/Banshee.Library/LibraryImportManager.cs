// 
// LibraryImportManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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

using Hyena;
using Hyena.Data.Sqlite;

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
                DatabaseTrackInfo track = AddTrackToLibrary (path);
                if (track != null && track.DbId > 0) {
                    IncrementProcessedCount (String.Format ("{0} - {1}", 
                        track.DisplayArtistName, track.DisplayTrackTitle));
                }
            } catch (Exception e) {
                LogError (path, e);
                IncrementProcessedCount (null);
            }
        }
        
        public DatabaseTrackInfo AddTrackToLibrary (string path)
        {
            return AddTrackToLibrary (new SafeUri (path));
        }
        
        private int count = 0;
        public DatabaseTrackInfo AddTrackToLibrary (SafeUri uri)
        {
            DatabaseTrackInfo track = null;
            
            if (DatabaseTrackInfo.ContainsUri (uri)) {
                IncrementProcessedCount (null);
                return null;
            }

            ServiceManager.DbConnection.BeginTransaction ();
            try {
                TagLib.File file = StreamTagger.ProcessUri (uri);
                track = new DatabaseTrackInfo ();
                StreamTagger.TrackInfoMerge (track, file);
                
                SafeUri newpath = track.CopyToLibrary ();
                if (newpath != null) {
                    track.Uri = newpath;
                }

                LibraryArtistInfo artist = LibraryArtistInfo.FindOrCreate (track.ArtistName);
                LibraryAlbumInfo album = LibraryAlbumInfo.FindOrCreate (artist, track.AlbumTitle);

                track.DateAdded = DateTime.Now;
                track.Source = ServiceManager.SourceManager.Library;
                track.ArtistId = artist.DbId;
                track.AlbumId = album.DbId;
                track.Save (false);

                ServiceManager.DbConnection.CommitTransaction ();
            } catch (Exception) {
                ServiceManager.DbConnection.RollbackTransaction ();
                throw;
            }

            if (count++ % 500 == 0) {
                ServiceManager.SourceManager.Library.NotifyTracksAdded ();
            }
            
            return track;
        }

        private void LogError (string path, Exception e)
        {
            LogError (path, e.Message);

            if (!(e is TagLib.CorruptFileException) && !(e is TagLib.UnsupportedFormatException)) {
                Log.DebugFormat ("Full import exception: {0}", e.ToString ());
            }
        }

        private void LogError (string path, string msg)
        {
            ErrorSource error_source = ServiceManager.SourceManager.Library.ErrorSource;
            error_source.AddMessage (Path.GetFileName (path), msg);
            
            Log.Error (path, msg, false);
        }

        protected override void OnImportFinished ()
        {
            count = 0;
            ServiceManager.SourceManager.Library.NotifyTracksAdded ();
            base.OnImportFinished ();
        }
        
        string IService.ServiceName {
            get { return "LibraryImportManager"; }
        }
    }
}
