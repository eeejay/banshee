// 
// HttpRequest.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
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
using System.Net;
using System.Collections.Generic;

using ICSharpCode.SharpZipLib.GZip;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Networking;

namespace Banshee.Web
{   
    public class HttpRequest : IDisposable
    {
        private HttpWebRequest request;
        private HttpWebResponse response;
        private List<string> ignore_mimetypes;
        
        public HttpRequest ()            { }
        public HttpRequest (string uri)  { CreateRequest (uri); }
        public HttpRequest (SafeUri uri) { CreateRequest (uri); }
        public HttpRequest (Uri uri)     { CreateRequest (uri); }
        
        public void Dispose ()
        {
            lock (this) {
                if (response != null) {
                    response.Close ();
                    response = null;
                }
                
                request = null;
            }
        }
        
        public void CreateRequest (string uri)
        {
            CreateRequest (new Uri (uri));
        }
        
        public void CreateRequest (SafeUri uri)
        {
            CreateRequest (new Uri (uri.AbsoluteUri));
        }
        
        public virtual void CreateRequest (Uri uri)
        {
            lock (this) {
                Dispose ();
                
                request = (HttpWebRequest)WebRequest.Create (uri.AbsoluteUri);
                request.UserAgent = Browser.UserAgent;
                request.Timeout = (int)Timeout.TotalMilliseconds;
                request.KeepAlive = false;
                request.AllowAutoRedirect = true;
            }
        }
        
        public virtual void GetResponse ()
        {
            lock (this) {
                if (response != null) {
                    return;
                }
                
                if (request == null) {
                    throw new InvalidOperationException ("CreateRequest must be called first");
                } else if (!InternetConnected) {
                    throw new NetworkUnavailableException ();
                }
                
                response = (HttpWebResponse)request.GetResponse ();
                if (ignore_mimetypes == null) {
                    return;
                }
                
                string [] content_types = response.Headers.GetValues ("Content-Type");
                if (content_types != null && content_types.Length > 0) {
                    foreach (string content_type in content_types) {
                        if (ignore_mimetypes.Contains (content_type)) {
                            response.Close ();
                            response = null;
                        }
                    }
                }
            }
        }
        
        public void DumpResponseStream ()
        {
            using (Stream stream = GetResponseStream ()) {
                StreamReader reader = new StreamReader (stream);
                Console.WriteLine (reader.ReadToEnd ());
                reader.Dispose ();
            }
        }
        
        public void SaveResponseStream (SafeUri path)
        {
            SaveResponseStream (path, true);
        }
        
        public void SaveResponseStream (SafeUri path, bool closeResponse)
        {
            SaveResponseStream (Banshee.IO.File.OpenWrite (path, true), closeResponse);
        }
        
        public virtual void SaveResponseStream (Stream toStream, bool closeResponse)
        {
            if (response == null) {
                throw new InvalidOperationException ("No response");
            }
        
            Stream from_stream = response.GetResponseStream ();
            if (from_stream == null) {
                if (response != null && closeResponse) {
                    response.Close ();
                }
                
                throw new InvalidDataException ("Response has no content stream");
            }
            
            Banshee.IO.StreamAssist.Save (from_stream, toStream);
            
            from_stream.Close ();
            if (closeResponse) {
                response.Close ();
            }
        }

        public HttpWebRequest Request {
            get { return request; }
        }
        
        public HttpWebResponse Response {
            get { return response; }
        }

        public Stream GetResponseStream ()
        {
            return response.ContentEncoding == "gzip"
                ? new GZipInputStream (response.GetResponseStream ())
                : response.GetResponseStream ();
        }

        private string response_body;
        public string ResponseBody {
            get {
                if (response_body == null) {
                    GetResponse ();
                    using (Stream stream = GetResponseStream ()) {
                        StreamReader reader = new StreamReader (stream);
                        response_body = reader.ReadToEnd ();
                        reader.Dispose ();
                    }
                }

                return response_body;
            }
        }
        
        private static TimeSpan default_timeout = TimeSpan.FromSeconds (20);
        protected virtual TimeSpan Timeout {
            get { return default_timeout; }
        }
        
#region Mimetypes
        
        public void AddIgnoreMimeType (string mimetype)
        {
            lock (this) {
                if (ignore_mimetypes == null) {
                    ignore_mimetypes = new List<string> ();
                }
                
                ignore_mimetypes.Add (mimetype);
            }
        }
        
        public void RemoveIgnoreMimeType (string mimetype)
        {
             lock (this) {
                if (ignore_mimetypes != null) {
                    ignore_mimetypes.Remove (mimetype);
                }
            }
        }
        
        public void ClearIgnoreMimeTypes ()
        {
            lock (this) {
                if (ignore_mimetypes != null) {
                    ignore_mimetypes.Clear ();
                }
            }
        }
        
        public string [] IgnoreMimeTypes {
            get { lock (this) { return ignore_mimetypes == null ? new string[0] : ignore_mimetypes.ToArray (); } }
            set { lock (this) { ignore_mimetypes = new List<string> (value); } }
        }
        
#endregion
        
        protected bool InternetConnected {
            get { return ServiceManager.Get<Network> ().Connected; }
        }
    }
}
