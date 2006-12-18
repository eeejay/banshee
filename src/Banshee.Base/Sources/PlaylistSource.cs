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
using Banshee.Database;

namespace Banshee.Sources
{
    public class PlaylistSource : AbstractPlaylistSource
    {
        private static List<PlaylistSource> playlists = new List<PlaylistSource>();
        private DbParameter<int> playlist_id_param = new DbParameter<int>("playlist_id");
    
        public static IEnumerable<PlaylistSource> Playlists {
            get { return playlists; }
        }
        
        public static int PlaylistCount {
            get { return playlists.Count; }
        }
        
        private Queue<TrackInfo> remove_queue = new Queue<TrackInfo>();
        private Queue<TrackInfo> append_queue = new Queue<TrackInfo>();
        
        public override int Id {
            get { return id; }
            protected set {
                id = value;
                playlist_id_param.Value = id;
            }
        }

        public PlaylistSource() : this(0)
        {
        }

        public PlaylistSource(string name) : base(name, 500)
        {
        }

        public PlaylistSource(int id) : base(Catalog.GetString("New Playlist"), 500)
        {
            Id = id;
            
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
            Id = Globals.Library.Db.Execute(new DbCommand(
                @"INSERT INTO Playlists
                    VALUES (NULL, :playlist_name, -1, 0)",
                    "playlist_name", Name
            ));
        }
        
