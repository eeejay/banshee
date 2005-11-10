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

namespace Banshee.Logging
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
        None,
        Debug,
        Warning,
        Error
    }
    
    public class LogEntry
    {
        private LogEntryType type;
        private string shortMessage;
        private string details;
        private bool showUser;
        private DateTime timestamp;
        
        internal LogEntry(LogEntryType type, string shortMessage, string details, bool showUser)
        {
            this.type = type;
            this.shortMessage = shortMessage;
            this.details = details;
            this.showUser = showUser;
            timestamp = DateTime.Now;
        }
        
        public override string ToString()
        {
            return String.Format("{0}: [{1}] ({2}) - {3}", type, timestamp, shortMessage, details);
        }
        
        public LogEntryType Type   { get { return type;         } }
        public string ShortMessage { get { return shortMessage; } }
        public string Details      { get { return details;      } }
        public DateTime TimeStamp  { get { return timestamp;    } }
        public bool ShowUser       { get { return showUser;     } }
    }
    
    public class ErrorLogEntry : LogEntry
    {
        public ErrorLogEntry(string shortMessage, string details, bool showUser) :
            base(LogEntryType.Error, shortMessage, details, showUser)
        {
        }
    }
    
    public class WarningLogEntry : LogEntry
    {
        public WarningLogEntry(string shortMessage, string details, bool showUser) :
            base(LogEntryType.Warning, shortMessage, details, showUser)
        {
        }
    }
    
    public class DebugLogEntry : LogEntry
    {
        public DebugLogEntry(string shortMessage, string details, bool showUser) :
            base(LogEntryType.Debug, shortMessage, details, showUser)
        {
        }
    }
    
    public class LogCore : IEnumerable
    {
        private static LogCore instance;
        public static LogCore Instance {
            get {
                if(instance == null) {
                    instance = new LogCore();
                }
                
                return instance;
            }
        }
         
        public static bool PrintEntries = true;
        private ArrayList entries = new ArrayList();
        public event LogCoreUpdatedHandler Updated;
         
        protected LogCore()
        {
        }
         
        private void QueueNotify(LogEntry entry)
        {
             LogCoreUpdatedHandler handler = Updated;
             if(handler != null) {
                 handler(this, new LogCoreUpdatedArgs(entry));
             }
        }
        
        public LogEntry PushError(string shortMessage, string details)
        {
            return PushError(shortMessage, details, true);
        }
        
        public LogEntry PushError(string shortMessage, string details, bool showUser)
        {
            return Push(new ErrorLogEntry(shortMessage, details, showUser));
        }
        
        public LogEntry PushWarning(string shortMessage, string details)
        {
            return PushWarning(shortMessage, details, true);
        }
        
        public LogEntry PushWarning(string shortMessage, string details, bool showUser)
        {
            return Push(new WarningLogEntry(shortMessage, details, showUser));
        }
        
        public LogEntry PushDebug(string shortMessage, string details)
        {
            return PushDebug(shortMessage, details, false);
        }
        
        public LogEntry PushDebug(string shortMessage, string details, bool showUser)
        {
            return Push(new DebugLogEntry(shortMessage, details, showUser));
        }
        
        public LogEntry Push(LogEntry entry)
        {
            if(PrintEntries) {
                Console.WriteLine(entry);
            }
            
            entries.Insert(0, entry);
            
            Gtk.Application.Invoke(delegate {
                QueueNotify(entry);
            });
            
            return entry;
        }
        
        public IEnumerator GetEnumerator()
        {
            return entries.GetEnumerator();
        }
    }
}
