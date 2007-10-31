/*
 *  Copyright (c) 2006 Sebastian Dröge <slomo@circular-chaos.org> 
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
using System.Xml;
using System.IO;

using Gtk;
using Mono.Unix;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.Collection.Database;

namespace Banshee.PlayerMigration
{
    public class RhythmboxPlayerImport : PlayerImport
    {
        private static readonly string library_path = Path.Combine (Path.Combine (Path.Combine (
                                                 Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                                 ".gnome2"),
                                                 "rhythmbox"),
                                                 "rhythmdb.xml");
        public override bool CanImport
        {
            get { return File.Exists (library_path); }
        }
        public override string Name
        {
            get { return Catalog.GetString ("Rhythmbox Music Player"); }
        }

        protected override void OnImport () {
            StreamReader stream_reader = new StreamReader (library_path);
            XmlDocument xml_doc = new XmlDocument ();
            xml_doc.Load (stream_reader);
            XmlElement root = xml_doc.DocumentElement;

            if (root == null || !root.HasChildNodes || root.Name != "rhythmdb") {
                Banshee.Sources.ImportErrorsSource.Instance.AddError(library_path,
                    Catalog.GetString("Invalid Rhythmbox database file"), null);
                return;
            }
            
            int count = root.ChildNodes.Count, processed = 0;

            foreach (XmlElement entry in root.ChildNodes) {
                if (user_event.IsCancelRequested)
                    break;

                processed++;
                
                if (entry == null || !entry.HasAttribute ("type") || entry.GetAttribute ("type") != "song")
                    continue;
                    
                string title = String.Empty, genre = String.Empty, artist = String.Empty, album = String.Empty;
                uint track_number = 0, rating = 0, play_count = 0;
                int year = 0;
                TimeSpan duration = TimeSpan.Zero;
                DateTime date_added = DateTime.Now, last_played = DateTime.MinValue;
                SafeUri uri = null;

                foreach (XmlElement child in entry.ChildNodes) {
                    if (child == null || child.InnerText == null || child.InnerText == String.Empty)
                        continue;

                    try {
                        switch (child.Name) {
                            case "title":
                                title = child.InnerText;
                                break;
                            case "genre":
                                genre = child.InnerText;
                                break;
                            case "artist":
                                artist = child.InnerText;
                                break;
                            case "album":
                                album = child.InnerText;
                                break;
                            case "track-number":
                                track_number = UInt32.Parse (child.InnerText);
                                break;
                            case "duration":
                                duration = TimeSpan.FromSeconds (Int32.Parse (child.InnerText));
                                break;
                            case "location":
                                uri = new SafeUri (child.InnerText);
                                break;
                            case "date":
                                if (child.InnerText != "0")
                                    year = (new DateTime (1, 1, 1).AddDays (Double.Parse (child.InnerText))).Year;
                                break;
                            case "rating":
                                rating = UInt32.Parse (child.InnerText[0].ToString ());
                                break;
                            case "first-seen":
                                date_added = Mono.Unix.Native.NativeConvert.ToDateTime (Int64.Parse (child.InnerText));
                                break;
                            case "play-count":
                                play_count = UInt32.Parse (child.InnerText);
                                break;
                            case "last-played":
                                last_played = Mono.Unix.Native.NativeConvert.ToDateTime (Int64.Parse (child.InnerText));
                                break;
                        }
                    } catch (Exception) {
                        // parsing InnerText failed
                    }
                }
                if (uri == null)
                    continue;

                UpdateUserEvent (processed, count, artist, title);
                
                try {
                    // FIXME merge
                    /*LibraryTrackInfo ti = new LibraryTrackInfo (uri, artist, album, title, genre, track_number, 0, year, duration,
                        String.Empty, RemoteLookupStatus.NoAttempt);
                    ti.Rating = rating;
                    ti.DateAdded = date_added;
                    ti.PlayCount = play_count;
                    ti.LastPlayed = last_played;*/
                } catch (Exception e) {
                    Banshee.Sources.ImportErrorsSource.Instance.AddError(SafeUri.UriToFilename (uri), e.Message, e);
                }
            }
            stream_reader.Close ();
        }
    }
}
