/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Library.cs
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
using System.Collections;
using System.Data;
using System.IO;
using Mono.Unix;

using Sql;

namespace Banshee.Base
{
    public class Library
    {
        public Database Db;
        public Hashtable Tracks = new Hashtable();
        public Hashtable TracksFnKeyed = new Hashtable();
        public Hashtable Playlists = new Hashtable();
        
        public event EventHandler Reloaded;
        public event EventHandler Updated;
        
        public Library()
        {
            string libraryLocation = Location;
            
            string db_file = Path.Combine(Paths.ApplicationData, "banshee.db");
            string olddb_file = libraryLocation + Path.DirectorySeparatorChar + ".banshee.db";

            try {
                if(!Directory.Exists(libraryLocation)) {
                    Directory.CreateDirectory(libraryLocation);
                }
            } catch(Exception) {
                Console.WriteLine("Could not create Library directory: " + libraryLocation);
            }
            
            if(!Directory.Exists(Paths.ApplicationData)) {
                Directory.CreateDirectory(Paths.ApplicationData);
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
            
            Db = new Database("Library",  db_file);
        }
        
        public void ReloadLibrary()
        {
            ThreadAssist.Spawn(ReloadLibraryThread);
        }
        
        private void ReloadLibraryThread()
        {
            Tracks.Clear();

            IDataReader reader = Db.Query(new Select("Tracks"));
            while(reader.Read()) {
                try {
                    new LibraryTrackInfo(reader);
                } catch(Exception e) {
                    LogCore.Instance.PushWarning(
                        Catalog.GetString("Could not load track from library"),
                        (reader["Uri"] as string) + ": " + e.Message, false);
                }
            }
            
            ThreadAssist.ProxyToMain(delegate {
                EventHandler handler = Reloaded;
                if(handler != null) {
                    handler(this, new EventArgs());
                }
            });
        }

        public string Location
        {
             get {
                string libraryLocation;
            
                try {
                    libraryLocation = (string)Globals.Configuration.Get(GConfKeys.LibraryLocation);
                } catch(Exception) {
                    libraryLocation = Paths.DefaultLibraryPath;
                }
            
                Globals.Configuration.Set(GConfKeys.LibraryLocation, libraryLocation);
                
                return libraryLocation;             
             }    
        }
        
        public void SetTrack(int id, LibraryTrackInfo track)
        {
            lock(Tracks.SyncRoot) {
                Tracks[id] = track;
            }
                
            lock(TracksFnKeyed.SyncRoot) {
                TracksFnKeyed[MakeFilenameKey(track.Uri)] = track;
            }
            
            EventHandler handler = Updated;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public void Remove(LibraryTrackInfo track)
        {
            lock(Tracks.SyncRoot) {
                Tracks.Remove(track.TrackId);
            }
            
            lock(TracksFnKeyed.SyncRoot) {
                TracksFnKeyed.Remove(MakeFilenameKey(track.Uri));
            }
        }
        
        public void Remove(int trackID, System.Uri trackUri)
        {
            lock(Tracks.SyncRoot) {
                Tracks.Remove(trackID);
            }
            
            lock(TracksFnKeyed.SyncRoot) {
                TracksFnKeyed.Remove(MakeFilenameKey(trackUri));
            }
        }
        
        public LibraryTrackInfo GetTrack(int id)
        {
            return Tracks[id] as LibraryTrackInfo;
        }
        
        public static string MakeFilenameKey(Uri uri)
        {
            return PathUtil.MakeFileNameKey(uri);
        }
    }
}
