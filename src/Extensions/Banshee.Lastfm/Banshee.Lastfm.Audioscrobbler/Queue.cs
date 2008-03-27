//
// Queue.cs
//
// Author:
//   Chris Toshok <toshok@ximian.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Text;
using System.Security.Cryptography;
using Mono.Security.Cryptography;
using System.Collections.Generic;
using System.Web;
using System.Xml;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Sources;

using Lastfm;
using Banshee.Lastfm.Radio;

namespace Banshee.Lastfm.Audioscrobbler
{
    class Queue : IQueue
    {
        internal class QueuedTrack
        {
            public QueuedTrack (TrackInfo track, DateTime start_time)
            {
                this.artist = track.ArtistName;
                this.album = track.AlbumTitle;
                this.title = track.TrackTitle;
                this.track_number = (int) track.TrackNumber;
                this.duration = (int) track.Duration.TotalSeconds;
                this.start_time = DateTimeUtil.ToTimeT(start_time.ToLocalTime ());
                // TODO
                //this.musicbrainzid = track.MusicBrainzId;
                
                this.musicbrainzid = "";
                
                // set trackauth value, otherwise empty string is default
                if (track is LastfmTrackInfo) {
                    this.track_auth = (track as LastfmTrackInfo).TrackAuth;
                }
            }

            public QueuedTrack (string artist, string album,
                                string title, int track_number, int duration, long start_time,
                                string musicbrainzid, string track_auth)
            {
                this.artist = artist;
                this.album = album;
                this.title = title;
                this.track_number = track_number;
                this.duration = duration;
                this.start_time = start_time;
                this.musicbrainzid = musicbrainzid;
                this.track_auth = track_auth;
            }

            public long StartTime {
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
            
            public int TrackNumber {
                get { return track_number; }
            }
            
            public int Duration {
                get { return duration; }
            }
            
            public string MusicBrainzId {
                get { return musicbrainzid; }
            }
            
            public string TrackAuth {
                get { return track_auth; }
            }

            string artist;
            string album;
            string title;
            int track_number;
            int duration;
            string musicbrainzid;
            long start_time;
            string track_auth = String.Empty;
        }

        List<QueuedTrack> queue;
        string xml_path;
        bool dirty;

        public event EventHandler TrackAdded;

        public Queue ()
        {
            string xmlfilepath = Path.Combine (Banshee.Base.Paths.ExtensionCacheRoot, "last.fm");
            xml_path = Path.Combine (xmlfilepath, "audioscrobbler-queue.xml");
            queue = new List<QueuedTrack> ();
            
            if (!Directory.Exists(xmlfilepath)) {
                Directory.CreateDirectory (xmlfilepath);
            }

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
                writer.WriteElementString ("TrackNumber", track.TrackNumber.ToString());
                writer.WriteElementString ("Duration", track.Duration.ToString());
                writer.WriteElementString ("StartTime", track.StartTime.ToString());
                writer.WriteElementString ("MusicBrainzId", track.MusicBrainzId);
                writer.WriteElementString ("TrackAuth", track.TrackAuth);
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
                    string artist = "";    
                    string album = "";
                    string title = "";
                    int track_number = 0;
                    int duration = 0;
                    long start_time = 0;
                    string musicbrainzid = "";
                    string track_auth = "";

                    foreach (XmlNode child in node.ChildNodes) {
                        if (child.Name == "Artist" && child.ChildNodes.Count != 0) {
                            artist = child.ChildNodes [0].Value;
                        } else if (child.Name == "Album" && child.ChildNodes.Count != 0) {
                            album = child.ChildNodes [0].Value;
                        } else if (child.Name == "Title" && child.ChildNodes.Count != 0) {
                            title = child.ChildNodes [0].Value;
                        } else if (child.Name == "TrackNumber" && child.ChildNodes.Count != 0) {
                            track_number = Convert.ToInt32 (child.ChildNodes [0].Value);
                        } else if (child.Name == "Duration" && child.ChildNodes.Count != 0) {
                            duration = Convert.ToInt32 (child.ChildNodes [0].Value);
                        } else if (child.Name == "StartTime" && child.ChildNodes.Count != 0) {
                            start_time = Convert.ToInt64 (child.ChildNodes [0].Value);
                        } else if (child.Name == "MusicBrainzId" && child.ChildNodes.Count != 0) {
                            musicbrainzid = child.ChildNodes [0].Value;
                        } else if (child.Name == "TrackAuth" && child.ChildNodes.Count != 0) {
                            track_auth = child.ChildNodes [0].Value;
                        }
                    }

                    queue.Add (new QueuedTrack (artist, album, title, track_number, duration,
                        start_time, musicbrainzid, track_auth));
                }
            } catch { 
            }
        }

        public string GetTransmitInfo (out int numtracks)
        {
            StringBuilder sb = new StringBuilder ();

            int i;
            for (i = 0; i < queue.Count; i ++) {
                /* Last.fm 1.2 can handle up to 50 songs in one request */
                if (i == 49) break;

                QueuedTrack track = (QueuedTrack) queue[i];
                
                string str_track_number = String.Empty;
                if (track.TrackNumber != 0)
                    str_track_number = track.TrackNumber.ToString();
                 
                string source = "P"; /* chosen by user */   
                if (track.TrackAuth.Length != 0) {
                    // from last.fm 
                    source = "L" + track.TrackAuth;
                }

                sb.AppendFormat (
                    "&a[{9}]={0}&t[{9}]={1}&i[{9}]={2}&o[{9}]={3}&r[{9}]={4}&l[{9}]={5}&b[{9}]={6}&n[{9}]={7}&m[{9}]={8}",
                    HttpUtility.UrlEncode (track.Artist),
                    HttpUtility.UrlEncode (track.Title),
                    track.StartTime.ToString (),
                    source, 
                    ""  /* rating: L/B/S */, 
                    track.Duration.ToString (),
                    HttpUtility.UrlEncode (track.Album),
                    str_track_number,
                    track.MusicBrainzId,
                    
                    i);
            }

            numtracks = i;
            return sb.ToString ();
        }

        public void Add (object track, DateTime started_at)
        {
            TrackInfo t = (track as TrackInfo);
            if (t != null) {
                queue.Add (new QueuedTrack (t, started_at));
                dirty = true;
                RaiseTrackAdded (this, new EventArgs ());
            }
        }

        public void RemoveRange (int first, int count)
        {
            queue.RemoveRange (first, count);
            dirty = true;
        }

        public int Count {
            get { return queue.Count; }
        }

        private void RaiseTrackAdded (object o, EventArgs args)
        {
            EventHandler handler = TrackAdded;
            if (handler != null)
                handler (o, args);
        }
    }
}
