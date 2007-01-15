/***************************************************************************
 *  IOProxy.cs
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
using System.Reflection;

using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.IO
{
    public static class IOProxy
    {
        private static IDirectory directory;
        private static IFile file;
        private static IIOConfig config;
        
        private static Type [] available_config_types = new Type [] {
            typeof(Banshee.IO.SystemIO.IOConfig),
            typeof(Banshee.IO.Unix.IOConfig),
            typeof(Banshee.IO.GnomeVfs.IOConfig)
        };
        
        static IOProxy()
        {
            Reload();
        }
        
        public static void SetFromConfig(IIOConfig _config)
        {
            config = _config;
            Console.WriteLine("Setting IO Backend to {0} ({1})", config.GetType(), config.Name);
            
            file = (IFile)Activator.CreateInstance(config.FileBackend);
            directory = (IDirectory)Activator.CreateInstance(config.DirectoryBackend);
            TagLib.File.SetFileAbstractionCreator(TagLibVfsCreator);
        }
        
        private static TagLib.File.IFileAbstraction TagLibVfsCreator(string file)
        {
            return (IDemuxVfs)Activator.CreateInstance(config.DemuxVfsBackend, new object [] { file });
        }
        
        public static void Reload()
        {
            string name = IOBackendSchema.Get();
            
            foreach(Type type in available_config_types) {
                try {
                    IIOConfig config = (IIOConfig)Activator.CreateInstance(type);
                    if(config.Name == name) {
                       SetFromConfig(config);
                       return;
                   }
                } catch {
                }
            }
        }
        
        public static IDirectory Directory {
            get { return directory; }
        }
        
        public static IFile File {
            get { return file; }
        }
        
        public static string DetectMimeType(SafeUri uri)
        {
            return config.DetectMimeType(uri);
        }
        
        public static readonly SchemaEntry<string> IOBackendSchema = new SchemaEntry<string>(
            "core", "io_backend",
            "unix",
            "Set the IO backend in Banshee",
            "Can be either \"systemio\" (.NET System.IO), \"unix\" (Native Unix), or " + 
                "\"gnomevfs\" (GNOME VFS); takes effect on Banshee start (restart necessary)"
        );

    }
}
