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
		public static int GetId(string lookup)
		{
			Statement query = new Select("Tracks", new List("TrackID")) +
				new Where(new Compare("Uri", Op.EqualTo, lookup), 
					Op.Or, new Compare("TrackID", Op.EqualTo, lookup));

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
              
              if(exists) {
                  // TODO: we should actually probably take this as a hint to
                  // reparse metadata
                  throw new ApplicationException("Song is already in library");
              } 
        }
	
	    public LibraryTrackInfo(Uri uri, string artist, string album, 
	       string title, string genre, uint trackNumber, uint trackCount,
	       int year, long duration)
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
			
			this.dateAdded = DateTime.Now;
			
			CheckIfExists(uri);
			
			SaveToDatabase(true);
			Core.Library.SetTrack(trackId, this);
			
			PreviousTrack = Gtk.TreeIter.Zero;
	    }
	    
        public LibraryTrackInfo(Uri uri, AudioCdTrackInfo track) : this(
            uri, track.Artist, track.Album, track.Title, track.Genre,
            track.TrackNumber, track.TrackCount, track.Year, track.Duration)
        {
        
        }
	
		public LibraryTrackInfo(Uri uri) : this()
		{
			if(uri.Scheme == "sql") {
			    throw new ApplicationException("SQL URI DEPRECATED");
				//LoadFromDatabase(uri.AbsolutePath);
			} else {
                CheckIfExists(uri);
                
            		if(!LoadFromDatabase(uri))
					LoadFromFile(uri);
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
			artist = "";
			album = "";
			title = "";
			trackNumber = 0;
			Match match;

			string fileName = StringUtil.UriEscape(path);
			fileName = Path.GetFileNameWithoutExtension(fileName);
		
			match = Regex.Match(fileName, @"(\d+)\.(.*)$");
			if(match.Success) {
				trackNumber = Convert.ToUInt32(match.Groups[1].ToString());
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
					"DateAddedStamp", Mono.Unix.UnixConvert.FromDateTime(dateAdded), 
					"TrackNumber", trackNumber, 
					"TrackCount", trackCount, 
					"Duration", duration, 
					"TrackGain", trackGain, 
					"TrackPeak", trackPeak, 
					"AlbumGain", albumGain, 
					"AlbumPeak", albumPeak, 
					"Rating", rating, 
					"NumberOfPlays", numberOfPlays, 
					"LastPlayedStamp", Mono.Unix.UnixConvert.FromDateTime(lastPlayed));
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
					"DateAddedStamp", Mono.Unix.UnixConvert.FromDateTime(dateAdded), 
					"TrackNumber", trackNumber, 
					"TrackCount", trackCount, 
					"Duration", duration, 
					"TrackGain", trackGain, 
					"TrackPeak", trackPeak, 
					"AlbumGain", albumGain, 
					"AlbumPeak", albumPeak, 
					"Rating", rating, 
					"NumberOfPlays", numberOfPlays, 
					"LastPlayedStamp", Mono.Unix.UnixConvert.FromDateTime(lastPlayed)) +
					new Where(new Compare("TrackID", Op.EqualTo, trackId));// +
				//	new Limit(1);
			}
			
			Core.Library.Db.Execute(tracksQuery);
			
			/*if(Core.Library.Db.Execute(query) <= 0 && retryIfFail) {
				trackId = 0;
				SaveToDatabase(false);
			} else if(trackId <= 0) {*/
			
			if(trackId <= 0)
			   trackId = GetId(uri.AbsoluteUri); /* OPTIMIZE! Seems like an unnecessary query */
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
            } catch(UriFormatException) {
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

            try {
                lastPlayed = Mono.Unix.UnixConvert.ToDateTime((long)reader["LastPlayedStamp"]);
            } catch(Exception) {
                lastPlayed = DateTime.MinValue;
            }

            try {
                dateAdded = Mono.Unix.UnixConvert.ToDateTime((long)reader["DateAddedStamp"]);
            } catch(Exception) {
                dateAdded = DateTime.MinValue;
            }
		}
		
		private void LoadFromFile(Uri uri)
		{
			this.uri = uri;
			ParseUri(uri.AbsolutePath);
			trackId = 0;
	
			AudioFileWrapper af = new AudioFileWrapper(uri.AbsolutePath);

			mimetype = null;

			artist = af.Artist == null ? artist : af.Artist;
			album = af.Album == null ? album : af.Album;
			title = af.Title == null ? title : af.Title;
			genre = af.Genre == null ? genre : af.Genre;
			trackNumber = (uint)af.TrackNumber;
			trackCount = 0;
			duration = af.Duration;
			year = af.Year;
			
			this.dateAdded = DateTime.Now;
			
			SaveToDatabase(true);
		}
		
		public override void Save()
		{
			try {
			     Core.Library.Db.WriteCycleFinished -= OnDbWriteCycleFinished;
			   SaveToDatabase(true);
			} catch(Exception) {
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
