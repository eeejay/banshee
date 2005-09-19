/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  LogCore.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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

namespace Banshee
{
    public delegate void LogCoreUpdatedHandler(object o,
         LogCoreUpdatedArgs args);

    public class LogCoreUpdatedArgs : EventArgs
    {
        public LogEntry Entry;
        
        public LogCoreUpdatedArgs(LogEntry entry)
        {
            Entry = entry;
        }
    }
        
    public enum LogEntryType
    {
        Debug,
        Warning,
        Error,
        UserError
    }
    
    public class LogEntry
    {
        private LogEntryType type;
        private string shortMessage;
        private string details;
        private DateTime timestamp;
        
        public LogEntry(LogEntryType type, string shortMessage, string details)
        {
            this.type = type;
            this.shortMessage = shortMessage;
            this.details = details;
            timestamp = DateTime.Now;
        }
        
        public LogEntryType Type   { get { return type;         } }
        public string ShortMessage { get { return shortMessage; } }
        public string Details      { get { return details;      } }
        public DateTime TimeStamp  { get { return timestamp;    } }        
    }
    
    public class LogCore
    {
        private Hashtable logs = new Hashtable();
        private ArrayList notifyQueue = new ArrayList();
        
        private Gtk.ThreadNotify notify;
        
        private bool queueLocked = false;
        
        public event LogCoreUpdatedHandler Updated;
        
        public LogCore()
        {
            notify = new Gtk.ThreadNotify(new Gtk.ReadyEvent(OnReadyNotify));
        }
        
        private void OnReadyNotify()
        {
            foreach(LogEntry entry in notifyQueue)
                QueueNotify(entry);
            notifyQueue.Clear();
        }
        
        private void QueueNotify(LogEntry entry)
        {
             LogCoreUpdatedHandler handler = Updated;
             if(handler != null) {
                 handler(this, new LogCoreUpdatedArgs(entry));
             }
        }
        
        public void Push(LogEntryType type, string shortMessage, 
            string details)
        {
            Push(new LogEntry(type, shortMessage, details));
        }
        
        public void Push(LogEntry entry)
        {
            if(logs[entry.Type] == null)
                logs[entry.Type] = new ArrayList();
                
            (logs[entry.Type] as ArrayList).Insert(0, entry);
            
            notifyQueue.Add(entry);
            notify.WakeupMain();
        }
    }
}
