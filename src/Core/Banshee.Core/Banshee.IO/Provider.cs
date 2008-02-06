//
// Provider.cs
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
using System.Reflection;

using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.IO
{
    internal static class Provider
    {
        private static IProvider provider;
        private static IDirectory directory;
        private static IFile file;
        
        private static void LoadProvider ()
        {
            lock (typeof (Provider)) {
                if (provider != null) {
                    return;
                }
                
                provider = new Banshee.IO.SystemIO.Provider ();
                directory = (IDirectory)Activator.CreateInstance (provider.DirectoryProvider);
                file = (IFile)Activator.CreateInstance (provider.FileProvider);
            }
        }
        
        public static IDirectory Directory {
            get { LoadProvider (); return directory; }
        }
        
        public static IFile File {
            get { LoadProvider (); return file; }
        }
        
        public static IDemuxVfs CreateDemuxVfs (string file)
        {
            return (IDemuxVfs)Activator.CreateInstance (provider.DemuxVfsProvider, new object [] { file });
        }
    }
}
