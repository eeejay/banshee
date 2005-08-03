/***************************************************************************
 *  TrackInfo.cs
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

namespace Sonance
{
	public class TrackInfo : ITrackInfo
	{
		private string uri;
	    private string mimetype;
	    private string artist;
	    private string album;
	    private string title;
	    private DateTime dateAdded;
	    private int year;
	    private string genre;
	    private string performer;
		
		private string asin;
		private string label;
	    
	    private uint rating;
	    private uint numberOfPlays;
	    private DateTime lastPlayed;
	    
	    private long duration;
	    private uint trackNumber;
	    private uint trackCount;
	   	private int uid;
	   	private int trackId;
	   	
	   	private double trackGain;
	   	private double trackPeak;
	   	private double albumGain;
	   	private double albumPeak;
	   	
	   	public Gtk.TreeIter PreviousTrack;
	
		public static int GetId(string name)
		{
			Statement query = new Select("Tracks", new List("TrackID")) +
				new Where(new Compare("Uri", Op.EqualTo, name), 
					Op.Or, new Compare("TrackID", Op.EqualTo, name));

			try {
				object result = Core.Library.Db.QuerySingle(query);
				int id = Convert.ToInt32(result);
				return id;
			} catch(Exception) {
				return 0;
			}
		}
	
		public TrackInfo(string uri)
		{
			if(uri.StartsWith("sql://")) {
				uri = uri.Substring(6);
				LoadFromDatabase(uri);
			} else {
				if(!LoadFromDatabase(uri))
					LoadFromFile(uri);
			}
			
			Core.Library.Tracks[trackId] = this;
			
			uid = Core.Instance.NextUid;
			PreviousTrack = Gtk.TreeIter.Zero;
		}
		
		public TrackInfo(IDataReader reader)
		{
			LoadFromDatabaseReader(reader);
			Core.Library.Tracks[trackId] = this;
			uid = Core.Instance.NextUid;
			PreviousTrack = Gtk.TreeIter.Zero;
		}
		
		private void ParseUri(string uri)
		{
			artist = "";
			album = "";
			title = "";
			trackNumber = 0;
			Match match;

			string fileName = StringUtil.UriEscape(uri);
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
					"DateAdded", dateAdded, 
					"TrackNumber", trackNumber, 
					"TrackCount", trackCount, 
					"Duration", duration, 
					"TrackGain", trackGain, 
					"TrackPeak", trackPeak, 
					"AlbumGain", albumGain, 
					"AlbumPeak", albumPeak, 
					"Rating", rating, 
					"NumberOfPlays", numberOfPlays, 
					"LastPlayed", lastPlayed);
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
					"DateAdded", dateAdded, 
					"TrackNumber", trackNumber, 
					"TrackCount", trackCount, 
					"Duration", duration, 
					"TrackGain", trackGain, 
					"TrackPeak", trackPeak, 
					"AlbumGain", albumGain, 
					"AlbumPeak", albumPeak, 
					"Rating", rating, 
					"NumberOfPlays", numberOfPlays, 
					"LastPlayed", lastPlayed) +
					new Where(new Compare("TrackID", Op.EqualTo, trackId));// +
				//	new Limit(1);
			}
			
			Core.Library.Db.Execute(tracksQuery);
			
			/*if(Core.Library.Db.Execute(query) <= 0 && retryIfFail) {
				trackId = 0;
				SaveToDatabase(false);
			} else if(trackId <= 0) {*/
			
			trackId = GetId(uri); /* OPTIMIZE! Seems like an unnecessary query */
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
			Hashtable colmap = Core.Library.Db.ColumnMap("Tracks");
			
			trackId = Convert.ToInt32(reader[(int)colmap["TrackID"]]);
				
			uri = (string)reader[(int)colmap["Uri"]];
			mimetype = (string)reader[(int)colmap["MimeType"]];
			
			album = (string)reader[(int)colmap["AlbumTitle"]];
			artist = (string)reader[(int)colmap["Artist"]];
			performer = (string)reader[(int)colmap["Performer"]];
			title = (string)reader[(int)colmap["Title"]];
			genre = (string)reader[(int)colmap["Genre"]];
			dateAdded = DateTime.Parse((string)reader[(int)colmap["DateAdded"]]);
			year = Convert.ToInt32(reader[(int)colmap["Year"]]);
			
			trackNumber = Convert.ToUInt32(reader[(int)colmap["TrackNumber"]]);
			trackCount = Convert.ToUInt32(reader[(int)colmap["TrackCount"]]);
			duration = Convert.ToInt64(reader[(int)colmap["Duration"]]);
			rating = Convert.ToUInt32(reader[(int)colmap["Rating"]]);
			numberOfPlays = 
				Convert.ToUInt32(reader[(int)colmap["NumberOfPlays"]]);
			lastPlayed = 
				DateTime.Parse((string)reader[(int)colmap["LastPlayed"]]);
				
			/*if(reader == null)
				return;
				
			foreach(string key in reader.GetSchemaTable().Columns) 
				Console.WriteLine(key);*/
				
			/*trackId = Convert.ToInt32(reader["TrackID"]);
				
			uri = (string)reader["Uri"];
			mimetype = (string)reader["MimeType"];
			
			artist = (string)reader["Artist"];
			performer = (string)reader["Performer"];
			title = (string)reader["Title"];
			genre = (string)reader["Genre"];
			date = DateTime.Parse((string)reader["Date"]);
			
			trackNumber = Convert.ToUInt32(reader["TrackNumber"]);
			trackCount = Convert.ToUInt32(reader["TrackCount"]);
			duration = Convert.ToInt64(reader["Duration"]);
			rating = Convert.ToUInt32(reader["Rating"]);
			numberOfPlays = 
				Convert.ToUInt32(reader["NumberOfPlays"]);
			lastPlayed = 
				DateTime.Parse((string)reader["LastPlayed"]);*/
		}
		
		private void LoadFromFile(string uri)
		{
			this.uri = uri;
			ParseUri(uri);
			trackId = 0;
	
			AudioFileWrapper af = new AudioFileWrapper(uri);

			mimetype = null;

			artist = af.Artist == null ? artist : af.Artist;
			album = af.Album == null ? album : af.Album;
			title = af.Title == null ? title : af.Title;
			genre = af.Genre == null ? genre : af.Genre;
			trackNumber = (uint)af.TrackNumber;
			trackCount = 0;
			duration = af.Duration;
			year = af.Year;
			
			SaveToDatabase(true);
		}
		
		public void Save()
		{
			SaveToDatabase(true);
		}
		
		public void IncrementPlayCount()
		{
			numberOfPlays++;
			lastPlayed = DateTime.Now;
			
			Statement query = new Update("Tracks",
				"NumberOfPlays", numberOfPlays, 
				"LastPlayed", lastPlayed) +
				new Where(new Compare("TrackID", Op.EqualTo, trackId));
				//new Limit(1);

			Core.Library.Db.Execute(query);
		}
		
		private void SaveRating()
		{
			Statement query = new Update("Tracks",
				"Rating", rating) +
				new Where(new Compare("TrackID", Op.EqualTo, trackId));
			Core.Library.Db.Execute(query);
		}
		
		public int TrackId      { get { return trackId;     } }
		
		public string Uri       { get { return uri;         } } 
	   	public string MimeType  { get { return mimetype;    } }
	    public string Artist    { get { return artist;      } }
	    public string Album     { get { return album;       } }
	    public string Title     { get { return title;       } }
	    public string Genre     { get { return genre;       } }
	    public string Performer { get { return performer;   } }
	    public int Year         { get { return year;        } }	    
	    
	    public long Duration    { get { return duration;    } }
	   	public uint TrackNumber { get { return trackNumber; } }
	    public uint TrackCount  { get { return trackCount;  } }
	    
	    public uint Rating         { get { return rating;        } 
	    	                         set { rating = value; 
	    	                               SaveRating();         } }
	    public uint NumberOfPlays  { get { return numberOfPlays; } }
	    public DateTime LastPlayed { get { return lastPlayed;    } }
	    public DateTime DateAdded  { get { return dateAdded;     } }
	    
	    public double TrackGain { get { return trackGain;   } }
	    public double TrackPeak { get { return trackPeak;   } }
	    public double AlbumGain { get { return albumGain;   } }
	    public double AlbumPeak { get { return albumPeak;   } }
	    
	    public string DisplayArtist { get { return artist == null 
	    	? "Unknown Artist" : artist; } }
	    public string DisplayAlbum { get { return album == null 
	    	? "Unknown Album" : album; } }
	    public string DisplayTitle { get { return title == null 
	    	? "Unknown Title" : title; } }
	    	
	    public int Uid          { get { return uid;         } }
	}
}
