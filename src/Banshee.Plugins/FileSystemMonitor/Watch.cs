/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Watch.cs
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
using System.Globalization;
using System.IO;

namespace Banshee.Plugins.FileSystemMonitor
{
    internal delegate void FileSystemMonitorPathEventHandler(object o, FileSystemMonitorPathEventArgs args);
    
    internal class FileSystemMonitorPathEventArgs : EventArgs
    {
        public string Path;
    }

    internal abstract class Watch 
    {
        private string watch_folder;
        
        internal event FileSystemMonitorPathEventHandler QueueImportRequest;
        internal event FileSystemMonitorPathEventHandler QueueRemoveRequest;
        
        internal Watch(string watchFolder)
        {
            watch_folder = watchFolder;
        }
        
        internal bool RecurseDirectory(string path)
        {
            DirectoryInfo di;
            
            try {
                di = new DirectoryInfo(path);
            } catch(Exception) {
                return false;
            }
            
            if(!di.Exists) {
                return false;
            }
            
            if(!AddWatch(path)) {
                return false;
            }
            
            foreach(DirectoryInfo sdi in di.GetDirectories()) {
                if(!sdi.Name.StartsWith(".")) {
                    RecurseDirectory(path + Path.DirectorySeparatorChar + sdi.Name);
                }
            }
            
            return true;
        }
        
        protected void QueueImport(string path)
        {
            FileSystemMonitorPathEventHandler handler = QueueImportRequest;
            if(handler != null) {
                FileSystemMonitorPathEventArgs args = new FileSystemMonitorPathEventArgs();
                args.Path = path;
                handler(this, args);
            }
        }
        
        protected void QueueRemove(string path)
        {
            FileSystemMonitorPathEventHandler handler = QueueRemoveRequest;
            if(handler != null) {
                FileSystemMonitorPathEventArgs args = new FileSystemMonitorPathEventArgs();
                args.Path = path;
                handler(this, args);
            }
        }
        
        protected string WatchFolder {
            get {
                return watch_folder;
            }
        }
        
        internal abstract bool IsWatching(string path);
        internal abstract bool AddWatch(string path);
        
        internal virtual void Dispose()
        {
        }
    }
}
