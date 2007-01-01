/***************************************************************************
 *  SafeUri.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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
using System.Runtime.InteropServices;

namespace Banshee.Base 
{
    public class SafeUri
    {
        private enum LocalPathCheck {
            NotPerformed,
            Yes,
            No
        }
        
        private static int MAX_SCHEME_LENGTH = 6;

        private string uri;
        private string local_path;
        private string scheme;
        private LocalPathCheck local_path_check = LocalPathCheck.NotPerformed;
        
        public SafeUri(string uri)
        {
            int scheme_delimit_index = uri.IndexOf("://");
            if(scheme_delimit_index > 0 && scheme_delimit_index <= MAX_SCHEME_LENGTH) {
                this.uri = uri;
            } else {
                this.uri = FilenameToUri(uri);
            }
        }
        
        public SafeUri(string uri, bool isUri)
        {
            if(isUri) {
                this.uri = uri;
            } else {
                this.uri = FilenameToUri(uri);
            }
        }
        
        public SafeUri(Uri uri)
        {
            this.uri = uri.AbsoluteUri;
        }

        public static string FilenameToUri(string localPath)
        {
            // TODO: replace with managed conversion to avoid marshalling
            IntPtr path_ptr = GLib.Marshaller.StringToPtrGStrdup(localPath);
            IntPtr uri_ptr = g_filename_to_uri(path_ptr, IntPtr.Zero, IntPtr.Zero);
            GLib.Marshaller.Free(path_ptr);

            if(uri_ptr == IntPtr.Zero) {
                throw new ApplicationException("Filename path must be absolute");
            }

            string uri = GLib.Marshaller.Utf8PtrToString(uri_ptr);
            GLib.Marshaller.Free(uri_ptr);
            
            return uri;
        }
        
        public static string UriToFilename(string uri)
        {
            // TODO: replace with managed conversion to avoid marshalling
            IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup(uri);
            IntPtr path_ptr = g_filename_from_uri(uri_ptr, IntPtr.Zero, IntPtr.Zero);
            GLib.Marshaller.Free(uri_ptr);
            
            if(path_ptr == IntPtr.Zero) {
                throw new ApplicationException("URI could not be converted to local file path");
            }
            
            string path = GLib.Marshaller.Utf8PtrToString(path_ptr);
            GLib.Marshaller.Free(path_ptr);
            
            return path;
        }
        
        public static string UriToFilename(SafeUri uri)
        {
            return UriToFilename(uri.AbsoluteUri);
        }
        
        public override string ToString()
        {
            return AbsoluteUri;
        }

        public string AbsoluteUri {
            get { return uri; }
        }
        
        public bool IsLocalPath {
            get {
                if(local_path_check == LocalPathCheck.NotPerformed) {
                    if(Scheme == "file") {
                        local_path_check = LocalPathCheck.Yes;
                        return true;
                    } else {
                        local_path_check = LocalPathCheck.No;
                        return false;
                    }
                }
                
                return local_path_check == LocalPathCheck.Yes;
            }
        }               
        
        public string AbsolutePath {
            get {
                if(local_path == null && IsLocalPath) {
                    local_path = UriToFilename(uri);
                }
                
                return local_path;
            }
        }
        
        public string LocalPath {
            get { return AbsolutePath; }
        }
        
        public string Scheme {
            get {
                if(scheme == null) {
                    scheme = uri.Substring(0, uri.IndexOf("://"));
                }
                
                return scheme;
            }
        }
        
        public bool IsFile {
            get { return Scheme == System.Uri.UriSchemeFile; }
        }
        
        [DllImport("libglib-2.0.so")]
        private static extern IntPtr g_filename_to_uri(IntPtr filename, IntPtr hostname, IntPtr error);

        [DllImport("libglib-2.0.so")]
        private static extern IntPtr g_filename_from_uri(IntPtr uri, IntPtr hostname, IntPtr error);
    }
}
