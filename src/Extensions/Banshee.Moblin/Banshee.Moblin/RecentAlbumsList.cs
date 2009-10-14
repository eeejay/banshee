// 
// RecentAlbumsList.cs
//  
// Author:
//   Gabriel Burt <gburt@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Moblin
{
    public class RecentAlbumsList
    {
        private List<AlbumInfo> list = new List<AlbumInfo> ();
        private int max_count;
        private HyenaSqliteCommand select_cmd;

        public event EventHandler Changed;

        public RecentAlbumsList (int maxCount)
        {
            max_count = maxCount;

            select_cmd = new HyenaSqliteCommand (@"
                SELECT a.AlbumID, a.Title, a.ArtistName, a.IsCompilation, MAX(t.LastPlayedStamp) as MaxLastPlayed
                    FROM CoreAlbums a, CoreTracks t 
                    WHERE t.PrimarySourceID = ? AND a.AlbumID = t.AlbumID AND a.Title != ''
                    GROUP BY a.AlbumID
                    ORDER BY MaxLastPlayed DESC
                    LIMIT ?");

            Reload ();
            ServiceManager.PlaybackController.TrackStarted += (o, a) => Reload ();
        }

        public ReadOnlyCollection<AlbumInfo> Albums {
            get { return list.AsReadOnly (); }
        }

        private void Reload ()
        {
            list.Clear ();

            using (var reader = ServiceManager.DbConnection.Query (select_cmd, 1, max_count)) {
                while (reader.Read ()) {
                    list.Add (new AlbumInfo (reader[1] as string) {
                        ArtistName = reader[2] as string
                    });
                }
            }

            Dump ();
            OnChanged ();
        }

        private void Dump ()
        {
            Console.WriteLine ("RecentAlbumsList has {0} albums", list.Count);
            foreach (var album in Albums) {
                Console.WriteLine ("Recent Album: {0} by {1} (ArtworkId: {2})", album.Title, album.ArtistName, album.ArtworkId);
            }
        }

        private void OnChanged ()
        {
            var handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}
