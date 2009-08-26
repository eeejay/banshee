//
// Directory.cs
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
using System.Linq;
using System.Collections.Generic;

using GLib;

using Hyena;
using Banshee.Base;

namespace Banshee.IO.Gio
{
    public class Directory : IDirectory
    {
        public void Create (string directory)
        {
            var file = FileFactory.NewForPath (directory);
            file.MakeDirectoryWithParents (null);
        }

        public void Delete (string directory)
        {
            Delete (directory, false);
        }

        public void Delete (string directory, bool recursive)
        {
            Delete (directory, recursive, false);
        }

        internal static bool DisableNativeOptimizations = false;

        private void Delete (string directory, bool recursive, bool directoryIsUri)
        {
            var dir = GetDir (directory, directoryIsUri);

            if (!dir.Exists) {
                Console.WriteLine ("{0} doesn't exist", directory);
                return;
            }

            if ((dir.QueryFileType (FileQueryInfoFlags.None, null) & FileType.Directory) == 0) {
                Console.WriteLine ("{0} isn't directory", directory);
                return;
            }

            // If native, use the System.IO recursive delete
            if (dir.IsNative && !DisableNativeOptimizations) {
                System.IO.Directory.Delete (directory, recursive);
                return;
            }

            if (recursive) {
                foreach (string child in GetFiles (dir, false)) {
                    FileFactory.NewForUri (child).Delete ();
                }

                foreach (string child in GetDirectories (dir, false)) {
                    Delete (child, true, true);
                }
            }

            dir.Delete ();
        }

        private GLib.File GetDir (string directory, bool directoryIsUri)
        {
            return directoryIsUri ? FileFactory.NewForUri (directory) : FileFactory.NewForPath (directory);
        }

        public bool Exists (string directory)
        {
            var file = FileFactory.NewForPath (directory);
            if (!file.QueryExists (null))
                return false;

            var type = file.QueryFileType (FileQueryInfoFlags.None, null);
            return (type & FileType.Directory) != 0;
        }

        public IEnumerable<string> GetFiles (string directory)
        {
            return GetFiles (directory, true, false);
        }

        private IEnumerable<string> GetFiles (string directory, bool followSymlinks, bool directoryIsUri)
        {
            return GetFiles (GetDir (directory, directoryIsUri), followSymlinks);
        }

        private IEnumerable<string> GetFiles (GLib.File dir, bool followSymlinks)
        {
            foreach (FileInfo file in dir.EnumerateChildren ("standard::type,standard::name", followSymlinks ? FileQueryInfoFlags.None : FileQueryInfoFlags.NofollowSymlinks, null)) {
                if ((file.FileType & FileType.Regular) != 0) {
                    yield return System.IO.Path.Combine (dir.Uri.AbsoluteUri, file.Name);
                }
            }
        }

        public IEnumerable<string> GetDirectories (string directory)
        {
            return GetDirectories (directory, true, false);
        }

        private IEnumerable<string> GetDirectories (string directory, bool followSymlinks, bool directoryIsUri)
        {
            return GetDirectories (GetDir (directory, directoryIsUri), followSymlinks);
        }

        private IEnumerable<string> GetDirectories (GLib.File dir, bool followSymlinks)
        {
            foreach (FileInfo file in dir.EnumerateChildren ("standard::type,standard::name", followSymlinks ? FileQueryInfoFlags.None : FileQueryInfoFlags.NofollowSymlinks, null)) {
                if ((file.FileType & FileType.Directory) != 0) {
                    yield return System.IO.Path.Combine (dir.Uri.AbsoluteUri, file.Name);
                }
            }
        }

        public void Move (SafeUri from, SafeUri to)
        {
            var dir = FileFactory.NewForUri (from.AbsoluteUri);
            dir.Move (FileFactory.NewForUri (to.AbsoluteUri), FileCopyFlags.None, null, null);
        }
    }
}
