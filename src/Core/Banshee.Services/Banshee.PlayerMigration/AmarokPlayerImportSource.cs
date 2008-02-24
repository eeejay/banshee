//
// AmarokPlayerImportSource.cs
//
// Author:
//   Sebastian Dröge <slomo@circular-chaos.org>
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2006 Sebastian Dröge, Scott Peterson
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
using System.Data;
using System.IO;

using Mono.Data.Sqlite;
using Mono.Unix;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.ServiceStack;

namespace Banshee.PlayerMigration
{
    public sealed class AmarokPlayerImportSource : ThreadPoolImportSource
    {
        private static readonly string library_path = Path.Combine ( Path.Combine (Path.Combine (Path.Combine (Path.Combine (
                                                 Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                                 ".kde"),
                                                 "share"),
                                                 "apps"),
                                                 "amarok"),
                                                 "collection.db");

        protected override void ImportCore ()
        {
            LibraryImportManager import_manager = ServiceManager.Get<LibraryImportManager> ("LibraryImportManager");
            IDbConnection conn;

            try {
                conn = new SqliteConnection ("Version=3,URI=file://" + library_path);
                conn.Open ();
            } catch (Exception e) {
                LogError (library_path, String.Format (
                    "Unable to open Amarok database: {0}", e.Message));
                return;
            }
            
            int count = 0;
            try {
                IDbCommand cmd = conn.CreateCommand ();
                cmd.CommandText = @"
                                    SELECT COUNT(*)
                                    FROM tags";
                count = Convert.ToInt32 (cmd.ExecuteScalar ());
            } catch (Exception) {}

            try {
                IDbCommand cmd = conn.CreateCommand ();
                cmd.CommandText = @"
                                    CREATE TEMP TABLE devices_tmp
                                           (id INTEGER PRIMARY KEY,
                                            lastmountpoint VARCHAR(255));
                                    INSERT INTO devices_tmp (id, lastmountpoint)
                                           SELECT devices.id,
                                                  devices.lastmountpoint
                                           FROM devices;
                                    INSERT OR IGNORE INTO devices_tmp (id, lastmountpoint)
                                           VALUES (-1, '/');";
                cmd.ExecuteNonQuery ();
                
                cmd = conn.CreateCommand ();
                cmd.CommandText = @"
                                    SELECT DISTINCT
                                           devices_tmp.lastmountpoint,
                                           tags.url,
                                           tags.title,
                                           artist.name,
                                           genre.name,
                                           album.name,
                                           year.name,
                                           tags.track,
                                           tags.length,
                                           tags.deviceid
                                     FROM  tags,
                                           devices_tmp,
                                           artist,
                                           album,
                                           genre,
                                           year
                                     WHERE tags.deviceid = devices_tmp.id
                                       AND tags.artist = artist.id
                                       AND tags.album = album.id
                                       AND tags.genre = genre.id
                                       AND tags.year = year.id";

                 IDataReader reader = cmd.ExecuteReader ();
                 int processed = 0;

                 while (reader.Read ()) {
                     if (CheckForCanceled ())
                         break;

                     processed++;

                     try {
                         string mountpoint = (string) reader[0], path = (string) reader[1];
                         SafeUri uri = null;
                         if (path.StartsWith ("./")) {
                             uri = new SafeUri (Path.Combine (mountpoint, path.Substring (2)));
                         } else if (path.StartsWith ("/")) {
                             uri = new SafeUri (path);
                         } else {
                             continue;
                         }

                         string title = (string) reader[2];
                         string artist = (string) reader[3];
                         
                         // the following fields are not critical and can be skipped if something goes wrong
                         string genre = reader[4] as string;
                         string album = reader[5] as string;
                         int year = 0, rating = 0, playcount = 0;
                         uint track_number = 0;
                         TimeSpan duration = TimeSpan.Zero;

                         try {
                             year = Int32.Parse ((string) reader[6]);
                         } catch (Exception) {}

                         try {
                             track_number = Convert.ToUInt32 ((long) reader[7]);
                         } catch (Exception) {}

                         try {
                             duration = TimeSpan.FromSeconds ((int) reader[8]);
                         } catch (Exception) {}

                         // Try to read stats
                         try {
                             int deviceid = Convert.ToInt32 (reader [9]);

                             IDbCommand stats_cmd = conn.CreateCommand ();
                             stats_cmd.CommandText = @"
                                                     SELECT DISTINCT
                                                            statistics.percentage,
                                                            statistics.playcounter
                                                     FROM   statistics
                                                     WHERE  statistics.url = :path
                                                       AND  statistics.deviceid = :deviceid";
                             stats_cmd.Parameters.Add (new SqliteParameter ("path", path));
                             stats_cmd.Parameters.Add (new SqliteParameter ("deviceid", deviceid));

                             IDataReader stats_reader = stats_cmd.ExecuteReader ();

                             while (stats_reader.Read ()) {
                                 rating = (int) Math.Round (5.0 * (Convert.ToDouble (stats_reader[0]) / 100.0));
                                 playcount = Convert.ToInt32 (stats_reader[1]);
                             }
                             stats_reader.Close ();
                         } catch (Exception) {}

                         UpdateUserJob (processed, count, artist, title);
                     
                         try {
                             DatabaseTrackInfo track = import_manager.AddTrackToLibrary (uri);
                            
                             if (track == null) {
                                 throw new Exception (String.Format ("Unable to import track: {0}", uri.AbsoluteUri));
                             }
                            
                             track.Rating = rating;
                             track.PlayCount = playcount;
                             track.Save ();
                         } catch (Exception e) {
                             LogError (SafeUri.UriToFilename (uri), e);
                         }
                     } catch (Exception) {
                         // something went wrong, skip entry
                     }
                 }

                 try {
                     reader.Close ();
                     conn.Close ();
                 } catch (Exception) {}
            } catch (Exception e) {
                LogError (library_path, "Importing from Amarok database failed");
            }
        }
        
        public static new bool CanImport
        {
            get { return File.Exists (library_path); }
        }
        
        public override string Name
        {
            get { return Catalog.GetString ("Amarok"); }
        }

        public override string [] IconNames {
            get { return new string [] { "system-search" }; }
        }
    }
}