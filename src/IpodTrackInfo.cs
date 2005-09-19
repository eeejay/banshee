/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  IpodTrackInfo.cs
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
using IPod;

namespace Banshee
{
	public class IpodTrackInfo : TrackInfo
	{
		private Song song;
		private bool needSync;
		private LibraryTrackInfo libTrack = null;
	
		public IpodTrackInfo(Song song)
		{
			this.song = song;
			Load();
			uid = UidGenerator.Next;
			PreviousTrack = Gtk.TreeIter.Zero;
			canSaveToDatabase = false;
			needSync = false;
		}
		
		public IpodTrackInfo(Device device, LibraryTrackInfo lti)
		{
			song = IpodMisc.TrackInfoToSong(device, lti);
			Load();
			PreviousTrack = Gtk.TreeIter.Zero;
			canSaveToDatabase = false;
			needSync = true;
			libTrack = lti;
			uri = libTrack.Uri;
		}
		
		private void Load()
		{
			uri = new Uri("file://" + song.Filename);
			album = song.Album == String.Empty ? null : song.Album;
			artist = song.Artist == String.Empty ? null : song.Artist;
			title = song.Title == String.Empty ? null : song.Title;
			genre = song.Genre == String.Empty ? null : song.Genre;
			
			trackId = song.Id;
			duration = song.Length / 1000;
			numberOfPlays = (uint)song.PlayCount;

			switch(song.Rating) {
				case SongRating.One:   rating = 1; break;
				case SongRating.Two:   rating = 2; break;
				case SongRating.Three: rating = 3; break;
				case SongRating.Four:  rating = 4; break;
				case SongRating.Five:  rating = 5; break;
				case SongRating.Zero: 
				default: 
					rating = 0; 
					break;
			}
			
			lastPlayed = song.LastPlayed;
			dateAdded = song.DateAdded;
			trackCount = (uint)song.TotalTracks;
			trackNumber = (uint)song.TrackNumber;
			year = song.Year;
			canPlay = !song.IsProtected;
		}
		
		public override void Save()
		{
			
		}
		
		public override void IncrementPlayCount()
		{
			numberOfPlays++;
			Save();
		}
		
		protected override void SaveRating()
		{
			Save();
		}
		
		public bool NeedSync
		{
			get {
				return libTrack != null;
			}
		}
		
		public LibraryTrackInfo LibraryTrack
		{
			get {
				return libTrack;
			}
		}
		
		public IPod.Song Song
		{
			get {
				return song;
			}
		}
	}
}
