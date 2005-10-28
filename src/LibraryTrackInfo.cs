/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  LibraryTrackInfo.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Text.RegularExpressions;
using System.IO;
using System.Data;
using System.Collections;
using System.Threading;
using Entagged;

using Sql;

namespace Banshee
{
    public class LibraryTrackInfo : TrackInfo
    {
        public static int GetId(Uri lookup)
        {
            Statement query = new Select("Tracks", new List("TrackID")) +
                new Where(new Compare("Uri", Op.EqualTo, lookup));

            try {
                object result = Core.Library.Db.QuerySingle(query);
                int id = Convert.ToInt32(result);
                return id;
            } catch(Exception) {
                return 0;
            }
        }
        
        protected LibraryTrackInfo()
        {
            canSaveToDatabase = true;
        }
    
        private void CheckIfExists(Uri uri)
        {
              bool exists = false;
              try {
                exists = Core.Library.TracksFnKeyed[Library.MakeFilenameKey(uri)] != null;
              } catch(Exception) {
                exists = false;
              }
//              Console.WriteLine ("{0} {1}", uri.LocalPath, exists ? "exists" : "does not exist");
              if(exists) {
                  // TODO: we should actually probably take this as a hint to
                  // reparse metadata
                  throw new ApplicationException("Song is already in library");
              } 
        }

        private void CheckIfExists(string filename)
        {
            CheckIfExists (new Uri ("file://" + filename));
        }

        private string MoveToPlace(string old_filename, bool initial_import)
        {
            bool in_library = old_filename.StartsWith (Core.Library.Location);
//            Console.WriteLine ("\"{0}\" in \"{1}\": {2}", old_filename, Core.Library.Location, in_library);

            if (initial_import && !in_library) {
                bool copy = false;
                try {
                    copy = (bool)Core.GconfClient.Get(GConfKeys.CopyOnImport);
                } catch {}

                if (copy) {
                    string new_filename = FileNamePattern.BuildFull(this,
                        Path.GetExtension (old_filename).Substring(1));
                    CheckIfExists(new_filename);

                    try {

//                        Console.WriteLine ("!in_library: {0}", new_filename);

//                        Console.WriteLine ("if (File.Exists (\"{0}\"): {1}", new_filename, File.Exists (new_filename));
                        if (File.Exists (new_filename))
                            return null;
//                        Console.WriteLine ("File.Copy(\"{0}\", \"{1}\", false);", old_filename, new_filename);
                        File.Copy(old_filename, new_filename, false);
                        return new_filename;
                    } catch {
                        return null;
                    }
                }
            }

            if (in_library) {
                bool move = false;

                try {
                    move = (bool)Core.GconfClient.Get(GConfKeys.MoveOnInfoSave);
                } catch {}
    
                if (move) {
                    string new_filename = FileNamePattern.BuildFull(this,
                        Path.GetExtension (old_filename).Substring(1));
//                    Console.WriteLine ("in_library: {0}", new_filename);
                    CheckIfExists(new_filename);

                    try {
                        if (File.Exists (new_filename))
                            return null;

                        if (old_filename != new_filename) {
                            // Move and set uri.
                            File.Move (old_filename, new_filename);
    
                            // Delete old directories if empty.
                            try {
                                string old_dir = Path.GetDirectoryName (old_filename);
                                while (old_dir != null && old_dir != String.Empty) {
                                    Directory.Delete (old_dir);
                                    old_dir = Path.GetDirectoryName (old_dir);
                                }
                            } catch {}

                            return new_filename;
                        }
                    } catch {
                        return null;
                    }
                }
            }
            return null;
        }

        public LibraryTrackInfo(Uri uri, string artist, string album, 
           string title, string genre, uint trackNumber, uint trackCount,
           int year, long duration, string asin)
        {
            this.uri = uri;
            trackId = 0;
    
            mimetype = null;
            
            this.artist = artist;
            this.album = album;
            this.title = title;
            this.genre = genre;
            this.trackNumber = trackNumber;
            this.trackCount = trackCount;
            this.year = year;
            this.duration = duration;
            this.asin = asin;
            
            this.dateAdded = DateTime.Now;
            
            CheckIfExists(uri);
            
            SaveToDatabase(true);
            Core.Library.SetTrack(trackId, this);
            
            PreviousTrack = Gtk.TreeIter.Zero;
        }
        
        public LibraryTrackInfo(Uri uri, AudioCdTrackInfo track) : this(
            uri, track.Artist, track.Album, track.Title, track.Genre,
            track.TrackNumber, track.TrackCount, track.Year, track.Duration, track.Asin)
        {
            
        }
    
