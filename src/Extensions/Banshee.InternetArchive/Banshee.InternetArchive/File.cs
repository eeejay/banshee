//
// File.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;

using Mono.Unix;

using Hyena.Json;

using InternetArchive;
using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class File
    {
        JsonObject file;
        string location_root;

        public File (JsonObject file, string location_root)
        {
            this.file = file;
            this.location_root = location_root;
        }

        private long GetLong (string i)
        {
            return i == null ? 0 : Int64.Parse (i);
        }

        private int GetInt (string i)
        {
            return i == null ? 0 : Int32.Parse (i);
        }

        private TimeSpan GetTimeSpan (string i)
        {
            if (i == null)
                return TimeSpan.Zero;

            int h = 0, m = 0, s = 0;
            var bits = i.Split (':');

            if (bits.Length > 0)
                s = Int32.Parse (bits[bits.Length - 1]);

            if (bits.Length > 1)
                m = Int32.Parse (bits[bits.Length - 2]);

            if (bits.Length > 2)
                h = Int32.Parse (bits[bits.Length - 3]);

            return new TimeSpan (h, m, s);
        }

        public string Location {
            get { return location_root + file.Get<string> ("location"); }
        }

        public long Size {
            get { return GetLong (file.Get<string> ("size")); }
        }

        public int Track {
            get { return GetInt (file.Get<string> ("track")); }
        }

        public string Creator {
            get { return file.Get<string> ("creator"); }
        }

        public string Title {
            get { return file.Get<string> ("title"); }
        }

        public int BitRate {
            get { return GetInt (file.Get<string> ("bitrate")); }
        }

        public string Format {
            get { return file.Get<string> ("format"); }
        }

        public TimeSpan Length {
            get { return GetTimeSpan (file.Get<string> ("length")); }
        }
    }
}
