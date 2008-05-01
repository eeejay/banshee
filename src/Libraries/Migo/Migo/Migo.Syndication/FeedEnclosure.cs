//
// FeedEnclosure.cs
//
// Authors:
//   Mike Urbanski  <michael.c.urbanski@gmail.com>
//   Gabriel Burt  <gburt@novell.com>
//
// Copyright (C) 2007 Michael C. Urbanski
// Copyright (C) 2008 Novell, Inc.
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

using Hyena;
using Hyena.Data.Sqlite;

namespace Migo.Syndication
{
    public class FeedEnclosure
    {
        private static SqliteModelProvider<FeedEnclosure> provider;
        public static SqliteModelProvider<FeedEnclosure> Provider {
            get { return provider; }
            set { provider = value; }
        }
        
        private bool canceled;
        private bool downloading;
        private bool stopped;
        
        private string mimetype;
        private FeedDownloadStatus download_status;        
        private bool active;
        private FeedDownloadError last_download_error;
        private long length;
        
        private string local_path;
        private FeedItem item;
        private string url;     
        
        private readonly object sync = new object ();
        
#region Constructors

        public FeedEnclosure ()
        {
        }
        
#endregion

#region Public Properties

        public FeedDownloadStatus DownloadStatus { 
            get { 
                lock (sync) {
                    return download_status;
                }
            }
            
            internal set { 
                lock (sync) {
                    download_status = value;
                    //Console.WriteLine ("Enclosure:  DownloadStatus:  {0}", downloadStatus);
                    switch (value) {
                    case FeedDownloadStatus.DownloadFailed: goto case FeedDownloadStatus.None;
                    case FeedDownloadStatus.Downloaded: 
                        Save ();
                        goto case FeedDownloadStatus.None;
                    case FeedDownloadStatus.None:
                        ResetDownloading ();
                        break;
                    }
                }
            }             
        }

        public FeedItem Item { 
            get { return item; } 
            internal set {          
                if (value == null) {
                	throw new ArgumentNullException ("Parent");
                }

                item = value as FeedItem;
                
                if (item == null) {
                    throw new ArgumentException (
                        "Parent must be of type FeedItem"
                    );
                }
            }
        }

#endregion

#region Public Methods
        
        public void Save ()
        {
            Provider.Save (this);
        }
        
        public void AsyncDownload ()
        {            
            if (SetDownloading ()) {
                if (!item.QueueDownload (this)) {
                    ResetDownloading ();
                }
            }
        }
        
        public void CancelAsyncDownload ()
        {
            if (SetCanceled ()) {
                CancelAsyncDownloadImpl ();
            }
        }

        public void StopAsyncDownload ()
        {
            if (SetStopped ()) {
                item.StopDownload (this);
            }
        }
        
        // Deletes file associated with enclosure.
        // Does not cancel an active download like the WRP.
        public void RemoveFile ()
        {
            lock (sync) {                
                CheckActive ();            
                
                if (!String.IsNullOrEmpty (local_path) && File.Exists (local_path)) {
                	try {
                        FileAttributes attributes = 
                                File.GetAttributes (local_path) | FileAttributes.ReadOnly;

                        if (attributes == FileAttributes.ReadOnly) {
                            File.Delete (local_path);	
                        }
                        
                        Directory.Delete (Path.GetDirectoryName (local_path));
                	} catch {}
                }
                
                local_path = String.Empty;
                download_status = FeedDownloadStatus.None;                                
                
                Save ();
            }
        }
        
#endregion

#region Database Columns

        [DatabaseColumn]
        public long Length { 
            get { lock (sync) { return length; } } 
            set { length = value; }
        }
        
