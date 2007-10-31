/***************************************************************************
 *  Library.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Unix;

using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration.Schema;

namespace Banshee.Base
{
    public delegate void LibraryTrackAddedHandler(object o, LibraryTrackAddedArgs args);
    public delegate void LibraryTrackRemovedHandler(object o, LibraryTrackRemovedArgs args);
    
    public class LibraryTrackAddedArgs : EventArgs
    {
        public LibraryTrackInfo Track;
    }
    
    public class LibraryTrackRemovedArgs : EventArgs
    {
        public LibraryTrackInfo Track;
        public ICollection Tracks;
    }

    public class Library
    {
        public BansheeDatabase Db;
        public Dictionary<int, TrackInfo> Tracks = new Dictionary<int, TrackInfo>();
        public Dictionary<string, TrackInfo> TracksFnKeyed = new Dictionary<string, TrackInfo>();
        
        public event EventHandler Reloaded;
        public event EventHandler Updated;
        public event LibraryTrackAddedHandler TrackAdded;
        public event LibraryTrackRemovedHandler TrackRemoved;
        
        private bool is_loaded;
        
        public Library()
        {
            string libraryLocation = Paths.LibraryLocation;
            
            string db_file = Path.Combine(Paths.ApplicationData, "banshee.db");
            string olddb_file = libraryLocation + Path.DirectorySeparatorChar + ".banshee.db";

            try {
                if(!Directory.Exists(libraryLocation)) {
                    Directory.CreateDirectory(libraryLocation);
                }
            } catch(Exception) {
                Console.WriteLine("Could not create Library directory: " + libraryLocation);
            }

            if(!File.Exists(db_file) && File.Exists(olddb_file)) {
                Console.WriteLine("Copied old library to new location");
                
                File.Copy(olddb_file, db_file);
                
                try {
                    File.Delete(olddb_file);
                } catch(Exception) {
                    Console.WriteLine("Could not remove old library");
                }
            }
            
            Db = new BansheeDatabase(db_file);
        }
        
        public void ReloadLibrary()
        {
            ThreadAssist.Spawn(ReloadLibraryThread);
        }
        
        public IEnumerable GetGenreList() 
        {
            IDataReader reader = Db.Query("SELECT DISTINCT Genre FROM Tracks ORDER BY Genre");
            while(reader.Read()) {
                yield return reader["Genre"];
            }
            reader.Dispose();
        }

        private void ReloadLibraryThread()
        {
            Tracks.Clear();

            IDataReader reader = Db.Query("SELECT * FROM Tracks");
            while(reader.Read()) {
                try {
                    new LibraryTrackInfo(reader);
                } catch(Exception e) {
                    LogCore.Instance.PushWarning(
                        Catalog.GetString("Could not load track from library"),
                        (reader["Uri"] as string) + ": " + e.Message, false);
                }
            }
            
            reader.Dispose();
            
            is_loaded = true;
            
            Banshee.Sources.SourceManager.AddSource(Banshee.Sources.LibrarySource.Instance, true);
            
            ThreadAssist.ProxyToMain(delegate {
                EventHandler handler = Reloaded;
                if(handler != null) {
                    handler(this, new EventArgs());
                }
            });
        }
        
        public bool IsLoaded {
            get {
                return is_loaded;
            }
        }
        
        public void SetTrack(int id, LibraryTrackInfo track)
        {
            lock(((IDictionary)Tracks).SyncRoot) {
                Tracks[id] = track;
            }
                
            lock(((IDictionary)TracksFnKeyed).SyncRoot) {
                TracksFnKeyed[MakeFilenameKey(track.Uri)] = track;
            }
            
            EventHandler handler = Updated;
            if(handler != null) {
                handler(this, new EventArgs());
            }
            
            if(!is_loaded) {
                return;
            }
            
            LibraryTrackAddedHandler added_handler = TrackAdded;
            if(added_handler != null) {
                LibraryTrackAddedArgs args = new LibraryTrackAddedArgs();
                args.Track = track;
                ThreadAssist.ProxyToMain(delegate {
                    added_handler(this, args);
                });
            }
        }
        
        private void CollectionRemove(LibraryTrackInfo track)
        {
            lock(((IDictionary)Tracks).SyncRoot) {
                Tracks.Remove(track.DbId);
            }
            
            lock(((IDictionary)TracksFnKeyed).SyncRoot) {
                TracksFnKeyed.Remove(MakeFilenameKey(track.Uri));
            }
        }

        /*private void Remove(SafeUri trackSafeUri)
        {
            Remove(TracksFnKeyed[MakeFilenameKey(trackSafeUri)] as LibraryTrackInfo);
        }*/
        
        public void Remove(LibraryTrackInfo track)
        {
            if(track == null) {
                return;
            }
            
            CollectionRemove(track);
            
            Db.Execute(new DbCommand(
                @"DELETE FROM Tracks
                    WHERE TrackID = :track_id",
                    "track_id", track.DbId
            ));
                        
            LibraryTrackRemovedHandler removed_handler = TrackRemoved;
            if(removed_handler != null) {
                LibraryTrackRemovedArgs args = new LibraryTrackRemovedArgs();
                args.Track = track;
                ThreadAssist.ProxyToMain(delegate {
                    removed_handler(this, args);
                });
            }
        }
        
        public void Remove(ICollection tracks)
        {
            string query = "DELETE FROM Tracks WHERE ";
            int remove_count = 0;
            int invalid_count = 0;
            
            foreach(object o in tracks) {
                LibraryTrackInfo track = null;
                
                if(o is SafeUri) {
                    track = TracksFnKeyed[MakeFilenameKey(o as SafeUri)] as LibraryTrackInfo;
                } else if(o is LibraryTrackInfo) {
                    track = o as LibraryTrackInfo;
                } 
                
                if(track == null) {
                    invalid_count++;
                    continue;
                }

                query += String.Format(" TrackID = '{0}' ", track.DbId);
                if(remove_count < tracks.Count - invalid_count - 1) {
                    query += " OR ";
                }
                
                CollectionRemove(track);
                remove_count++;
            }
            
            if(remove_count > 0) {
                Db.Execute(query);
                            
                LibraryTrackRemovedHandler removed_handler = TrackRemoved;
                if(removed_handler != null) {
                    LibraryTrackRemovedArgs args = new LibraryTrackRemovedArgs();
                    args.Tracks = tracks;
                    ThreadAssist.ProxyToMain(delegate {
                        removed_handler(this, args);
                    });
                }
            }
        }
        
        private ArrayList remove_queue = new ArrayList();
        
        public void QueueRemove(TrackInfo track)
        {
            remove_queue.Add(track);
        }
        
        public void QueueRemove(SafeUri trackSafeUri)
        {
            remove_queue.Add(trackSafeUri);
        }
        
        public void CommitRemoveQueue()
        {
            Remove(remove_queue);
            remove_queue.Clear();
        }
        
        public LibraryTrackInfo GetTrack(int id)
        {
            return Tracks[id] as LibraryTrackInfo;
        }
        
        public static string MakeFilenameKey(SafeUri uri)
        {
            return PathUtil.MakeFileNameKey(uri);
        }
    }
}