        public LibraryTrackInfo(string filename) : this()
        {
//            Console.WriteLine ("LibraryTrackInfo(\"{0}\");", filename);
            Uri old_uri = new Uri ("file://" + filename);

            CheckIfExists(old_uri);
            if(!LoadFromDatabase(old_uri)) {
//                Console.WriteLine ("LoadFromFile(\"{0}\");", filename);
                LoadFromFile(filename);

                string new_filename = MoveToPlace(filename, true);

                uri = new Uri ("file://" + (new_filename != null ? new_filename : filename));

                CheckIfExists(uri);

                SaveToDatabase(true);
            }

            Core.Library.SetTrack(trackId, this);

            PreviousTrack = Gtk.TreeIter.Zero;
        }
        
        public LibraryTrackInfo(IDataReader reader) : this()
        {
            LoadFromDatabaseReader(reader);
            Core.Library.SetTrack(trackId, this);
            PreviousTrack = Gtk.TreeIter.Zero;
        }
        
        private void ParseUri(string path)
        {
            artist = String.Empty;
            album = String.Empty;
            title = String.Empty;
            trackNumber = 0;
            Match match;

            string fileName = StringUtil.UriEscape(path);
            fileName = Path.GetFileNameWithoutExtension(fileName);
        
            match = Regex.Match(fileName, @"(\d+)\.(.*)$");
            if(match.Success) {
                trackNumber = Convert.ToUInt32(match.Groups[1].ToString());
//                Console.WriteLine ("trackNumber = {0}", trackNumber);
                fileName = match.Groups[2].ToString().Trim();
            }

            /* Artist - Album - Title */
            match = Regex.Match(fileName, @"\s*(.*)-\s*(.*)-\s*(.*)$");
            if(match.Success) {
                artist = match.Groups[1].ToString();
                album = match.Groups[2].ToString();
                title = match.Groups[3].ToString();
            } else {
                /* Artist - Title */
                match = Regex.Match(fileName, @"\s*(.*)-\s*(.*)$");
                if(match.Success) {
                    artist = match.Groups[1].ToString();
                    title = match.Groups[2].ToString();
                } else {
                    /* Title */
                    title = fileName;
                }
            }

            while (path != null && path != String.Empty) {
                path = Path.GetDirectoryName(path);
                fileName = Path.GetFileName (path);
                if (album == String.Empty) {
                    album = fileName;
                    continue;
                }
                if (artist == String.Empty) {
                    artist = fileName;
                    continue;
                }
                break;
            }
            
            artist = artist.Trim();
            album = album.Trim();
            title = title.Trim();
            
            if(artist.Length == 0)
                artist = /*"Unknown Artist"*/ null;
            if(album.Length == 0)
                album = /*"Unknown Album"*/ null;
            if(title.Length == 0)
                title = /*"Unknown Title"*/ null;
        }
        
        private void SaveToDatabase(bool retryIfFail)
        {
            Statement tracksQuery;

//            Console.WriteLine ("{0} has id {1}", uri.LocalPath, TrackId);

            if(trackId <= 0) {
                tracksQuery = new Insert("Tracks", true,
                    "TrackID", null, 
                    "Uri", uri, 
                    "MimeType", mimetype, 
                    "Artist", artist, 
                    "Performer", performer, 
                    "AlbumTitle", album,
                    "ASIN", asin,
                    "Label", label,
                    "Title", title, 
                    "Genre", genre, 
                    "Year", year,
                    "DateAddedStamp", DateTimeUtil.FromDateTime(dateAdded), 
                    "TrackNumber", trackNumber, 
                    "TrackCount", trackCount, 
                    "Duration", duration, 
                    "TrackGain", trackGain, 
                    "TrackPeak", trackPeak, 
                    "AlbumGain", albumGain, 
                    "AlbumPeak", albumPeak, 
                    "Rating", rating, 
                    "NumberOfPlays", numberOfPlays, 
                    "LastPlayedStamp", DateTimeUtil.FromDateTime(lastPlayed));
            } else {
                tracksQuery = new Update("Tracks",
                    "Uri", uri, 
                    "MimeType", mimetype, 
                    "Artist", artist, 
                    "Performer", performer, 
                    "AlbumTitle", album,
                    "ASIN", asin,
                    "Label", label,
                    "Title", title, 
                    "Genre", genre, 
                    "Year", year,
                    "DateAddedStamp", DateTimeUtil.FromDateTime(dateAdded), 
                    "TrackNumber", trackNumber, 
                    "TrackCount", trackCount, 
                    "Duration", duration, 
                    "TrackGain", trackGain, 
                    "TrackPeak", trackPeak, 
                    "AlbumGain", albumGain, 
                    "AlbumPeak", albumPeak, 
                    "Rating", rating, 
                    "NumberOfPlays", numberOfPlays, 
                    "LastPlayedStamp", DateTimeUtil.FromDateTime(lastPlayed)) +
                    new Where(new Compare("TrackID", Op.EqualTo, trackId));// +
                //    new Limit(1);
            }

            Core.Library.Db.Execute(tracksQuery);

            /*if(Core.Library.Db.Execute(query) <= 0 && retryIfFail) {
                trackId = 0;
                SaveToDatabase(false);
            } else if(trackId <= 0) {*/
            
            if(trackId <= 0)
               trackId = GetId(uri); /* OPTIMIZE! Seems like an unnecessary query */

//            Console.WriteLine ("{0} has id {1}", uri.LocalPath, TrackId);
        }
        
