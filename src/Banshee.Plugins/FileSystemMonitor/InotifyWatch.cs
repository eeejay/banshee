/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  InotifyWatch.cs
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
    internal sealed class InotifyWatch : Watch 
    {
        private static bool HasFlag(Inotify.EventType type, Inotify.EventType val)
        {
            return (type & val) == val;
        }
    
        internal InotifyWatch(string watchFolder) : base(watchFolder) 
        {
            RecurseDirectory(PathUtil.FileUriStringToPath(WatchFolder));
            Inotify.Verbose = false;
            Inotify.Start();
        }
        
        internal override bool IsWatching(string path)
        {
            return Inotify.IsWatching(path);
        }
       
        internal override bool AddWatch(string path)
        {            
            if(IsWatching(path) || !Directory.Exists(path)) {
                return false;
            }
                
            LogCore.Instance.PushDebug("Registering Inotify watch", path);
            
            Inotify.Subscribe(path, OnInotifyEvent, Inotify.EventType.CloseWrite | 
                Inotify.EventType.MovedFrom | Inotify.EventType.MovedTo |
                Inotify.EventType.Create | Inotify.EventType.Delete);
               
            return true;
        }
        
        internal override void Dispose()
        {
            Inotify.Stop();
        }
        
        private void OnInotifyEvent(Inotify.Watch watch, string path, string subitem, 
            string srcpath, Inotify.EventType type)
        {
            Console.WriteLine("Inotify event ({03}) {0}: {1}/{2}", type, path, subitem, srcpath);

            string fullPath = Path.Combine(path, subitem);

            lock(this) {
                if(HasFlag(type, Inotify.EventType.MovedTo) || HasFlag(type, Inotify.EventType.CloseWrite)) {
                    QueueImport(fullPath);
                        
                    if(srcpath != null) {
                        QueueRemove(srcpath);
                    }
                } else if(HasFlag(type, Inotify.EventType.Create)) { /*HasFlag (type, Inotify.EventType.IsDirectory)) */
                    QueueImport(fullPath);
                } else if(HasFlag(type, Inotify.EventType.Delete) || HasFlag(type, Inotify.EventType.MovedFrom)) {
                    QueueRemove(fullPath);
                }
            }
        }    
    }
}
