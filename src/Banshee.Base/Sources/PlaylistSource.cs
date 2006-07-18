/***************************************************************************
 *  PlaylistSource.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.Sources
{
    public class PlaylistSource : ChildSource
    {
        private static ArrayList playlists = new ArrayList();
    
        public static IEnumerable Playlists {
            get {
                return playlists;
            }
        }
        
        public static int PlaylistCount {
            get {
                return playlists.Count;
            }
        }
        
        private ArrayList tracks = new ArrayList();
        private Queue remove_queue = new Queue();
        private Queue append_queue = new Queue();
        private int id;

        public int Id {
            get { return id; }
        }

        public override string UnmapLabel {
            get { return Catalog.GetString("Delete Playlist"); }
        }

        public override string GenericName {
            get { return Catalog.GetString("Playlist"); }
        }

        public PlaylistSource() : this(0)
        {
        }

        public PlaylistSource(string name) : base(name, 500)
        {
        }

        public PlaylistSource(int id) : base(Catalog.GetString("New Playlist"), 500)
        {
            this.id = id;
            
            if(id < 0) {
                return;
            } else if(id == 0) {
                CreateNewPlaylist();
            } else {
                if(Globals.Library.IsLoaded) {
                    LoadFromDatabase();
                } else {
                    Globals.Library.Reloaded += OnLibraryReloaded;
                }
            }
            
            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;
            playlists.Add(this);
        }

        private void OnLibraryReloaded (object o, EventArgs args)
        {
            LoadFromDatabase();
        }
        
        private void CreateNewPlaylist()
        {
            id = Globals.Library.Db.Execute(String.Format(
                @"INSERT INTO Playlists
                    VALUES (NULL, '{0}')",
                    Sql.Statement.EscapeQuotes(Name))
            );
        }
        
        private void LoadFromDatabase()
        {   
            Name = (string)Globals.Library.Db.QuerySingle(String.Format(
                @"SELECT Name
                    FROM Playlists
                    WHERE PlaylistID = '{0}'",
                    id
            ));
         
            // check to see if ViewOrder has ever been set, if not, perform
            // a default ordering as a compatibility update
            if(Convert.ToInt32(Globals.Library.Db.QuerySingle(String.Format(
                @"SELECT COUNT(*) 
                    FROM PlaylistEntries
                    WHERE PlaylistID = '{0}'
                        AND ViewOrder > 0",
                    id))) <= 0) {
                Console.WriteLine("Performing compatibility update on playlist '{0}'", Name);
                Globals.Library.Db.Execute(String.Format(
                    @"UPDATE PlaylistEntries
                        SET ViewOrder = (ROWID -
                            (SELECT COUNT(*) 
                                FROM PlaylistEntries
                                WHERE PlaylistID < '{0}'))
                        WHERE PlaylistID = '{0}'",
                        id
                ));
            }
   
            IDataReader reader = Globals.Library.Db.Query(String.Format(
                @"SELECT TrackID 
                    FROM PlaylistEntries
                    WHERE PlaylistID = '{0}'
                    ORDER BY ViewOrder",
                    id
            ));
            
            lock(TracksMutex) {
                while(reader.Read()) {
                    tracks.Add(Globals.Library.Tracks[Convert.ToInt32(reader[0])]);
                }
            }
            
            reader.Dispose();
        }
        
        protected override bool UpdateName(string oldName, string newName)
        {
            if(oldName.Equals(newName)) {
                return false;
            }
            
            if(PlaylistUtil.PlaylistExists(newName)) {
                LogCore.Instance.PushWarning(
                    Catalog.GetString("Cannot Rename Playlist"),
                    Catalog.GetString("A playlist with this name already exists. Please choose another name."));
                return false;
            }
            
            string query = String.Format(
                @"UPDATE Playlists
                    SET Name = '{0}'
                    WHERE PlaylistID = '{1}'",
                    Sql.Statement.EscapeQuotes(newName),
                    id
            );
          
            try {
                Globals.Library.Db.Execute(query);
                Name = newName;
                return true;
            } catch(Exception) {
                return false;
            }
        }
 
        public override void AddTrack(TrackInfo track)
        {
            if(track is LibraryTrackInfo) {
                lock(TracksMutex) {
                    tracks.Add(track);
                    append_queue.Enqueue(track);
                }
                OnUpdated();
            }
        }

        public void ClearTracks()
        {
            tracks.Clear();
        }
        
        public override void RemoveTrack(TrackInfo track)
        {
            lock(TracksMutex) {
                tracks.Remove(track);
                remove_queue.Enqueue(track);
            }
        }
        
        public override void SourceDrop(Source source)
        {
            if(source == this || !(source is PlaylistSource)) {
                return;
            }
            
            foreach(TrackInfo track in source.Tracks) {
                AddTrack(track);
            }
        }

        public bool ContainsTrack(TrackInfo track)
        {
            return tracks.Contains(track);
        }
        
        public override bool Unmap()
        {
            Globals.Library.Db.Execute(String.Format(
                @"DELETE FROM PlaylistEntries
                    WHERE PlaylistID = '{0}'",
                    id
            ));
            
            Globals.Library.Db.Execute(String.Format(
                @"DELETE FROM Playlists
                    WHERE PlaylistID = '{0}'",
                    id
            ));
            
            tracks.Clear();
            append_queue.Clear();
            remove_queue.Clear();
            
            SourceManager.RemoveSource(this);
            playlists.Remove(this);

            return true;
        }
        
        public override void Commit()
        {
            if(remove_queue.Count > 0) {
                lock(TracksMutex) {
                    while(remove_queue.Count > 0) {
                        TrackInfo track = remove_queue.Dequeue() as TrackInfo;
                        Globals.Library.Db.Execute(String.Format(
                            @"DELETE FROM PlaylistEntries
                                WHERE PlaylistID = '{0}'
                                AND TrackID = '{1}'",
                                id, track.TrackId
                        ));
                    }
                }
            }
            
            if(append_queue.Count > 0) {
                lock(TracksMutex) {
                    while(append_queue.Count > 0) {
                        TrackInfo track = append_queue.Dequeue() as TrackInfo;
                        Globals.Library.Db.Execute(String.Format(
                            @"INSERT INTO PlaylistEntries 
                                VALUES (NULL, '{0}', '{1}', (
                                    SELECT CASE WHEN MAX(ViewOrder)
                                        THEN MAX(ViewOrder) + 1
                                        ELSE 1 END
                                    FROM PlaylistEntries 
                                    WHERE PlaylistID = '{0}')
                                )", id, track.TrackId
                        ));
                    }
                }
            }
        }
        
        public override void Reorder(TrackInfo track, int position)
        {
            lock(TracksMutex) {
                int sql_position = 1;
            
                if(position > 0) {
                    TrackInfo sibling = tracks[position] as TrackInfo;
                    if(sibling == track || sibling == null) {
                        return;
                    }
                    
                    sql_position = Convert.ToInt32(Globals.Library.Db.QuerySingle(String.Format(
                        @"SELECT ViewOrder
                            FROM PlaylistEntries
                            WHERE PlaylistID = '{0}'
                                AND TrackID = '{1}'
                            LIMIT 1", id, sibling.TrackId)
                    ));
                } else if(tracks[position] == track) {
                    return;
                } 
                
                Globals.Library.Db.Execute(String.Format(
                    @"UPDATE PlaylistEntries
                        SET ViewOrder = ViewOrder + 1
                        WHERE PlaylistID = '{0}'
                            AND ViewOrder >= '{1}'",
                    id, sql_position
                ));
                
                Globals.Library.Db.Execute(String.Format(
                    @"UPDATE PlaylistEntries
                        SET ViewOrder = '{1}'
                        WHERE PlaylistID = '{0}'
                            AND TrackID = '{2}'",
                    id, sql_position, track.TrackId
                ));
                
                tracks.Remove(track);
                tracks.Insert(position, track);
            }
        }
        
        private void OnLibraryTrackRemoved(object o, LibraryTrackRemovedArgs args)
        {
            if(args.Track != null) {
                if(tracks.Contains(args.Track)) {
                    RemoveTrack(args.Track);
                    
                    if(Count == 0) {
                        Unmap();
                    } else {
                        Commit();
                    }
                }
                
                return;
            } else if(args.Tracks == null) {
                return;
            }
            
            int removed_count = 0;
            
            lock(TracksMutex) {
                foreach(TrackInfo track in args.Tracks) {
                    if(tracks.Contains(track)) {
                        tracks.Remove(track);
                        remove_queue.Enqueue(track);
                        removed_count++;
                    }
                }
            }
            
            if(removed_count > 0) {
                if(Count == 0) {
                    Unmap();
                } else {
                    Commit();
                }
            }
        }
        
        public override IEnumerable Tracks {
            get { return tracks; }
        }
        
        public override object TracksMutex {
            get { return tracks.SyncRoot; }
        }
        
        public override int Count {
            get { return tracks.Count; }
        }  
        
        public override bool IsDragSource {
            get { return true; }
        }
        
        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, "source-playlist");
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
    }
    
    public static class PlaylistUtil
    {
        public static ICollection LoadSources()
        {
            ArrayList sources = new ArrayList();
            IDataReader reader = Globals.Library.Db.Query(@"
                SELECT PlaylistID FROM Playlists"
            );
            
            while(reader.Read()) {
                PlaylistSource playlist = new PlaylistSource(Convert.ToInt32(reader[0]));
                sources.Add(playlist);
            }
            
            reader.Dispose();
            return sources;
        }
    
        internal static int GetPlaylistID(string name)
        {
            string query = String.Format(
                @"SELECT PlaylistID
                    FROM Playlists
                    WHERE Name = '{0}'
                    LIMIT 1",
                    Sql.Statement.EscapeQuotes(name)
            );
            
            try {
                object result = Globals.Library.Db.QuerySingle(query);
                int id = Convert.ToInt32(result);
                return id;
            } catch(Exception) {
                return 0;
            }
        }
        
        internal static bool PlaylistExists(string name)
        {
            return GetPlaylistID(name) > 0;
        }
        
        public static string UniqueName {
            get { return NamingUtil.PostfixDuplicate(Catalog.GetString("New Playlist"), PlaylistExists); }
        }
        
        public static string GoodUniqueName(IEnumerable tracks)
        {
            return NamingUtil.PostfixDuplicate(NamingUtil.GenerateTrackCollectionName(
                tracks, Catalog.GetString("New Playlist")), PlaylistExists);
        }
    }
}
