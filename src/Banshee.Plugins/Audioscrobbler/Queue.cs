/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Queue.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Chris Toshok (toshok@ximian.com)
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
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Mono.Security.Cryptography;
using System.Collections;
using System.Web;
using System.Xml;

using GLib;
using Banshee.MediaEngine;
using Banshee.Base;
using Banshee;

namespace Banshee.Plugins.Audioscrobbler {
	internal class Queue {

		class QueuedTrack {
			public QueuedTrack (TrackInfo track, DateTime start_time)
			{
				this.artist = track.Artist;
				this.album = track.Album;
				this.title = track.Title;
				this.duration = (int)track.Duration.TotalSeconds;
				this.start_time = start_time.ToUniversalTime ();
			}

			public QueuedTrack (string artist, string album,
								string title, int duration, DateTime start_time)
			{
				this.artist = artist;
				this.album = album;
				this.title = title;
				this.duration = duration;
				this.start_time = start_time;
			}

			public DateTime StartTime {
				get { return start_time; }
			}
			public string Artist {
				get { return artist; }
			}
			public string Album {
				get { return album; }
			}
			public string Title {
				get { return title; }
			}
			public int Duration {
				get { return duration; }
			}

			string artist;
			string album;
			string title;
			int duration;
			DateTime start_time;
		}

		ArrayList queue;
		string xml_path;
		bool dirty;

		public Queue ()
		{
			EnsurePluginDirectoryExists();

			xml_path = System.IO.Path.Combine (Paths.UserPluginDirectory, "AudioscrobblerQueue.xml");
			queue = new ArrayList ();

			Load ();
		}

		public void Save ()
		{
			if (!dirty)
				return;

			XmlTextWriter writer = new XmlTextWriter (xml_path, Encoding.Default);

			writer.Formatting = Formatting.Indented;
			writer.Indentation = 4;
			writer.IndentChar = ' ';

			writer.WriteStartDocument (true);

			writer.WriteStartElement ("AudioscrobblerQueue");
			foreach (QueuedTrack track in queue) {
				writer.WriteStartElement ("QueuedTrack");	
				writer.WriteElementString ("Artist", track.Artist);
				writer.WriteElementString ("Album", track.Album);
				writer.WriteElementString ("Title", track.Title);
				writer.WriteElementString ("Duration", track.Duration.ToString());
				writer.WriteElementString ("StartTime", DateTimeUtil.ToTimeT(track.StartTime).ToString());
				writer.WriteEndElement (); // Track
			}
			writer.WriteEndElement (); // AudioscrobblerQueue
			writer.WriteEndDocument ();
			writer.Close ();
		}

		public void Load ()
		{
			queue.Clear ();

			try {
				string query = "//AudioscrobblerQueue/QueuedTrack";
				XmlDocument doc = new XmlDocument ();

				doc.Load (xml_path);
				XmlNodeList nodes = doc.SelectNodes (query);

				foreach (XmlNode node in nodes) {
					string artist = null;	
					string album = null;
					string title = null;
					int duration = 0;
					DateTime start_time = new DateTime (0);

					foreach (XmlNode child in node.ChildNodes) {
						if (child.Name == "Artist") {
							artist = child.ChildNodes [0].Value;
						} else if (child.Name == "Album") {
							album = child.ChildNodes [0].Value;
						} else if (child.Name == "Title") {
							title = child.ChildNodes [0].Value;
						} else if (child.Name == "Duration") {
							duration = Convert.ToInt32 (child.ChildNodes [0].Value);
						} else if (child.Name == "StartTime") {
							long time = Convert.ToInt64 (child.ChildNodes [0].Value);
							start_time = DateTimeUtil.FromTimeT (time);
						}
					}

                    queue.Add (new QueuedTrack (artist, album, title, duration, start_time));
				}
			} catch (System.Exception e) { }
		}

		public string GetTransmitInfo (out int num_tracks)
		{
			StringBuilder sb = new StringBuilder ();

			int i;
			for (i = 0; i < queue.Count; i ++) {
				/* we queue a maximum of 10 tracks per request */
				if (i == 9) break;

				QueuedTrack track = (QueuedTrack)queue[i];

				sb.AppendFormat (
						 "&a[{6}]={0}&t[{6}]={1}&b[{6}]={2}&m[{6}]={3}&l[{6}]={4}&i[{6}]={5}",
						 HttpUtility.UrlEncode (track.Artist),
						 HttpUtility.UrlEncode (track.Title),
						 HttpUtility.UrlEncode (track.Album),
						 "" /* musicbrainz id */,
						 track.Duration.ToString (),
						 HttpUtility.UrlEncode (track.StartTime.ToString ("yyyy-MM-dd HH:mm:ss")),
						 i);
			}

			num_tracks = i;
			return sb.ToString();
		}

		public void Add (TrackInfo track, DateTime started_at)
		{
			queue.Add (new QueuedTrack (track, started_at));
			dirty = true;
		}

		public void RemoveRange (int first, int count)
		{
			queue.RemoveRange (first, count);
			dirty = true;
		}

		public int Count {
			get { return queue.Count; }
		}

		private void EnsurePluginDirectoryExists ()
		{
			if (!Directory.Exists (Paths.UserPluginDirectory))
				Directory.CreateDirectory (Paths.UserPluginDirectory);
		}
	}

}
