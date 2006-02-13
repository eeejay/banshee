
/***************************************************************************
 *  Watcher.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Doğacan Güney  <dogacan@gmail.com>
 *             Aaron Bockover <aaron@aaronbock.net>
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
using System.Threading;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.Plugins.FileSystemMonitor
{
    public class Watcher : Banshee.Plugins.Plugin
    {
        protected override string ConfigurationName { get { return "FileSystemMonitor"; } }
        public override string DisplayName { get { return "File System Monitor"; } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Automatically keep your Banshee library directory in sync " +
                    "with your music folder. This plugin responds to changes made " +
                    "in the file system to reflect them in your library."
                );
            }
        }
        
        public override string [] Authors {
            get {
                return new string [] { 
                    "Do\u011facan G\u00fcney",
                    "Aaron Bockover"
                };
            }
        }

        private Queue import_queue;
        private Queue remove_queue;
        private bool processing_queues;
        
        private Watch watch;

        protected override void PluginInitialize()
        {
            import_queue = new Queue();
            remove_queue = new Queue();
            
            processing_queues = false;
            
            watch = Inotify.Enabled 
                ? (Watch)new InotifyWatch(Globals.Library.Location)
                : (Watch)new FileSystemWatcherWatch(Globals.Library.Location);
                
            watch.QueueImportRequest += OnWatchQueueImportRequest;
            watch.QueueRemoveRequest += OnWatchQueueRemoveRequest;
        }
        
        protected override void PluginDispose()
        {
            watch.QueueImportRequest -= OnWatchQueueImportRequest;
            watch.QueueRemoveRequest -= OnWatchQueueRemoveRequest;
            
            while(processing_queues);
            
            import_queue.Clear();
            remove_queue.Clear();
            import_queue = null;
            remove_queue = null;
            
            watch.Dispose();
        }
        
        private void OnWatchQueueImportRequest(object o, FileSystemMonitorPathEventArgs args)
        {
            lock(import_queue.SyncRoot) {
                import_queue.Enqueue(args.Path);
            }
            
            ProcessQueues();
        }
        
        private void OnWatchQueueRemoveRequest(object o, FileSystemMonitorPathEventArgs args)
        {
            lock(remove_queue.SyncRoot) {
                remove_queue.Enqueue(args.Path);
            }
            
            ProcessQueues();
        }
        
        private void ProcessQueues()
        {
            if(!processing_queues) {
                ThreadAssist.Spawn(ProcessQueuesThread);
            }
        }
        
        private void ProcessQueuesThread()
        {
            processing_queues = true;
            
            if(remove_queue.Count > 0) {
                while(remove_queue.Count > 0) {
                    if(DisposeRequested) {
                        processing_queues = false;
                        return;
                    }
    
                    string path = null;

                    lock(remove_queue.SyncRoot) {
                        path = remove_queue.Dequeue() as string;
                    }
                    
                    if(path == null) {
                        continue;
                    }
                    
                    Uri uri = new Uri(path);
                    if(uri == null) {
                        continue;
                    }
                    
                    string query = String.Format(
                        @"SELECT TrackID
                            FROM Tracks 
                            WHERE Uri LIKE '{0}/%'
                                OR Uri = '{0}'",
                                Sql.Statement.EscapeQuotes(uri.AbsoluteUri)
                    );
                    
                    IDataReader reader = Globals.Library.Db.Query(query);
                    
                    while(reader.Read()) {
                        int id = Convert.ToInt32(reader[0]);
                        if(id > 0) {
                            Globals.Library.QueueRemove(Globals.Library.Tracks[id] as LibraryTrackInfo);
                        }
                    }
                    
                    reader.Dispose();
                }
                
                Globals.Library.CommitRemoveQueue();
            }

            while(import_queue.Count > 0) {
                if(DisposeRequested) {
                    processing_queues = false;
                    return;
                }

                lock(import_queue.SyncRoot) {
                    string path = import_queue.Dequeue() as string;
                    ImportManager.Instance.QueueSource(path);
                    watch.RecurseDirectory(path);
                }
            }

            if(import_queue.Count > 0 || remove_queue.Count > 0) {
                ProcessQueuesThread();
            }
            
            processing_queues = false;
        }
    }
}
