//
// Log.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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

namespace Banshee.Base
{
    public delegate void LogNotifyHandler (LogNotifyArgs args);

    public class LogNotifyArgs : EventArgs
    {
        private LogEntry entry;
        
        public LogNotifyArgs (LogEntry entry)
        {
            this.entry = entry;
        }
        
        public LogEntry Entry {
            get { return entry; }
        }
    }
        
    public enum LogEntryType
    {
        Debug,
        Warning,
        Error,
        Information
    }
    
    public class LogEntry
    {
        private LogEntryType type;
        private string message;
        private string details;
        private DateTime timestamp;
        
        internal LogEntry (LogEntryType type, string message, string details)
        {
            this.type = type;
            this.message = message;
            this.details = details;
            this.timestamp = DateTime.Now;
        }

        public LogEntryType Type { 
            get { return type; }
        }
        
        public string Message { 
            get { return message; } 
        }
        
        public string Details { 
            get { return details; } 
        }

        public DateTime TimeStamp { 
            get { return timestamp; } 
        }
    }
    
    public static class Log
    {
        public static event LogNotifyHandler Notify;
        
        private static void Commit (LogEntryType type, string message, string details, bool showUser)
        {
            if (type != LogEntryType.Information) {
                if (details != null) {
                    Console.WriteLine ("[{0}::{1}] {2} - {3}", type, DateTime.Now, message, details);
                } else {
                    Console.WriteLine ("[{0}::{1}] {2}", type, DateTime.Now, message);
                }
            }
            
            if (showUser) {
                OnNotify (new LogEntry (type, message, details));
            }
        }
        
        private static void OnNotify (LogEntry entry)
        {
            LogNotifyHandler handler = Notify;
            if (handler != null) {
                handler (new LogNotifyArgs (entry));
            }
        }
                                    
        public static void Debug (string message, string details)
        {
            Commit (LogEntryType.Debug, message, details, false);
        }
        
        public static void Debug (string message)
        {
            Debug (message, null);
        }
        
        public static void Information (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Information, message, details, showUser);
        }
        
        public static void Information (string message, bool showUser)
        {
            Information (message, null, showUser);
        }
        
        public static void Warning (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Warning, message, details, showUser);
        }
        
        public static void Warning (string message, bool showUser)
        {
            Warning (message, null, showUser);
        }
        
        public static void Error (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Error, message, details, showUser);
        }
        
        public static void Error (string message, bool showUser)
        {
            Error (message, null, showUser);
        }
    }
}
