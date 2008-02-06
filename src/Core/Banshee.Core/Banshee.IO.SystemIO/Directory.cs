//
// Directory.cs
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
using System.Collections.Generic;

using Banshee.Base;

namespace Banshee.IO.SystemIO
{
    public class Directory : Banshee.IO.IDirectory
    {
        public void Create (string directory)
        {
            System.IO.Directory.CreateDirectory (directory);
        }
        
        public void Delete (string directory)
        {
            Delete (directory, false);
        }
        
        public void Delete (string directory, bool recursive)
        {
            System.IO.Directory.Delete (directory, recursive);
        }
        
        public bool Exists (string directory)
        {
            return System.IO.Directory.Exists (directory);
        }
        
        public IEnumerable<string> GetFiles (string directory)
        {
            return System.IO.Directory.GetFiles (directory);
        }
        
        public IEnumerable<string> GetDirectories (string directory)
        {
            return System.IO.Directory.GetDirectories (directory);
        }

        public void Move (SafeUri from, SafeUri to)
        {
            System.IO.Directory.Move (from.LocalPath, to.LocalPath);
        }
    }
}
