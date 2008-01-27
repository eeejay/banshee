/***************************************************************************
 *  DownloadInfo.cs
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

namespace Banshee.Plugins.Podcast.Download
{
    public class DownloadInfo
    {
        private long _length;
        private bool _active;
        private string mime_type;
        private string local_path;
        private DownloadState _state;

        private readonly Uri remote_uri;
        private readonly string unique_key;
        private readonly string directory_path;

        private readonly object _syncRoot = new object ();

        public string DirectoryPath { get
                                      { return directory_path; } }
        public string LocalPath {
            get
            { return local_path; }
            internal set
            { local_path = value; }
        }

        public Uri RemoteUri { get
                               { return remote_uri; } }

        public long Length {
            get
            {
                return _length;
            }

            internal set
            {
                _length = value;
            }
        }

        public string MimeType {
            get
            {
                return mime_type;
            }

            internal set
            {
                mime_type = value;
            }
        }

        public string UniqueKey {
            get
            {
                return unique_key;
            }
        }

        public DownloadState State {
            get
            {
                lock (_syncRoot)
                { return _state; }
            }

            internal set
            {
                lock (_syncRoot)
                {

                    if (_state == value || !_active)
                    {
                        return;
                    }

                    _state = value;

                    switch (_state)
                    {
                        case DownloadState.Canceled:
                        case DownloadState.Completed:
                        case DownloadState.Failed:
                            _active = false;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        internal bool Active { get
                                   { lock (_syncRoot)
                                   { return _active; } } }
        internal object SyncRoot { get
                                   { return _syncRoot; } }

        internal DownloadInfo (string uri, string path, long length, string mimeType) :
                this (uri, path, length)
        {
            mime_type = mimeType;
        }

        internal DownloadInfo (string uri, string path, long length)
        {
            if (path.LastIndexOf (System.IO.Path.DirectorySeparatorChar) != (path.Length-1))
            {
                path += System.IO.Path.DirectorySeparatorChar;
            }

            directory_path = path;
            _length = length;
            remote_uri = new Uri (uri);

            unique_key = String.Format (
                             "{0}-{1}",
                             path,
                             remote_uri
                         );

            _active = true;
            local_path = null;
            _state = DownloadState.New;
        }
    }
}

