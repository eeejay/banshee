
/***************************************************************************
 *  FileSystemWatcherWatch.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Doğacan Güney  <dogacan@gmail.com>
 *             Aaron Bockover <aaron@aaronbock.net>
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
using System.IO;

using Banshee.Base;

namespace Banshee.Plugins.FileSystemMonitor
{
    internal sealed class FileSystemWatcherWatch : Watch
    {
        private Hashtable watch_map;
        
        internal FileSystemWatcherWatch(string watchFolder) : base(watchFolder)
        {
            watch_map = new Hashtable();
            RecurseDirectory(WatchFolder);
        }
                
        internal override bool IsWatching(string path)
        {
            return watch_map.ContainsKey(path);
        }
        
        internal override bool AddWatch(string path)
        {
            if(IsWatching(path) || !Directory.Exists(path)) {
                return false;
            }

            LogCore.Instance.PushDebug("Registering FileSystemWatcher watch", path);
            
            FileSystemWatcher watcher = new FileSystemWatcher();
            
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.IncludeSubdirectories = false;
            watcher.Filter = "";

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            watcher.EnableRaisingEvents = true;
            
            watch_map.Add(path, watcher);
            
            return true;
        }
        
        private void OnChanged(object source, FileSystemEventArgs args)
        {
            lock(this) {
                QueueImport(args.FullPath);
            }
        }
        
        private void OnDeleted(object source, FileSystemEventArgs args)
        {
            lock(this) {
                QueueRemove(args.FullPath);
            }
        }

        private void OnRenamed(object source, RenamedEventArgs args)
        {
            lock(this) {
                QueueImport(args.FullPath);
                QueueRemove(args.OldFullPath);
            }
        }
    }
}
