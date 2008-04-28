//
// FeedItem.cs
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
using System.Collections.Generic;

using Hyena;
using Hyena.Data.Sqlite;

using Migo.Syndication.Data;

namespace Migo.Syndication
{
    public class FeedItem
    {
        private static SqliteModelProvider<FeedItem> provider;
        public static SqliteModelProvider<FeedItem> Provider {
            get { return provider; }
            set { provider = value; }
        }

        private bool active;
        private string author;
        private string comments;
        private string description;
        private FeedEnclosure enclosure;
        private string guid;
        private bool isRead;
        private DateTime lastDownloadTime;  
        private string link;
        private long dbid;
        private DateTime modified;     
        private Feed feed;
        private DateTime pubDate;       
        private string title;        
        
        public readonly object sync = new object ();

#region Database-backed Properties
                
        [DatabaseColumn]
        internal bool Active {
            get { lock (sync) { return active; } }
            set { 
                lock (sync) {                
                    if (value != active) {
                        active = value;
                        if (enclosure != null) {
                            enclosure.Active = value;
                        }
                    }
                }
            }
        }
        
        [DatabaseColumn]
        public string Author {
            get { lock (sync) { return author; } }
            set { author = value; }
        }
        
        [DatabaseColumn]
        public string Comments {
            get { lock (sync) { return comments; } } 
            set { comments = value; }
        }
        
        [DatabaseColumn]
        public string Description {
            get { lock (sync) { return description; }} 
            set { description = value; }
        }
        
        [DatabaseColumn]
        public string Guid {
            get { lock (sync) { return guid; } }
            set { guid = value; }
        }

        [DatabaseColumn]
        public bool IsRead {
            get { lock (sync) { return isRead; } }
            set { 
                int delta = 0;                
                
                lock (sync) {                    
                    if (isRead != value) {
                        isRead = value;
                        delta = value ? -1 : 1;    
                        
                        if (feed == null) {
                            return;
                        }
                                
                        Save ();
                    }
                }
                
                if (delta != 0) {
                    feed.UpdateItemCounts (0, delta);                            
                }                                            
            }
        }
        
        [DatabaseColumn]
        public DateTime LastDownloadTime {
            get { lock (sync) { return lastDownloadTime; } } 
            set { lastDownloadTime = value; }
        }  
        
        [DatabaseColumn]
        public string Link {
            get { lock (sync) { return link; } }
            set { link = value; }
        }
        
        [DatabaseColumn ("ItemID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public long DbId {
            get { lock (sync) { return dbid; } }
            internal set { 
                lock (sync) { 
                    dbid = value; 
                }
            }
        }
        
        [DatabaseColumn]
        public DateTime Modified { 
            get { lock (sync) { return modified; } } 
            set { modified = value; }
        }      
        
        [DatabaseColumn]
        public DateTime PubDate {
            get { lock (sync) { return pubDate; } } 
            set { pubDate = value; }
        }       
        
        [DatabaseColumn]
        public string Title {
            get { lock (sync) { return title; } } 
            set { title = value; }
        }

#endregion

        public Feed Feed {
            get { lock (sync) { return feed; } }
            
            internal set {
                if (value == null) {
                    throw new ArgumentNullException ("Feed");
                }
                
                lock (sync) {  
                    feed = value;
                }
            }
        }
        
        private bool enclosure_loaded;
        public void LoadEnclosure ()
        {
            if (!enclosure_loaded && DbId > 0) {
                Console.WriteLine ("Loading item enclosures");
                IEnumerable<FeedEnclosure> enclosures = FeedEnclosure.Provider.FetchAllMatching (String.Format (
                    "{0}.ItemID = {1}", FeedEnclosure.Provider.TableName, DbId
                ));
                
                foreach (FeedEnclosure enclosure in enclosures) {
                    enclosure.Item = this;
                    this.enclosure = enclosure;
                    break; // should only have one
                }
                Console.WriteLine ("Done loading item enclosures");
                enclosure_loaded = true;
            }
        }

        public FeedEnclosure Enclosure {
            get { lock (sync) { return enclosure; } }
            internal set {
                lock (sync) {
                    if (value == null) {
                        throw new ArgumentNullException ("Enclosure");
                    }
                        
                    FeedEnclosure tmp = value as FeedEnclosure;
            
                    if (tmp == null) {
                        throw new ArgumentException (
                            "Enclosure must be derived from 'FeedEnclosure'"
                        );
                    }
                        
                    enclosure = tmp;
                    enclosure.Item = this;
                }
            }            
        }
 
        public FeedItem ()
        {
        }

        public void Save ()
        {
            Provider.Save (this);
            if (enclosure != null)
                enclosure.Save ();
        }
      
        public void Delete ()
        {
            DeleteImpl (true, true);
        }
        
        public void Delete (bool delEncFile)
        {
            DeleteImpl (true, delEncFile);
        }        
        
        internal void DeleteImpl (bool removeFromParent, bool delEncFile)
        {
            lock (sync) {
                if (!active) {
                    return;
                }
                
                if (delEncFile) {
                    if (enclosure != null && delEncFile) {
                        enclosure.RemoveFile ();
                    }                
                }                   
                
                Active = false;
                
                if (removeFromParent) {
                    // TODO should this be Provider.Delete ?
                    //CommitImpl ();    
                }                
            }

            if (removeFromParent) {
                feed.Remove (this);       
            }            
        }
        
        internal void CancelDownload (FeedEnclosure enc)
        {
            if (feed != null) {
                feed.CancelDownload (enc);
            }
        }
        
        internal bool QueueDownload (FeedEnclosure enc) 
        {
            bool queued = false;
        
            if (feed != null) {
                queued = feed.QueueDownload (enc) != null;	
            }
        
            return queued;
        }

        internal void StopDownload (FeedEnclosure enc)
        {
            if (feed != null) {
                feed.StopDownload (enc);
            }
        }
    }
}
