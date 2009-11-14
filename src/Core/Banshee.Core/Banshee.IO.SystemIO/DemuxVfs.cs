//
// DemuxVfs.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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

using Banshee.Base;

namespace Banshee.IO.SystemIO
{
    public class DemuxVfs : Banshee.IO.IDemuxVfs
    {
        private FileInfo file_info;

        public DemuxVfs (string path)
        {
            file_info = new FileInfo (path);
        }

        public void CloseStream (Stream stream)
        {
            stream.Close ();
        }

        public string Name {
            get { return file_info.FullName; }
        }

        public Stream ReadStream {
            get { return file_info.Open (FileMode.Open, FileAccess.Read, FileShare.Read); }
        }

        public Stream WriteStream {
            get { return file_info.Open (FileMode.Open, FileAccess.ReadWrite); }
        }

        public bool IsReadable {
            get {
                try {
                    ReadStream.Close ();
                    return true;
                } catch {
                    return false;
                }
            }
        }

        public bool IsWritable {
            get {
                try {
                    WriteStream.Close ();
                    return true;
                } catch {
                    return false;
                }
            }
        }
    }
}