        private bool LoadFromDatabase(object id)
        {
            Statement query = 
                new Select("Tracks") +
                new Where(
                    new Compare("Uri", Op.EqualTo, id), Op.Or,
                    new Compare("TrackID", Op.EqualTo, id)) +
                new Limit(1);

            IDataReader reader = Core.Library.Db.Query(query);
            
            if(reader == null)
                return false;
            
            if(!reader.Read())
                return false;
                
            LoadFromDatabaseReader(reader);
            
            return true;
        }
        
        private void LoadFromDatabaseReader(IDataReader reader)
        {
            trackId = Convert.ToInt32(reader["TrackID"]);

            try {
                uri = new Uri(reader["Uri"] as string);
            } catch(UriFormatException e) {
                uri = new Uri("file://" + (reader["Uri"] as string));
            }
            
            mimetype = reader["MimeType"] as string;

            album = reader["AlbumTitle"] as string;
            artist = reader["Artist"] as string;
            performer = reader["Performer"] as string;
            title = reader["Title"] as string;
            genre = reader["Genre"] as string;

            year = Convert.ToInt32(reader["Year"]);

            trackNumber = Convert.ToUInt32(reader["TrackNumber"]);
            trackCount = Convert.ToUInt32(reader["TrackCount"]);
            duration = Convert.ToInt64(reader["Duration"]);
            rating = Convert.ToUInt32(reader["Rating"]);
            numberOfPlays = Convert.ToUInt32(reader["NumberOfPlays"]);

			if (reader != null){
				lastPlayed = DateTime.MinValue;

				try {
					string s = (string)reader ["LastPlayedStamp"];
					if (s != null){
						long time = Int64.Parse (s);
						lastPlayed = DateTimeUtil.ToDateTime(time);
					}
				} catch(Exception e) {
					Console.WriteLine ("E1: " + e);
				}
			}

			if (reader != null){
				dateAdded = DateTime.MinValue;
				
				try {
					string s = (string)reader ["LastPlayedStamp"];
					if (s != null){
						long time = Int64.Parse (s);
						dateAdded = DateTimeUtil.ToDateTime(time);
					}
				} catch(Exception e) {
					Console.WriteLine ("E2: " + e);
				}
            }
        }
        
        private void LoadFromFile(string filename)
        {
            ParseUri(filename);
            trackId = 0;
    
            AudioFileWrapper af = new AudioFileWrapper(filename);

            mimetype = null;

            artist = af.Artist == null ? artist : af.Artist;
            album = af.Album == null ? album : af.Album;
            title = af.Title == null ? title : af.Title;
            genre = af.Genre == null ? genre : af.Genre;
            trackNumber = af.TrackNumber == 0 ? trackNumber : (uint)af.TrackNumber;
            trackCount = 0;
            duration = af.Duration;
            year = af.Year;
            
            this.dateAdded = DateTime.Now;
        }

        public override void Save()
        {
            try {
                string new_filename = MoveToPlace (uri.LocalPath, false);
                if (new_filename != null) {
                    this.uri = new Uri ("file://" + new_filename);
                }
            } catch {}

            try {
                Core.Library.Db.WriteCycleFinished -= OnDbWriteCycleFinished;
                SaveToDatabase(true);
            } catch {
                Core.Library.Db.WriteCycleFinished += OnDbWriteCycleFinished;
            }
        }
        
        private void OnDbWriteCycleFinished(object o, EventArgs args)
        {
            Save();
        }
        
        public override void IncrementPlayCount()
        {
            numberOfPlays++;
            lastPlayed = DateTime.Now;
            
            /*Statement query = new Update("Tracks",
                "NumberOfPlays", numberOfPlays, 
                "LastPlayed", lastPlayed.ToString(ci.DateTimeFormat)) +
                new Where(new Compare("TrackID", Op.EqualTo, trackId));
                //new Limit(1);

            Core.Library.Db.Execute(query);*/
            
            Save();
        }
        
        protected override void SaveRating()
        {
            /*Statement query = new Update("Tracks",
                "Rating", rating) +
                new Where(new Compare("TrackID", Op.EqualTo, trackId));
            Core.Library.Db.Execute(query);*/
            Save();
        }
    }
}
