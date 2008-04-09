/*************************************************************************** 
 *  FeedItem.cs
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

using Migo.Syndication.Data;

namespace Migo.Syndication
{
    public class FeedItem : IFeedItem
    {
        private bool active;
        private string author;
        private string comments;
        private string description;
        private FeedEnclosure enclosure;
        private string guid;
        private bool isRead;
        private DateTime lastDownloadTime;  
        private string link;
        private long localID;
        private DateTime modified;     
        private Feed parent;
        private DateTime pubDate;       
        private string title;        
        
        public readonly object sync = new object ();
                
        internal bool Active 
        {
            get { 
                lock (sync) {
                    return active; 
                }
            }
            
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
        
        public string Author 
        { 
            get { 
                lock (sync) {
                    return author; 
                } 
            }
        }
        
        public string Comments 
        { 
            get { 
                lock (sync) {
                    return comments; 
                }
            } 
        }
        
        public string Description 
        { 
            get { 
                lock (sync) {
                    return description; 
                }
            } 
        }
        
        public IFeedEnclosure Enclosure 
        { 
            get { 
                lock (sync) {
                    return enclosure;
                }
            }
            
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
                    enclosure.Parent = this;
                }
            }            
        }

        public string Guid 
        { 
            get { 
                lock (sync) {
                    return guid; 
                } 
            }
        }
        
        public bool IsRead 
        { 
            get { 
                lock (sync) {
                    return isRead;
                }
            } 
            
            set { 
                int delta = 0;                
                
                lock (sync) {                    
                    if (isRead != value) {
                        isRead = value;
                        delta = value ? -1 : 1;    
                        
                        if (parent == null) {
                            return;
                        }
                                
                        CommitImpl ();
                    }
                }
                
                if (delta != 0) {
                    parent.UpdateItemCounts (0, delta);                            
                }                                            
            }
        }
        
        public DateTime LastDownloadTime 
        { 
            get { 
                lock (sync) {
                    return lastDownloadTime;
                }
            } 
        }  
        
        public string Link 
        { 
            get { 
                lock (sync) {
                    return link; 
                } 
            }
        }
        
        public long LocalID 
        { 
            get { 
                lock (sync) {   
                    return localID; 
                }
            }
            
            internal set { 
                lock (sync) { 
                    localID = value; 
                }
            }
        }
        
        public DateTime Modified 
        { 
            get { 
                lock (sync) {
                    return modified;
                }
            } 
        }      
        
        public IFeed Parent 
        { 
            get { lock (sync) { return parent; } }
            
            internal set {
                if (value == null) {
                	throw new ArgumentNullException ("Parent");              	
                }
                
                lock (sync) {  
                    Feed feed = value as Feed;
                    
                    if (feed == null) {
                        throw new ArgumentException ("Parent must be of type 'Feed'");
                    }
                    
                    parent = feed;
                }
            }
        }
        
        public DateTime PubDate 
        { 
            get { lock (sync) { return pubDate; } } 
        }       
        
        public string Title 
        { 
            get { lock (sync) { return title; } } 
        }      

        internal FeedItem (IFeedItemWrapper wrapper) : this (null, wrapper) {}
        internal FeedItem (Feed parent, IFeedItemWrapper wrapper)
        {
            if (wrapper == null) {
                throw new ArgumentNullException ("wrapper");            	
            }    

            this.parent = parent; 
            
            active = wrapper.Active;
            author = wrapper.Author;
            comments = wrapper.Comments;
            description = wrapper.Description;
            guid = wrapper.Guid;
            isRead = wrapper.IsRead;
            lastDownloadTime = wrapper.LastDownloadTime;  
            link = wrapper.Link;
            localID = wrapper.LocalID;
            modified = wrapper.Modified;
            pubDate = wrapper.PubDate;       
            title = wrapper.Title;
            
            if (wrapper.Enclosure != null) {
                CreateEnclosure (wrapper.Enclosure);  
            }            
        }        

        internal void Commit ()
        {
            lock (sync) {
                CommitImpl ();
            }
        }
        
        private void CommitImpl ()
        {
            if (localID < 0) {
                localID = ItemsTableManager.Insert (this);
            } else {
                ItemsTableManager.Update (this);   
            }
                        
            if (enclosure != null) {
                enclosure.Commit ();
            }            
        }
        
        private void CreateEnclosure (IFeedEnclosureWrapper wrapper)
        {   
            enclosure = new FeedEnclosure (this, wrapper);
        }
        
        internal void DBDelete ()
        {
            ItemsTableManager.Delete (this);   
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
                    CommitImpl ();    
                }                
            }

            if (removeFromParent) {
                parent.Remove (this);       
            }            
        }
        
        internal void CancelDownload (FeedEnclosure enc)
        {
            if (parent != null) {
                parent.CancelDownload (enc);
            }
        }
        
        internal bool QueueDownload (FeedEnclosure enc) 
        {
            bool queued = false;
        
            if (parent != null) {
                queued = parent.QueueDownload (enc) != null;	
            }
        
            return queued;
        }

        internal void StopDownload (FeedEnclosure enc)
        {
            if (parent != null) {
                parent.StopDownload (enc);
            }
        }
    }
}
