/***************************************************************************
 *  HttpDownloadTask.cs
 *
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
using System.Net;
using System.Web;
using System.Threading;

using Mono.Gettext;

using Banshee.Plugins.Podcast;

namespace Banshee.Plugins.Podcast.Download
{
    internal class HttpDownloadTask : DownloadTask
    {
        private HttpWebRequest request;
        private HttpWebResponse response;

        public HttpDownloadTask (DownloadInfo dif) : base (dif) {}

        protected override void Download ()
        {
            try
            {
                ImplDownload ();
            }
            catch {}
        }
        
        protected override void SetFilePathFromUri (Uri uri)
        {
            if (uri != null)
            {
                string[] segments = uri.Segments;
		        FilePath = System.Web.HttpUtility.UrlDecode (segments [segments.Length-1]);
            }
        }        

        private void ImplDownload ()
        {
            CheckState ();

            PrepRequest ();
            
            Connect ();

            CheckState ();

            SetFilePathFromUri (response.ResponseUri);
            
            webContentLength = response.ContentLength;
            remote_last_updated = response.LastModified;

            CheckLength ();

            HandleExistingTempFile ();
            HandleExistingFile ();

            if (bytesRead > 0)
            {
                CheckState ();

                Disconnect ();
                PrepRequest (Convert.ToInt32(bytesRead));
                Connect ();

                CheckState ();

                try
                {
                    localFile = new FileStream (TempFilePath, FileMode.Append);
                }
                catch (System.IO.IOException ioe)
                {
                    Stop (DownloadState.Failed);
                    throw new TaskStoppedException (ioe.Message);
                }
            }

            CheckState ();

            try
            {
                if (localFile == null)
                {
                    localFile = new FileStream (TempFilePath, FileMode.Create);

                }
                else if (localFile != null)
                {
                    if (response.ContentLength == localFile.Length)
                    {
                        Stop (DownloadState.Completed);
                        throw new TaskStoppedException (Catalog.GetString("File complete"));
                    }
                }
            }
            catch (System.IO.IOException ioe)
            {
                Stop (DownloadState.Failed);
                throw new TaskStoppedException (ioe.Message);
            }

            CheckState ();

            DoDownload ();
        }

        private void DoDownload ()
        {
            Stream st;

            int nRead = -1;
            long length = (totalLength <= -1 || totalLength > 8192) ? 8192 : totalLength; 
            byte [] buffer = new byte [length];

            try
            {
                st = response.GetResponseStream ();
            }
            catch (Exception e)
            {
                Stop (DownloadState.Failed);
                throw new TaskStoppedException (e.Message);
            }

            while (nRead != 0)
            {
                if (dif.State != DownloadState.Running)
                {
                    break;
                }

                try
                {
                    nRead = st.Read (buffer, 0, (int)length);
                    localFile.Write (buffer, 0, nRead);
                }
                catch (System.IO.IOException ioe)
                {
                    Stop (DownloadState.Failed);
                    throw new TaskStoppedException (ioe.Message);
                }

                bytesRead += nRead;

                OnProgressChanged (nRead);
            }

            lock (dif.SyncRoot)
            {
                if (dif.Active)
                {
                    DetermineTerminalState ();
                }
            }

            Stop ();
        }

        private bool IsError (HttpStatusCode statusCode)
        {
            int status = (int) statusCode;

            if ((status >= 100) && (status <= 399))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // TODO Create a separate HttpRequest sugar class.
        protected virtual void PrepRequest ()
        {
            PrepRequest (0);
        }

        protected virtual void PrepRequest (int range)
        {
            request = (HttpWebRequest) WebRequest.Create (dif.RemoteUri);
            request.UserAgent = "Banshee"; // Should be defined in Banshee.Base.Globals
            request.Timeout = (60 * 1000); // One minute
            request.KeepAlive = false;
            request.AllowAutoRedirect = true;

            if (range > 0) {
                request.AddRange (range);
            }
        }

        protected virtual void Connect ()
        {
            try
            {
                Console.WriteLine (Catalog.GetString("Contacting {0}..."), request.Address.Host.ToString ());

                CheckState ();
                
                try {
                	response = request.GetResponse () as HttpWebResponse;
                } catch {
                    Stop (DownloadState.Failed);
                    throw new TaskStoppedException (Catalog.GetString("HTTP error"));                	
                }
                
                CheckState ();
                
                if (IsError (response.StatusCode))
                {
                    Stop (DownloadState.Failed);
                    throw new TaskStoppedException (Catalog.GetString("HTTP error"));
                }

            }
            catch (WebException we)
            {
                if ((we.Response as HttpWebResponse).StatusCode == 
                        HttpStatusCode.RequestedRangeNotSatisfiable) {
                    
                    CheckState ();

                    Disconnect ();
                    
                    bytesRead = 0;
                    DeleteTempFile ();
                    
                    PrepRequest ();
                    Connect ();

                    CheckState ();                    
                    
                } else {
                    Stop (DownloadState.Failed);
                    throw new TaskStoppedException (we.Message);                
                }
            }
        }

        protected virtual void Disconnect ()
        {
            if (response != null)
            {
                response.Close ();
                response = null;
            }

            request = null;
        }

        protected override void CleanUp ()
        {
            if (localFile != null)
            {
                localFile.Close ();
                localFile = null;
            }

            Disconnect ();
        }
    }
}