        private void LoadFromDatabase()
        {
            Name = (string)Globals.Library.Db.QuerySingle(new DbCommand(
                "SELECT Name FROM Playlists WHERE PlaylistID = :playlist_id",
                playlist_id_param));
            
            // check to see if ViewOrder has ever been set, if not, perform
            // a default ordering as a compatibility update
            if(Convert.ToInt32(Globals.Library.Db.QuerySingle(new DbCommand(
                @"SELECT COUNT(*) 
                    FROM PlaylistEntries
                    WHERE PlaylistID = :playlist_id
                        AND ViewOrder > 0",
                    playlist_id_param))) <= 0) {
                Console.WriteLine("Performing compatibility update on playlist '{0}'", Name);
                Globals.Library.Db.Execute(new DbCommand(
                    @"UPDATE PlaylistEntries
                        SET ViewOrder = (ROWID -
                            (SELECT COUNT(*) 
                                FROM PlaylistEntries
                                WHERE PlaylistID < :playlist_id))
                        WHERE PlaylistID = :playlist_id",
                        playlist_id_param
                ));
            }
   
            IDataReader reader = Globals.Library.Db.Query(new DbCommand(
                @"SELECT TrackID 
                    FROM PlaylistEntries
                    WHERE PlaylistID = :playlist_id
                    ORDER BY ViewOrder",
                    playlist_id_param
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
            if (newName.Length > 256) {
                newName = newName.Substring (0, 256);
            }
            
            if(oldName.Equals(newName)) {
                return false;
            }
            
            if(PlaylistUtil.PlaylistExists(newName)) {
                LogCore.Instance.PushWarning(
                    Catalog.GetString("Cannot Rename Playlist"),
                    Catalog.GetString("A playlist with this name already exists. Please choose another name."));
                return false;
            }
            
            DbCommand command = new DbCommand(
                @"UPDATE Playlists
                    SET Name = :playlist_name
                    WHERE PlaylistID = :playlist_id",
                    "playlist_name", newName,
                    playlist_id_param
            );
          
            try {
                Globals.Library.Db.Execute(command);
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

        public override bool Unmap()
        {
            if (base.Unmap()) {
                Globals.Library.Db.Execute(new DbCommand(
                    @"DELETE FROM PlaylistEntries
                        WHERE PlaylistID = :playlist_id",
                        playlist_id_param
                ));
                
                Globals.Library.Db.Execute(new DbCommand(
                    @"DELETE FROM Playlists
                        WHERE PlaylistID = :playlist_id",
                        playlist_id_param
                ));
            
                append_queue.Clear();
                remove_queue.Clear();

                playlists.Remove(this);

                return true;
            }

            return false;
        }
        
        public override void Commit()
        {
            if(remove_queue.Count > 0) {
                lock(TracksMutex) {
                    while(remove_queue.Count > 0) {
                        TrackInfo track = remove_queue.Dequeue();
                        Globals.Library.Db.Execute(new DbCommand(
                            @"DELETE FROM PlaylistEntries
                                WHERE PlaylistID = :playlist_id
                                AND TrackID = :track_id",
                                "track_id", track.TrackId,
                                playlist_id_param
                        ));
                        OnTrackRemoved(track);
                    }
                }
            }
            
            if(append_queue.Count > 0) {
                lock(TracksMutex) {
                    while(append_queue.Count > 0) {
                        TrackInfo track = append_queue.Dequeue();
                        Globals.Library.Db.Execute(new DbCommand(
                            @"INSERT INTO PlaylistEntries 
                                VALUES (NULL, :playlist_id, :track_id, (
                                    SELECT CASE WHEN MAX(ViewOrder)
                                        THEN MAX(ViewOrder) + 1
                                        ELSE 1 END
                                    FROM PlaylistEntries 
                                    WHERE PlaylistID = :playlist_id)
                                )", 
                                "track_id", track.TrackId,
                                playlist_id_param
                        ));
                        OnTrackAdded(track);
                    }
                }
            }
        }
        
        public override void Reorder(TrackInfo track, int position)
        {
            lock(TracksMutex) {
                int sql_position = 1;
            
                if(position > 0) {
                    TrackInfo sibling = tracks[position];
                    if(sibling == track || sibling == null) {
                        return;
                    }
                    
                    sql_position = Convert.ToInt32(Globals.Library.Db.QuerySingle(new DbCommand(
                        @"SELECT ViewOrder
                            FROM PlaylistEntries
                            WHERE PlaylistID = :playlist_id
                                AND TrackID = :track_id
                            LIMIT 1", 
                            "track_id", sibling.TrackId, 
                            playlist_id_param)
                    ));
                } else if(tracks[position] == track) {
                    return;
                } 
                
                Globals.Library.Db.Execute(new DbCommand(
                    @"UPDATE PlaylistEntries
                        SET ViewOrder = ViewOrder + 1
                        WHERE PlaylistID = :playlist_id
                            AND ViewOrder >= :sql_position",
                    "sql_position", sql_position, 
                    playlist_id_param
                ));
                
                Globals.Library.Db.Execute(new DbCommand(
                    @"UPDATE PlaylistEntries
                        SET ViewOrder = :sql_position
                        WHERE PlaylistID = :playlist_id
                            AND TrackID = :track_id",
                    "sql_position", sql_position, 
                    "track_id", track.TrackId,
                    playlist_id_param
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
        
        public override int SortColumn {
            get { 
                try {
                    return Convert.ToInt32(Globals.Library.Db.QuerySingle(new DbCommand(
                        @"SELECT SortColumn
                            FROM Playlists
                            WHERE PlaylistID = :playlist_id",
                            playlist_id_param)));
                } catch {
                    return base.SortColumn;
                }
            }
            
            set {
                try {
                    Globals.Library.Db.Execute(new DbCommand(
                        @"UPDATE Playlists
                            SET SortColumn = :sort_column
                            WHERE PlaylistID = :playlist_id",
                            "sort_column", value,
                            playlist_id_param));
                } catch {
                    base.SortColumn = value;
                }
            }
        }        
        
        public override Gtk.SortType SortType {
            get { 
                try {
                    return (Gtk.SortType)Convert.ToInt32(Globals.Library.Db.QuerySingle(new DbCommand(
                        @"SELECT SortType
                            FROM Playlists
                            WHERE PlaylistID = :playlist_id",
                            playlist_id_param)));
                } catch {
                    return base.SortType;
                }
            }
            
            set {
                try {
                    Globals.Library.Db.Execute(new DbCommand(
                        @"UPDATE Playlists
                            SET SortType = :sort_type
                            WHERE PlaylistID = :playlist_id",
                            "sort_type", value,
                            playlist_id_param));
                } catch {
                    base.SortType = value;
                }
            }
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
            try {
                return Convert.ToInt32(Globals.Library.Db.QuerySingle(new DbCommand(
                @"SELECT PlaylistID
                    FROM Playlists
                    WHERE Name = :name
                    LIMIT 1",
                    "name", name
                )));
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
