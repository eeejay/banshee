/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  ImportManager.cs
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
using System.IO;
using System.Threading;
using Mono.Unix;
using Gtk;

using Banshee.Widgets;

namespace Banshee.Base
{
    public delegate void ImportEventHandler(object o, ImportEventArgs args);
    
    public class ImportEventArgs : EventArgs
    {
        public string FileName;
        public string ReturnMessage;
    }

    public class ImportManager
    {
        private class ImportCanceledException : ApplicationException
        {
        }
        
        private static ImportManager instance;
        public static ImportManager Instance {
            get {
                if(instance == null) {
                    instance = new ImportManager();
                }
                
                return instance;
            }
        }
            
        private Queue path_queue;
        private ActiveUserEvent user_event;
        private int total_count;
        private int processed_count;
        private int scan_ref_count = 0;
        private bool processing_queue = false;
        
        public event ImportEventHandler ImportRequested;
        
        private ImportManager()
        {
            path_queue = new Queue();
        }
        
        private void CreateUserEvent()
        {
            if(user_event == null) {
                user_event = new ActiveUserEvent(Catalog.GetString("Importing Songs"));
                user_event.Icon = IconThemeUtils.LoadIcon("system-search", 22);
                lock(user_event) {
                    user_event.Message = Catalog.GetString("Scanning for songs");
                    total_count = 0;
                    processed_count = 0;
                }
            }
        }
        
        private void DestroyUserEvent()
        {
            if(user_event != null) {
                lock(user_event) {
                    user_event.Dispose();
                    user_event = null;
                    total_count = 0;
                    processed_count = 0;
                    scan_ref_count = 0;
                }
            }
        }
        
        private void UpdateCount(string message)
        {
            CreateUserEvent();
            processed_count++;
            
            double new_progress = (double)processed_count / (double)total_count;
            double old_progress = user_event.Progress;
            
            if(new_progress >= 0.0 && new_progress <= 1.0 && Math.Abs(new_progress - old_progress) > 0.01) {
                string disp_message = String.Format(Catalog.GetString("{0} of {1}"),
                    processed_count, total_count);
                if(message != null) {
                    disp_message += ": " + message;
                }
                
                user_event.Message = disp_message;
                user_event.Progress = new_progress;
            }
        }
        
        private void CheckForCanceled()
        {
            if(user_event != null && user_event.IsCancelRequested) {
                throw new ImportCanceledException();  
            }
        }
        
        private void FinalizeImport()
        {
            path_queue.Clear();
            processing_queue = false;
            DestroyUserEvent();
        }
        
        private void Enqueue(string path)
        {
            if(path_queue.Contains(path)) {
                return;
            }
            
            total_count++;
            lock(path_queue.SyncRoot) {
                path_queue.Enqueue(path);
            }
        }
        
        public void QueueSource(UriList uris)
        {
            CreateUserEvent();
            ThreadAssist.Spawn(delegate {
                try {
                    foreach(string path in uris.LocalPaths) {
                        scan_ref_count++;
                        ScanForFiles(path);
                        scan_ref_count--;
                    }
                    
                    if(scan_ref_count == 0) {
                        ProcessQueue();
                    }
                } catch(ImportCanceledException) {
                    FinalizeImport();
                }
            });
        }
        
        public void QueueSource(Gtk.SelectionData selection)
        {
            QueueSource(new UriList(selection));
        }
        
        public void QueueSource(string source)
        {
            QueueSource(new UriList(source));
        }
        
        public void QueueSource(string [] paths)
        {
            QueueSource(new UriList(paths));
        }
        
        private void ScanForFiles(string source)
        {
            CheckForCanceled();
            scan_ref_count++;

            if(File.Exists(source) && !Path.GetFileName(source).StartsWith(".")) {
                Enqueue(source);
            } else if(Directory.Exists(source) && 
                !Path.GetFileName(Path.GetDirectoryName(source)).StartsWith(".")) {

                try {
                    foreach(string file in Directory.GetFiles(source)) {
                        ScanForFiles(file);
                    }

                    foreach(string directory in Directory.GetDirectories(source)) {
                        ScanForFiles(directory);
                    }
                } catch(System.UnauthorizedAccessException) {
                }
            }

            scan_ref_count--;
        }
        
        private void ProcessQueue()
        {
            if(processing_queue) {
                return;
            }
            
            processing_queue = true;
            
            while(path_queue.Count > 0) {
                CheckForCanceled();
                
                string filename = path_queue.Dequeue() as string;
                
                ImportEventHandler handler = ImportRequested;
                if(handler != null && filename != null) {
                    ImportEventArgs args = new ImportEventArgs();
                    args.FileName = filename;
                    handler(this, args);
                    UpdateCount(args.ReturnMessage);
                } else {
                    UpdateCount(null);
                }
            }

            path_queue.Clear();
            processing_queue = false;
            
            if(scan_ref_count == 0) {
                DestroyUserEvent();
            }
        }
    }
}

