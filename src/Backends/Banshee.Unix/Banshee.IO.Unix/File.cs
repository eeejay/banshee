//
// File.cs
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
using Mono.Unix;
using Mono.Unix.Native;

using Banshee.Base;

namespace Banshee.IO.Unix
{
    internal struct FileStat
    {
        private Stat buf;
        private bool is_directory;
        private bool is_regular_file;
        private long mtime;
        
        internal FileStat (string path)
        {
            buf = new Stat ();
            is_directory = is_regular_file = Syscall.stat (path, out buf) == 0;
            is_regular_file &= (buf.st_mode & FilePermissions.S_IFREG) == FilePermissions.S_IFREG;
            is_directory &= (buf.st_mode & FilePermissions.S_IFDIR) == FilePermissions.S_IFDIR;
            // FIXME: workaround for http://bugzilla.ximian.com/show_bug.cgi?id=76966
            is_directory &= ! ((buf.st_mode & FilePermissions.S_IFSOCK) == FilePermissions.S_IFSOCK);
            mtime = buf.st_mtime;
        }
        
        internal bool IsDirectory {
            get { return is_directory; }
        }
        
        internal bool IsRegularFile {
            get { return is_regular_file; }
        }

        internal long MTime {
            get { return mtime; }
        }
    }

    public class File : IFile
    {
        public void Delete (SafeUri uri)
        {
            UnixFileInfo info = new UnixFileInfo (uri.LocalPath);
            info.Delete ();
        }

        public bool Exists (SafeUri uri)
        {
            FileStat stat = new FileStat (uri.LocalPath);
            return stat.IsRegularFile && !stat.IsDirectory;
        }

        public void Move (SafeUri from, SafeUri to)
        {
            Mono.Unix.Native.Stdlib.rename (from.LocalPath, to.LocalPath);
        }

        public void Copy (SafeUri from, SafeUri to, bool overwrite)
        {
            System.IO.File.Copy (from.LocalPath, to.LocalPath, overwrite);
        }
        
        public Stream OpenRead (SafeUri uri)
        {
            return new UnixFileInfo (uri.LocalPath).OpenRead ();
        }
        
        public Stream OpenWrite (SafeUri uri, bool overwrite)
        {
            return overwrite 
                ? new UnixFileInfo (uri.LocalPath).Open (FileMode.Create, FileAccess.ReadWrite, FilePermissions.DEFFILEMODE)
                : new UnixFileInfo (uri.LocalPath).OpenWrite ();
        }
        
        public long GetSize (SafeUri uri)
        {
            try {
                Mono.Unix.Native.Stat stat;
                Mono.Unix.Native.Syscall.lstat (uri.LocalPath, out stat);
                return stat.st_size;
            } catch {
                return -1;
            }
        }

        public long GetModifiedTime (SafeUri uri)
        {
            FileStat stat = new FileStat (uri.LocalPath);
            return stat.MTime;
        }
    }
}
