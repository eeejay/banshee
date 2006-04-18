/***************************************************************************
 *  Unix.cs
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
using System.Collections;

using Mono.Unix;
using Mono.Unix.Native;

using Banshee.Base;

namespace Banshee.IO.Unix
{
    public class IOConfig : IIOConfig
    {
        public string Name           { get { return "unix";            } }
        public Type FileBackend      { get { return typeof(File);      } }
        public Type DirectoryBackend { get { return typeof(Directory); } }
    }

    internal struct FileStat
    {
        private Stat buf;
        private bool is_directory;
        private bool is_regular_file;
        
        internal FileStat(string path)
        {
            buf = new Stat();
            is_directory = is_regular_file = Syscall.stat(path, out buf) == 0;
            is_regular_file &= (buf.st_mode & FilePermissions.S_IFREG) == FilePermissions.S_IFREG;
            is_directory &= (buf.st_mode & FilePermissions.S_IFDIR) == FilePermissions.S_IFDIR;
            // FIXME: workaround for http://bugzilla.ximian.com/show_bug.cgi?id=76966
            is_directory &= ! ((buf.st_mode & FilePermissions.S_IFSOCK) == FilePermissions.S_IFSOCK);
        }
        
        internal bool IsDirectory {
            get { return is_directory; }
        }
        
        internal bool IsRegularFile {
            get { return is_regular_file; }
        }
    }

    public class File : IFile
    {
        public bool Exists(string path)
        {
            FileStat stat = new FileStat(path);
            return stat.IsRegularFile && !stat.IsDirectory;
        }
    }

    public class Directory : IDirectory
    {
        public void Create(string directory)
        {
            UnixDirectoryInfo unix_dir = new UnixDirectoryInfo(directory);
            unix_dir.Create();
        }
        
        public void Delete(string directory)
        {
            Delete(directory, false);
        }
        
        public void Delete(string directory, bool recursive)
        {
            UnixDirectoryInfo unix_dir = new UnixDirectoryInfo(directory);
            unix_dir.Delete(recursive);
        }
        
        public bool Exists(string directory)
        {
            FileStat stat = new FileStat(directory);
            return stat.IsDirectory;
        }
        
        public IEnumerable GetFiles(string directory)
        {
            UnixDirectoryInfo unix_dir = new UnixDirectoryInfo(directory);
            foreach(UnixFileSystemInfo entry in unix_dir.GetFileSystemEntries()) {
                if(!entry.IsDirectory && entry.IsRegularFile && !entry.IsSocket && entry.Exists) {
                    yield return entry.FullName;
                }
            }
        }
        
        public IEnumerable GetDirectories(string directory)
        {
            UnixDirectoryInfo unix_dir = new UnixDirectoryInfo(directory);
            foreach(UnixFileSystemInfo entry in unix_dir.GetFileSystemEntries()) {
                if(entry.IsDirectory && entry.Exists && !entry.IsSocket) {
                    yield return entry.FullName;
                }
            }
        }
    }
}
