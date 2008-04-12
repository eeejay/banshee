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
using Mono.Addins;

using Hyena;
using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.IO
{
    public static class Provider
    {
        private static IProvider provider;
        private static IDirectory directory;
        private static IFile file;
        
        static Provider () {
            lock (typeof (Provider)) {
                if (provider != null) {
                    return;
                }
                
                foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Banshee/Platform/IOProvider")) {
                    try {
                        if (node.HasId && node.Id == ProviderSchema.Get ()) {
                            provider = (IProvider)node.CreateInstance (typeof (IProvider));
                        }
                    } catch (Exception e) {
                        Log.Warning ("IO provider extension failed to load", e.Message);
                    }
                }
                
                if (provider == null) {
                    provider = new Banshee.IO.SystemIO.Provider ();
                }
                
                Log.DebugFormat ("IO provider extension loaded ({0})", provider.GetType ().FullName);
                
                directory = (IDirectory)Activator.CreateInstance (provider.DirectoryProvider);
                file = (IFile)Activator.CreateInstance (provider.FileProvider);
            }
        }

        public static void SetProvider (IProvider customProvider)
        {
            provider = customProvider;
        }
        
        internal static IDirectory Directory {
            get { return directory; }
        }
        
        internal static IFile File {
            get { return file; }
        }
        
        internal static IDemuxVfs CreateDemuxVfs (string file)
        {
            return (IDemuxVfs)Activator.CreateInstance (provider.DemuxVfsProvider, new object [] { file });
        }
        
        internal static readonly SchemaEntry<string> ProviderSchema = new SchemaEntry<string> (
            "core", "io_provider",
            "Banshee.IO.Unix.Provider",
            "Set the IO provider backend in Banshee",
            "Can be either \"Banshee.IO.SystemIO.Provider\" (.NET System.IO), " + 
                "\"Banshee.IO.Unix.Provider\" (Native Unix/POSIX), or " +
                "\"Banshee.IO.GnomeVfs.Provider\" (GNOME VFS); " +
                "takes effect on Banshee start (restart necessary)"
        );
    }
}
