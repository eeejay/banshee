//
// File.cs
//
// Author:
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
using GLib;

using Banshee.Base;

namespace Banshee.IO.Gio
{
    public class File : IFile
    {
        public void Delete (SafeUri uri)
        {
            FileFactory.NewForUri (uri.AbsoluteUri).Delete ();
        }

        public bool Exists (SafeUri uri)
        {
            var file = FileFactory.NewForUri (uri.AbsoluteUri);

            if (!file.Exists) {
                return false;
            }

            var type = file.QueryFileType (FileQueryInfoFlags.None, null);
            return (type & FileType.Regular) != 0 && (type & FileType.Directory) == 0;
        }

        public void Move (SafeUri from, SafeUri to)
        {
            FileFactory.NewForUri (from.AbsoluteUri).Move (FileFactory.NewForUri (to.AbsoluteUri), FileCopyFlags.None, null, null);
        }

        public void Copy (SafeUri from, SafeUri to, bool overwrite)
        {
            FileFactory.NewForUri (from.AbsoluteUri).Move (FileFactory.NewForUri (to.AbsoluteUri), overwrite ? FileCopyFlags.Overwrite : FileCopyFlags.None, null, null);
        }

        public System.IO.Stream OpenRead (SafeUri uri)
        {
            var file = FileFactory.NewForUri (uri.AbsoluteUri);
            return new GioStream (file.Read (null));
        }

        public System.IO.Stream OpenWrite (SafeUri uri, bool overwrite)
        {
            var file = FileFactory.NewForUri (uri.AbsoluteUri);
            return new GioStream (overwrite
                ? file.Replace (null, false, FileCreateFlags.None, null)
                : file.Create (FileCreateFlags.None, null));
        }

        public long GetSize (SafeUri uri)
        {
            try {
                var file = FileFactory.NewForUri (uri.AbsoluteUri);
                var file_info = file.QueryInfo ("standard::size", FileQueryInfoFlags.None, null);
                return file_info.Size;
            } catch {
                return -1;
            }
        }

        public long GetModifiedTime (SafeUri uri)
        {
            var file = FileFactory.NewForUri (uri.AbsoluteUri);
            var file_info = file.QueryInfo ("time::modified", FileQueryInfoFlags.None, null);
            return (long) file_info.GetAttributeULong ("time::modified");
        }
    }
}
