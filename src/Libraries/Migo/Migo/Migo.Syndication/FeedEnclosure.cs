/*************************************************************************** 
 *  FeedEnclosure.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
using System.IO;

using Migo.Syndication.Data;

namespace Migo.Syndication
{
    public class FeedEnclosure : IFeedEnclosure 
    {
        private bool canceled;
        private bool downloading;
        private bool stopped;
        
        private string downloadMimeType;
        private FEEDS_DOWNLOAD_STATUS downloadStatus;        
        private string downloadUrl;
        private bool active;
        private FeedDownloadError lastDownloadError;
        private long length;
        private long localID;
        private string localPath;
        private FeedItem parent;
        private string type;
        private string url;     
        
        private readonly object sync = new object ();
        
        internal bool Active 
        {
            get { 
                lock (sync) {                
                    return active; 
                }
            }
            
            set {
                lock (sync) {
                    active = value;
                }
            }
        }
       
        public string DownloadMimeType 
        { 
            get {
                lock (sync) { 
                    return downloadMimeType; 
                }
            }
        }

        public FEEDS_DOWNLOAD_STATUS DownloadStatus 
        { 
            get { 
                lock (sync) {
                    return downloadStatus;
                }
            }
            
            internal set { 
                lock (sync) {
                    downloadStatus = value;
                    //Console.WriteLine ("Enclosure:  DownloadStatus:  {0}", downloadStatus);
                    switch (value) {
                    case FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOAD_FAILED: goto case FEEDS_DOWNLOAD_STATUS.FDS_NONE;
                    case FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOADED: 
                        Commit ();
                        goto case FEEDS_DOWNLOAD_STATUS.FDS_NONE;
                    case FEEDS_DOWNLOAD_STATUS.FDS_NONE:
                        ResetDownloading ();
                        break;
                    }
                }
            }             
        }        
        
        public string DownloadUrl 
        { 
            get { lock (sync) { return downloadUrl; } } 
        }

        public FeedDownloadError LastDownloadError
        {
            get { 
                lock (sync) {
                    return lastDownloadError; 
                }
            }
            
            internal set {
                lock (sync) {
                    lastDownloadError = value;                
                }
            }
        }
        
        public long Length 
        { 
            get { lock (sync) { return length; } } 
        }
        
        public long LocalID 
        { 
            get { return localID; }
            internal set { localID = value; }
        }
        
        public string LocalPath 
        { 
            get { lock (sync) { return localPath; } } 
        }
        
        public IFeedItem Parent 
        { 
            get { return parent; } 
            internal set {          
                if (value == null) {
                	throw new ArgumentNullException ("Parent");
                }

                parent = value as FeedItem;
                
                if (parent == null) {
                    throw new ArgumentException (
                        "Parent must be of type FeedItem"
                    );
                }
            }
        }
        
        public string Type 
        { 
            get { lock (sync) { return type; } } 
        }
        
        public string Url 
        { 
            get { lock (sync) { return url; } } 
        }
 
        internal FeedEnclosure (IFeedEnclosureWrapper wrapper) : this (null, wrapper) {}
        internal FeedEnclosure (FeedItem parent, IFeedEnclosureWrapper wrapper)
        {
            if (wrapper == null) {
                throw new ArgumentNullException ("wrapper");            	
            }
            
            active = wrapper.Active;
            localID = wrapper.LocalID;
            downloadMimeType = wrapper.DownloadMimeType;
            downloadUrl = wrapper.DownloadUrl;
            length = wrapper.Length;
            lastDownloadError = wrapper.LastDownloadError;
            localPath = wrapper.LocalPath;
            type = wrapper.Type;
            url = wrapper.Url;    

            if (!String.IsNullOrEmpty (localPath)) {
                downloadStatus = FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOADED;
            }            
            
            this.parent = parent;
        }        

        public void AsyncDownload ()
        {            
            if (SetDownloading ()) {
                if (!parent.QueueDownload (this)) {
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

        private void CancelAsyncDownloadImpl ()      
        {
            parent.CancelDownload (this);            
        }

        public void StopAsyncDownload ()
        {
            if (SetStopped ()) {
                parent.StopDownload (this);
            }
        }
        
        private void CheckActive ()
        {
            if (!active) {
                throw new InvalidOperationException ("Enclosure previously deleted");                    
            }
        }
        
        internal void Commit ()
        {
            if (localID < 0) {
                localID = EnclosuresTableManager.Insert (this);
            } else {
                EnclosuresTableManager.Update (this);
            }
        }
        
        // Deletes file associated with enclosure.
        // Does not cancel an active download like the WRP.
        public void RemoveFile ()
        {
            lock (sync) {                
                CheckActive ();            
                
                if (!String.IsNullOrEmpty (localPath) && File.Exists (localPath)) {
                	try {
                        FileAttributes attributes = 
                                File.GetAttributes (localPath) | FileAttributes.ReadOnly;

                        if (attributes == FileAttributes.ReadOnly) {
                            File.Delete (localPath);	
                        }
                        
                        Directory.Delete (Path.GetDirectoryName (localPath));
                	} catch {}
                }
                
                localPath = String.Empty;
                downloadStatus = FEEDS_DOWNLOAD_STATUS.FDS_NONE;                                
                
                Commit ();
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
                    lastDownloadError = FeedDownloadError.Canceled;
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
                    downloadStatus != FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOADED) {
                    canceled = false;
                    stopped = false;
                    ret = downloading = true;    
                    
                    downloadStatus = FEEDS_DOWNLOAD_STATUS.FDS_PENDING;                    
                    lastDownloadError = FeedDownloadError.None;
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
                    lastDownloadError = FeedDownloadError.Canceled;
                }            
            }
            
            return ret;            
        }

        public void SetFile (string url, string path, string mimeType, string filename)
        {
            
        }
        
        internal void SetFileImpl (string url, string path, string mimeType, string filename)
        {      
            string tmpLocalPath;
            string fullPath = path;
            string localEnclosurePath = parent.Parent.LocalEnclosurePath;
            
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
                    lastDownloadError = FeedDownloadError.DownloadFailed;
                    downloadStatus = FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOAD_FAILED;
                    throw;
                }
                
                localPath = tmpLocalPath;
                
                this.downloadUrl = url;
                this.downloadMimeType = mimeType;
                                
                downloadStatus = FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOADED;
                lastDownloadError = FeedDownloadError.None;                    

                Commit ();
            }
        }
    }
}