        [DatabaseColumn ("EnclosureID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private long dbid;
        public long DbId { 
            get { return dbid; }
            internal set { dbid = value; }
        }
        
        [DatabaseColumn ("ParentID")]
        private long parent_id;
        public long ParentId {
            get { return parent_id; }
        }
        
        [DatabaseColumn]
        public string LocalPath { 
            get { lock (sync) { return local_path; } }
            set { local_path = value; }
        }
        
        [DatabaseColumn]
        public string Url { 
            get { lock (sync) { return url; } } 
            set { url = value; }
        }
        
        [DatabaseColumn]
        public string MimeType {
            get { return mimetype; }
            set {
                mimetype = value;
                if (String.IsNullOrEmpty (mimetype)) {
                    mimetype = "application/octet-stream";
                }
            }
        }
        
        [DatabaseColumn]
        public FeedDownloadError LastDownloadError {
            get { lock (sync) { return last_download_error;  } }
            internal set { lock (sync) { last_download_error = value; } }
        }
        
        [DatabaseColumn]
        internal bool Active {
            get { lock (sync) { return active; } }
            set { lock (sync) { active = value; } }
        }

#endregion

        private void CheckActive ()
        {
            if (!active) {
                throw new InvalidOperationException ("Enclosure previously deleted");                    
            }
        }

        internal void ResetDownloading ()
        {
            lock (sync) {
                if (downloading) {
                    canceled = 
                    downloading = 
                    stopped = false;
                }
            }
        }
        
        internal bool SetCanceled ()
        {
            bool ret = false;
            
            lock (sync) {
                //Console.WriteLine ("Status - SetCanceled:  canceled:  {0} - downloading:  {1}", canceled, downloading);
                if (!canceled && !stopped && downloading) {
                    ret = canceled = true;
                    last_download_error = FeedDownloadError.Canceled;
                }   
            }
                //Console.WriteLine ("Status - SetCanceled:  ret:  {0}", ret);
            return ret;
        }
        
        internal bool SetDownloading ()
        {
            bool ret = false;
            
            lock (sync) {
                if (!downloading && 
                    download_status != FeedDownloadStatus.Downloaded) {
                    canceled = false;
                    stopped = false;
                    ret = downloading = true;    
                    
                    download_status = FeedDownloadStatus.Pending;                    
                    last_download_error = FeedDownloadError.None;
                }            
            }
            
            return ret;
        }

        internal bool SetStopped () 
        {
            bool ret = false;
            
            lock (sync) {
                if (!canceled && !stopped && downloading) {
                    ret = stopped = true;
                    last_download_error = FeedDownloadError.Canceled;
                }            
            }
            
            return ret;            
        }
        
        private void CancelAsyncDownloadImpl ()      
        {
            item.CancelDownload (this);            
        }

        internal void SetFileImpl (string url, string path, string mimeType, string filename)
        {      
            string tmpLocalPath;
            string fullPath = path;
            string localEnclosurePath = item.Feed.LocalEnclosurePath;
            
            lock (sync) {   
                CheckActive ();                
                
                if (!localEnclosurePath.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
                    localEnclosurePath += Path.DirectorySeparatorChar;
                }
                
                if (!fullPath.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
                    fullPath += Path.DirectorySeparatorChar;
                }           
                
                fullPath += filename;
                tmpLocalPath = localEnclosurePath+filename;            

                try {
                    if (!Directory.Exists (path)) {
                    	throw new InvalidOperationException ("Directory specified by path does not exist");            	
                    } else if (!File.Exists (fullPath)) {
                    	throw new InvalidOperationException (
                    	    String.Format ("File:  {0}, does not exist", fullPath)
                    	);
                    }

                    if (!Directory.Exists (localEnclosurePath)) {
                    	Directory.CreateDirectory (localEnclosurePath);
                    }

                    if (File.Exists (tmpLocalPath)) {
                        int lastDot = tmpLocalPath.LastIndexOf (".");
                        
                        if (lastDot == -1) {
                            lastDot = tmpLocalPath.Length-1;
                        }
                        
                        string rep = String.Format (
                            "-{0}", 
                            Guid.NewGuid ().ToString ()
                                           .Replace ("-", String.Empty)
                                           .ToLower ()
                        );
                        
                        tmpLocalPath = tmpLocalPath.Insert (lastDot, rep);
                    }
                
                    File.Move (fullPath, tmpLocalPath);
                    File.SetAttributes (tmpLocalPath, FileAttributes.ReadOnly);
                    
                    try {
                        Directory.Delete (path);
                    } catch {}
                } catch { 
                    last_download_error = FeedDownloadError.DownloadFailed;
                    download_status = FeedDownloadStatus.DownloadFailed;
                    throw;
                }
                
                local_path = tmpLocalPath;
                
                this.url = url;
                this.mimetype = mimeType;
                                
                download_status = FeedDownloadStatus.Downloaded;
                last_download_error = FeedDownloadError.None;                    

                Save ();
            }
        }
    }
}
