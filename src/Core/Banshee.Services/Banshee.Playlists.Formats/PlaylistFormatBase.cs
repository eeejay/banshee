//
// PlaylistFormatBase.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Playlists.Formats
{
    public abstract class PlaylistFormatBase : IPlaylistFormat
    {
        private Dictionary<string, object> attributes = new Dictionary<string, object>();
        private List<Dictionary<string, object>> elements = new List<Dictionary<string, object>>();
        private Uri base_uri = new Uri(Environment.CurrentDirectory);
        private string title = null;
        
        public PlaylistFormatBase()
        {
            attributes = new Dictionary<string, object>();
            elements = new List<Dictionary<string, object>>();
        }
        
        public virtual void Load(Stream stream, bool validateHeader)
        {
            using(StreamReader reader = new StreamReader(stream)) {
                Load(reader, validateHeader);
            }
        }
            
        public abstract void Load(StreamReader reader, bool validateHeader);
        
        public abstract void Save(Stream stream, ITrackModelSource source);
        
        protected virtual Dictionary<string, object> AddElement()
        {
            Dictionary<string, object> element = new Dictionary<string, object>();
            Elements.Add(element);
            return element;
        }
        
        protected virtual Uri ResolveUri(string uri)
        {
            return BaseUri == null ? new Uri(uri) : new Uri(BaseUri, uri);
        }
        
        protected virtual string ExportUri(SafeUri uri)
        {
            if(BaseUri == null) {
                return uri.IsLocalPath ? uri.LocalPath : uri.AbsoluteUri;
            }
            
            // TODO replace with call to Paths.MakeRelativeTo
            string base_uri = uri.IsLocalPath ? BaseUri.LocalPath : BaseUri.AbsoluteUri;
            string relative_uri = uri.IsLocalPath ? uri.LocalPath : uri.AbsoluteUri;
            
            if(relative_uri.StartsWith(base_uri)) {
                relative_uri = relative_uri.Substring(base_uri.Length);
                if(relative_uri[0] == Path.DirectorySeparatorChar) {
                    relative_uri = relative_uri.Substring(1);
                }
            }
            
            return relative_uri;
        }
        
        protected virtual TimeSpan SecondsStringToTimeSpan(string seconds)
        {
            try {
                int parsed_seconds = Int32.Parse(seconds.Trim(), ApplicationContext.InternalCultureInfo.NumberFormat);
                return parsed_seconds < 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(parsed_seconds);
            } catch {
                return TimeSpan.Zero;
            }
        }
        
        public virtual Dictionary<string, object> Attributes { 
            get { return attributes; }
        }
        
        public virtual List<Dictionary<string, object>> Elements { 
            get { return elements; }
        }
        
        public virtual Uri BaseUri {
            get { return base_uri; }
            set {
                string abs_uri = value.AbsoluteUri;
                if (abs_uri[abs_uri.Length - 1] != System.IO.Path.DirectorySeparatorChar) {
                    value = new Uri (abs_uri + System.IO.Path.DirectorySeparatorChar);
                }
                base_uri = value;
            }
        }
        
        public virtual string Title {
            get { return title; }
            set { title = value; }
        }
    }
}
