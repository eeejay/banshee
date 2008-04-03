//
// ImportManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.Collections.Generic;
using System.Threading;
using Mono.Unix;

using Hyena;
using Banshee.IO;
using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Collection
{
    public class ImportManager
    {
        private class ImportCanceledException : ApplicationException
        {
        }
        
        private readonly object user_job_mutex = new object ();
        private UserJob user_job;
        
        private Queue<string> path_queue = new Queue<string> ();
        private bool processing_queue = false;
        
        private int total_count;
        private int processed_count;
        private int scan_ref_count = 0;
        
        public event ImportEventHandler ImportRequested;
        public event EventHandler ImportFinished;
        
        public ImportManager ()
        {
        }
        
        public bool IsImportInProgress {
            get { return processing_queue; }
        }

        private bool keep_user_job_hidden = false;
        public bool KeepUserJobHidden {
            get { return keep_user_job_hidden; }
            set { keep_user_job_hidden = value; }
        }
        
        private void CreateUserJob ()
        {
            lock (user_job_mutex) {
                if (user_job != null) {
                    return;
                }
                
                user_job = new UserJob (Title, Title, Catalog.GetString ("Scanning for media"));
                user_job.IconNames = new string [] { "system-search", "gtk-find" };
                user_job.CancelMessage = CancelMessage;
                user_job.CanCancel = true;

                if (!KeepUserJobHidden) {
                    user_job.Register ();
                }
                
                total_count = 0;
                processed_count = 0;
            }
        }
        
        private void DestroyUserJob ()
        {
            lock (user_job_mutex) {
                if (user_job == null) {
                    return;
                }
                
                user_job.Finish ();
                user_job = null;
                    
                total_count = 0;
                processed_count = 0;
                scan_ref_count = 0;
            }
        }
        
        protected void IncrementProcessedCount (string message)
        {
            CreateUserJob ();
            processed_count++;
            
            double new_progress = (double)processed_count / (double)total_count;
            double old_progress = user_job.Progress;
            
            if (new_progress >= 0.0 && new_progress <= 1.0 && Math.Abs (new_progress - old_progress) > 0.001) {
                string disp_progress = String.Format (ProgressMessage, processed_count, total_count);
                
                user_job.Title = disp_progress;
                user_job.Status = String.IsNullOrEmpty (message) ? Catalog.GetString ("Scanning...") : message;
                user_job.Progress = new_progress;
            }
        }
        
        private void CheckForCanceled ()
        {
            lock (user_job_mutex) {
                if (user_job != null && user_job.IsCancelRequested) {
                    throw new ImportCanceledException ();  
                }
            }
        }
        
        private void FinalizeImport ()
        {
            path_queue.Clear ();
            processing_queue = false;
            DestroyUserJob ();
            OnImportFinished ();
        }
        
        private void Enqueue (string path)
        {
            if (path_queue.Contains (path)) {
                return;
            }
            
            total_count++;
            lock (path_queue) {
                path_queue.Enqueue (path);
            }
        }
        
        public void QueueSource (UriList uris)
        {
            CreateUserJob ();
            ThreadPool.QueueUserWorkItem (ThreadedQueueSource, uris);
        }
        
        public void QueueSource (string source)
        {
            QueueSource (new UriList (source));
        }
        
        public void QueueSource (string [] paths)
        {
            QueueSource (new UriList (paths));
        }

        private void ThreadedQueueSource (object o)
        {
            UriList uris = (UriList)o;

            try {
                foreach (string path in uris.LocalPaths) {
                    Interlocked.Increment (ref scan_ref_count);
                    ScanForFiles (path);
                    Interlocked.Decrement (ref scan_ref_count);
                }
                
                if(scan_ref_count == 0) {
                    ProcessQueue ();
                }
            } catch (ImportCanceledException) {
                FinalizeImport ();
            }
        }
        
        private void ScanForFiles (string source)
        {
            CheckForCanceled ();
            Interlocked.Increment (ref scan_ref_count);
            
            bool is_regular_file = false;
            bool is_directory = false;
            
            SafeUri source_uri = new SafeUri (source);
            
            try {
                is_regular_file = Banshee.IO.File.Exists (source_uri);
                is_directory = !is_regular_file && Banshee.IO.Directory.Exists (source);
            } catch {
                Interlocked.Decrement (ref scan_ref_count);
                return;
            }
            
            if (is_regular_file) {
                try {
                    if (!Path.GetFileName (source).StartsWith (".")) {
                        Enqueue (source);
                    }
                } catch (System.ArgumentException) {
                    // If there are illegal characters in path
                }
            } else if (is_directory) {
                try {
                    if (!Path.GetFileName (Path.GetDirectoryName (source)).StartsWith (".")) {
                        try {
                            foreach (string file in Banshee.IO.Directory.GetFiles (source)) {
                                ScanForFiles (file);
                            }

                            foreach (string directory in Banshee.IO.Directory.GetDirectories (source)) {
                                ScanForFiles (directory);
                            }
                        } catch {
                        }
                    }
                } catch (System.ArgumentException) {
                    // If there are illegal characters in path
                }
            }

            Interlocked.Decrement (ref scan_ref_count);
        }

        private void ProcessQueue ()
        {
            if (processing_queue) {
                return;
            }
            
            processing_queue = true;
            uint timer_id = Log.DebugTimerStart ();
            
            while (path_queue.Count > 0) {
                CheckForCanceled ();
                OnImportRequested (path_queue.Dequeue ());
            }
            
            Log.DebugTimerPrint (timer_id, Title + " duration: {0}");
            
            path_queue.Clear ();
            processing_queue = false;
            
            if (scan_ref_count == 0) {
                DestroyUserJob ();
                OnImportFinished ();
            }
        }
        
        protected virtual void OnImportRequested (string path)
        {
            ImportEventHandler handler = ImportRequested;
            if (handler != null && path != null) {
                ImportEventArgs args = new ImportEventArgs (path);
                handler (this, args);
                IncrementProcessedCount (args.ReturnMessage);
            } else {
                IncrementProcessedCount (null);
            }
        }
        
        protected virtual void OnImportFinished ()
        {
            EventHandler handler = ImportFinished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        private string title = Catalog.GetString ("Importing Media");
        public string Title {
            get { return title; }
            set { title = value; }
        }

        private string cancel_message = Catalog.GetString ("The import process is currently running. Would you like to stop it?");
        public string CancelMessage {
            get { return cancel_message; }
            set { cancel_message = value; }
        }

        private string progress_message = Catalog.GetString ("Importing {0} of {1}");
        public string ProgressMessage {
            get { return progress_message; }
            set { progress_message = value; }
        }
    }
}
