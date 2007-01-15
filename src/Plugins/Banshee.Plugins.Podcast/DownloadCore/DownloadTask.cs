/***************************************************************************
 *  DownloadTask.cs
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
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using Mono.Gettext;

using Banshee.Base;
using Banshee.Plugins.Podcast;

namespace Banshee.Plugins.Podcast.Download
{
    public delegate void DownloadLengthChangedEventHandler (object sender,
            DownloadLengthChangedEventArgs args);
    public delegate void DownloadMimeTypeChangedEventHandler (object sender,
            DownloadMimeTypeChangedEventArgs args);
    public delegate void DownloadFilePathChangedEventHandler (object sender,
            DownloadFilePathChangedEventArgs args);

    public class DownloadLengthChangedEventArgs : DownloadEventArgs
    {
        private readonly long bytes_read;
        private readonly long current_length;
        private readonly long previous_length;

        public long BytesRead { get
                                { return bytes_read; } }
        public long CurrentLength { get
                                    { return current_length; } }
        public long PreviousLength { get
                                     { return previous_length; } }

        public DownloadLengthChangedEventArgs (DownloadInfo downloadInfo,
                                               long previousLength, long currentLength) : base (downloadInfo)
        {
            previous_length = previousLength;
            current_length = currentLength;
        }
    }

    public class DownloadMimeTypeChangedEventArgs : DownloadEventArgs
    {
        private readonly string new_mime_type;
        private readonly string previous_mime_type;

        public string NewMimeType { get
                                    { return new_mime_type; } }
        public string PreviousMimeType { get
                                         { return previous_mime_type; } }

        public DownloadMimeTypeChangedEventArgs (DownloadInfo downloadInfo,
                string previousMimeType, string newMimeType) : base (downloadInfo)
        {
            new_mime_type = newMimeType;
            previous_mime_type = previousMimeType;
        }
    }

    public class DownloadFilePathChangedEventArgs : DownloadEventArgs
    {
        private readonly string new_file_path;
        private readonly string previous_file_path;

        public string NewMimeType { get { return new_file_path; } }
        public string PreviousMimeType { get { return previous_file_path; } }

        public DownloadFilePathChangedEventArgs (DownloadInfo downloadInfo,
                string previousFilePath, string newFilePath) : base (downloadInfo)
        {
            new_file_path = newFilePath;
            previous_file_path = previousFilePath;
        }
    }

    public abstract class DownloadTask
    {
        private string filePath;
        private string tempFilePath;

        protected FileStream localFile;
        protected DownloadInfo dif = null;
        protected Thread downloadThread = null;
        protected DateTime remote_last_updated;

        protected long bytesRead = 0;
        protected long totalLength = 0;
        protected long webContentLength = 0;

    protected class TaskStoppedException : Exception
    {
        public TaskStoppedException (string message) : base (message) {}}

        private readonly object file_path_sync = new Object ();

        public string FilePath {
            get
            {
                return filePath;
            }

            // bit of a misgnomer, only appends the file name 
            // to the directory path.  Should be updated for clarity.
            protected set
            {
                lock (file_path_sync)
                {
                    filePath = dif.DirectoryPath + value;
                    dif.LocalPath = filePath;
                    
                    MD5 hasher = MD5.Create ();
                    byte[] hash = hasher.ComputeHash (
                        Encoding.UTF8.GetBytes (dif.UniqueKey)
                    );
                    
                    string hashString = String.Format (
                        "-{0}", 
                        BitConverter.ToString (hash).Replace ("-", String.Empty)
                    );
			                                    
                    tempFilePath = TempFileDir +
                        Path.DirectorySeparatorChar +
                        dif.RemoteUri.Host +
                        Path.DirectorySeparatorChar +
			            value + hashString
			            + Util.Defines.TMP_EXT;
                    CreateTempDirectory ();
                }
            }
        }
        
        protected static string TempFileDir {
            get 
            {
                string tempPath = Paths.TempDir + 
                    System.IO.Path.DirectorySeparatorChar + 
                    "downloads";
 
                return tempPath;
            }
        }

        protected string TempFilePath {
            get
            {
                lock (file_path_sync)
                {
                    return tempFilePath;
                }
            }
        }

        public event DownloadEventHandler Started;
        public event DownloadEventHandler Stopped;
        public event DownloadEventHandler Finished;

        public event DownloadLengthChangedEventHandler LengthChanged;
        public event DownloadFilePathChangedEventHandler FilePathChanged;
        public event DownloadMimeTypeChangedEventHandler MimeTypeChanged;
        public event DiscreteDownloadProgressEventHandler ProgressChanged;

        public DownloadInfo DownloadInfo { get
                                           { return dif; } }

        public long Length {
            get
            { return totalLength; }
        }

        public double Progress {
            get
            {
                if (bytesRead == 0 || totalLength == 0)
                {
                    return 0;
                }
                else
                {
                    int progress = Convert.ToInt32 ((bytesRead*100)/totalLength); 
     
                    return (progress > (-1)) ? progress : 0;
                }
            }
        }

        public long BytesReceived {
            get
            { return bytesRead; }
        }

        public System.Threading.Thread Thread {
            get
            { return downloadThread; }
        }

        public static DownloadTask Create (DownloadInfo dif)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            DownloadTask dt = null;

            if (dif.RemoteUri.Scheme == Uri.UriSchemeHttp ||
                    dif.RemoteUri.Scheme == Uri.UriSchemeHttps)
            {
                dt = new HttpDownloadTask (dif);
            }
            else
            {
                throw new NotSupportedException (Catalog.GetString("Uri scheme not supported"));
            }

            return dt;
        }

        protected internal DownloadTask (DownloadInfo dif)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            this.dif = dif;
            totalLength = dif.Length;
        }

        public virtual void Execute ()
        {
            OnStart ();

            downloadThread = new Thread (new ThreadStart(Download));
            downloadThread.IsBackground = true;

            downloadThread.Start ();
        }

    // TODO let users decide what action to take
    protected virtual void HandleExistingFile ()
    {
        lock (file_path_sync) {
            if (File.Exists (FilePath))
            {
                long length = Util.FileLength (FilePath);

                if (length == totalLength)
                {
                    bytesRead = length;
                    OnProgressChanged (length);

                    Stop (DownloadState.Completed);
                    throw new TaskStoppedException (Catalog.GetString("File complete"));
                }
                else
                {
                    int index = filePath.LastIndexOf (".");
                    string guid = String.Format (
                        "-{0}", Guid.NewGuid ().ToString ()
                    );
                    
                    if (index != -1) {
                        filePath = filePath.Insert (index, guid);     
                    } else {
                        filePath += guid;
                    }

                    dif.LocalPath = filePath;

                    //DeleteExistingFile ();
                }
            }
        }
    }

    protected virtual void HandleExistingTempFile ()
        {
            lock (file_path_sync) {        
                if (File.Exists (TempFilePath))
                {
                    long length = Util.FileLength (TempFilePath);

                    if (length > totalLength ||
                            remote_last_updated.ToLocalTime () > File.GetLastWriteTime (TempFilePath))
                    {
                        try
                        {
                            DeleteTempFile ();
                        } catch {}

                        return;
                    }

                    bytesRead = length;
                    OnProgressChanged (length);

                    if (length == totalLength)
                    {
                        Stop (DownloadState.Completed);
                        throw new TaskStoppedException (Catalog.GetString("File complete"));
                    }
                }
            }
        }

        protected static bool CreateDirectory (string path)
        {
            if (File.Exists (path)) {
                return false;
            }

            try
            {
                Directory.CreateDirectory (path);
            }
            catch {
                throw new TaskStoppedException (
                    String.Format (Catalog.GetString("Unable to create directory:  {0}"), path)
                );
            }
                
            return true;
        }

	    protected static bool DeleteDirectory (string path)
        {
            try {
                Directory.Delete (path);
            } catch { return false; }
            
            return true;
        }

        protected virtual bool CreateDirectory ()
        {
            return CreateDirectory (Path.GetDirectoryName (FilePath));
        }

        protected virtual bool CreateTempDirectory ()
        {
            return CreateDirectory (Path.GetDirectoryName (TempFilePath));
        }

        protected virtual bool DeleteDirectory ()
        {
            return DeleteDirectory (Path.GetDirectoryName (FilePath));
        }

        protected virtual bool DeleteTempDirectory ()
        {
            return DeleteDirectory (Path.GetDirectoryName (TempFilePath));
        }

        protected virtual void DeleteTempFile ()
        {
            try 
            {
                File.Delete (TempFilePath);
                DeleteTempDirectory ();
            } catch {}
        }        

        protected virtual void DeleteExistingFile ()
        {
            try 
            {        
                File.Delete (FilePath);
                DeleteDirectory ();
            } catch {}
        }
        
        protected virtual void CheckLength ()
        {        	
            if (totalLength != webContentLength)
            {

                long previousLength = totalLength;

                dif.Length = webContentLength;
                totalLength = webContentLength;

                OnLengthChanged (dif, previousLength, webContentLength);
            }
        }

        protected virtual void DetermineTerminalState ()
        {
            if (dif.State == DownloadState.Queued ||
                dif.State == DownloadState.CancelRequested ||
                !dif.Active)
            {
                return;
            }
            else if (bytesRead == 0)
            {
                dif.State = DownloadState.Failed;
            }
            else if (bytesRead == totalLength || totalLength == -1)
            {
                dif.State = DownloadState.Completed;
            }
        }

        protected virtual void Stop (DownloadState state)
        {
            dif.State = state;
            CleanUp ();
            OnStop ();
        }

        protected virtual void Stop ()
        {
            DetermineTerminalState ();
            CleanUp ();
            OnStop ();
        }

        protected virtual void CheckState ()
        {
            if (dif.State != DownloadState.Running)
            {
                Stop ();
                throw new TaskStoppedException (Catalog.GetString("Dif is not in 'running' state"));
            }
        }

        protected abstract void CleanUp ();
        protected abstract void Download ();

        protected virtual void SetFilePathFromUri (Uri uri)
        {
            if (uri != null)
            {
                string[] segments = uri.Segments;
		        FilePath = segments [segments.Length-1];
            }
        }

        protected virtual void OnStart ()
        {
            DownloadEventHandler handler = Started;

            if (handler != null)
            {
                handler (this, new DownloadEventArgs (dif));
            }
        }

        protected virtual void OnStop ()
        {
            if (dif.State == DownloadState.CancelRequested)
            {
                dif.State = DownloadState.Canceled;

                try
                {
                    if (File.Exists (TempFilePath))
                    {
                        File.Delete (TempFilePath);
                        DeleteTempDirectory ();
                        DeleteDirectory ();
                    }
                }
                catch {}
            }
    
            else if (dif.State == DownloadState.Completed)
            {
                try
                {
                    if (File.Exists (TempFilePath))
                    {
                        CreateDirectory ();
                        HandleExistingFile ();
                        File.Move (TempFilePath, FilePath);
                        DeleteTempDirectory ();
                    }
                }
                catch {}

            }
            else if (dif.State == DownloadState.Queued)
            {
                dif.State = DownloadState.Ready;
            }

            DownloadEventHandler handler = Stopped;

            if (handler != null)
            {
                handler (this, new DownloadEventArgs (dif));
            }

            if (!dif.Active)
            {
                OnFinished ();
            }
        }

        protected virtual void OnProgressChanged (long bytesThisInterval)
        {
            DiscreteDownloadProgressEventHandler handler = ProgressChanged;

            if (handler != null)
            {
                DiscreteDownloadProgressEventArgs args = new DiscreteDownloadProgressEventArgs (
                            Convert.ToInt32(Progress), dif, bytesRead, totalLength, bytesThisInterval
                        );

                handler (this, args);
            }
        }

        protected virtual void OnFinished ()
        {
            DownloadEventHandler handler = Finished;

            if (handler != null)
            {
                handler (this, new DownloadEventArgs (dif));
            }
        }

        protected virtual void OnLengthChanged (DownloadInfo dif,
                                                long previousLength,
                                                long currentLength)
        {
            DownloadLengthChangedEventHandler handler = LengthChanged;

            if (handler != null)
            {
                handler (this, new DownloadLengthChangedEventArgs (dif, previousLength,
                         currentLength));
            }
        }

        protected virtual void OnMimeTypeChanged ( DownloadInfo downloadInfo,
                string previousMimeType,
                string newMimeType)
        {
            DownloadMimeTypeChangedEventHandler handler = MimeTypeChanged;

            if (handler != null)
            {
                handler (this, new DownloadMimeTypeChangedEventArgs (downloadInfo,
                         previousMimeType, newMimeType));
            }
        }

        protected virtual void OnFilePathChanged ( DownloadInfo downloadInfo,
                string previousFilePath,
                string newFilePath)
        {
            DownloadFilePathChangedEventHandler handler = FilePathChanged;

            if (handler != null)
            {
                handler (this, new DownloadFilePathChangedEventArgs (downloadInfo,
                         previousFilePath, previousFilePath));
            }
        }
    }
}
