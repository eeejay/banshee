/***************************************************************************
 *  MetadataServiceJob.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
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
using System.IO;
using System.Net;
using System.Collections.Generic;

using Banshee.Kernel;
using Banshee.Base;

namespace Banshee.Metadata
{
    public class MetadataServiceJob : IMetadataLookupJob
    {
        private MetadataService service;
        private IBasicTrackInfo track;
        private List<StreamTag> tags = new List<StreamTag>();
        
        protected MetadataServiceJob()
        {
        }
        
        public MetadataServiceJob(MetadataService service, IBasicTrackInfo track)
        {
            this.service = service;
            this.track = track;
        }
    
        public virtual void Run()
        {
            foreach(IMetadataProvider provider in service.Providers) {
                try {
                    IMetadataLookupJob job = provider.CreateJob(track);
                    job.Run();
                    
                    foreach(StreamTag tag in job.ResultTags) {
                        AddTag(tag);
                    }
                } catch(Exception e) {
                    Console.WriteLine(e);
                }
            }
        }
        
        public virtual IBasicTrackInfo Track { 
            get { return track; }
            protected set { track = value; }
        }
        
        public virtual IList<StreamTag> ResultTags { 
            get { return tags; }
        }
        
        protected void AddTag(StreamTag tag)
        {
            tags.Add(tag);
        }

        protected Stream GetHttpStream(Uri uri)
        {
            if(!Globals.Network.Connected) {
                throw new NetworkUnavailableException();
            }
        
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri.AbsoluteUri);
            request.UserAgent = Banshee.Web.Browser.UserAgent;
            request.Timeout = 20 * 1000;
            request.KeepAlive = false;
            request.AllowAutoRedirect = true;
            
            return ((HttpWebResponse)request.GetResponse()).GetResponseStream();
        }
        
        protected void SaveHttpStream(Uri uri, string path)
        {
            using(Stream from_stream = GetHttpStream(uri)) {
                long bytes_read = 0;

                using(FileStream to_stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite)) {
                    byte [] buffer = new byte[8192];
                    int chunk_bytes_read = 0;

                    while((chunk_bytes_read = from_stream.Read(buffer, 0, buffer.Length)) > 0) {
                        to_stream.Write(buffer, 0, chunk_bytes_read);
                        bytes_read += chunk_bytes_read;
                    }
                }
            }
        }
    }
}
