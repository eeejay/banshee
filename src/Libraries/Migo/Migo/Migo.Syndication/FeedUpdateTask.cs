/***************************************************************************
 *  FeedUpdateTask.cs
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
using System.Threading;
using System.ComponentModel;

using Migo.Net;
using Migo.TaskCore;

namespace Migo.Syndication
{
	public class FeedUpdateTask : Task, IDisposable
	{
        private Feed feed;
	    private bool disposed;	 
	    private ManualResetEvent mre;

        public Feed Feed 
        {
            get { return feed; }
        }

	    public override WaitHandle WaitHandle 
	    {
	        get {
	            lock (SyncRoot) {
	                if (mre == null) {
                        mre = new ManualResetEvent (true);
	                }
	                
	                return mre;
	            }
	        }
	    }
	    
	    public FeedUpdateTask (Feed feed)
	    {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            }

            this.feed = feed;
            this.Name = feed.Link;
            feed.FeedDownloadCompleted += OnFeedDownloadCompletedHandler;
	    }

        public override void CancelAsync ()
	    { 
	        //Console.WriteLine ("CancelAsync - {0} - FeedUpdateTask - 000", IsCompleted);
            lock (SyncRoot) {	
                if (!feed.CancelAsyncDownload () && !IsCompleted) {
                	//Console.WriteLine ("CancelAsync - FeedUpdateTask - 001");
                    EmitCompletionEvents (FEEDS_DOWNLOAD_ERROR.FDE_CANCELED);                
                }
            }
	        //Console.WriteLine ("CancelAsync - FeedUpdateTask - 002");            
	    }
    
        public void Dispose ()
        {
            lock (SyncRoot) {
                if (!disposed) {
                    if (mre != null) {
                        mre.Close ();
                        mre = null;                    
                    }
                    
                    disposed = true;
                }   
            }
        }

	    public override void ExecuteAsync ()
	    {
	        lock (SyncRoot) {
	            SetStatus (TaskStatus.Running);
                
                if (mre != null) {
                    mre.Reset ();                    
                }	            
            }

            feed.AsyncDownloadImpl ();
	    }
        
        private void OnFeedDownloadCompletedHandler (object sender, 
                                                     FeedDownloadCompletedEventArgs e)
        {
            lock (SyncRoot) {
                EmitCompletionEvents (e.Error);
	        }            
        }
        
        private void EmitCompletionEvents (FEEDS_DOWNLOAD_ERROR err)
        {
            feed.FeedDownloadCompleted -= OnFeedDownloadCompletedHandler;

            switch (err) {                        
                case FEEDS_DOWNLOAD_ERROR.FDE_NONE:
                    SetStatus (TaskStatus.Succeeded);
                    break;
                case FEEDS_DOWNLOAD_ERROR.FDE_CANCELED:
                    SetStatus (TaskStatus.Cancelled);
                    break;
                default:
                    SetStatus (TaskStatus.Failed);
                    break;
            }
            
            OnTaskCompleted (null, (Status == TaskStatus.Cancelled));

            if (mre != null) {
                mre.Set ();   
            }                 
        }
	}
}
