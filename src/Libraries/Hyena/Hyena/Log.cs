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
using System.Collections.Generic;

namespace Hyena
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
        
        private static Dictionary<uint, DateTime> timers = new Dictionary<uint, DateTime> ();
        private static uint next_timer_id = 1;

        private static bool debugging = false;
        public static bool Debugging {
            get { return debugging; }
            set { debugging = value; }
        }
        
        private static void Commit (LogEntryType type, string message, string details, bool showUser)
        {
            if (type == LogEntryType.Debug && !Debugging) {
                return;
            }
        
            if (type != LogEntryType.Information || (type == LogEntryType.Information && !showUser)) {
                switch (type) {
                    case LogEntryType.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                    case LogEntryType.Warning: Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                    case LogEntryType.Information: Console.ForegroundColor = ConsoleColor.Green; break;
                    case LogEntryType.Debug: Console.ForegroundColor = ConsoleColor.Blue; break;
                }
                
                Console.Write ("[{0} {1:00}:{2:00}:{3:00}.{4:000}]", type, DateTime.Now.Hour,
                    DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
                
                Console.ResetColor ();
                               
                if (details != null) {
                    Console.WriteLine (" {0} - {1}", message, details);
                } else {
                    Console.WriteLine (" {0}", message);
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
        
        #region Public Debug Methods
                                    
        public static void Debug (string message, string details)
        {
            if (Debugging) {
                Commit (LogEntryType.Debug, message, details, false);
            }
        }
        
        public static void Debug (string message)
        {
            if (Debugging) {
                Debug (message, null);
            }
        }
        
        public static void DebugFormat (string format, params object [] args)
        {
            if (Debugging) {
                Debug (String.Format (format, args));
            }
        }
        
        public static uint DebugTimerStart (string message)
        {
            if (!Debugging) {
                return 0;
            }
            
            Debug (message);
            return DebugTimerStart ();
        }
        
        public static uint DebugTimerStart ()
        {
            if (!Debugging) {
                return 0;
            }
            
            uint timer_id = next_timer_id++;
            timers.Add (timer_id, DateTime.Now);
            return timer_id;
        }
        
        public static void DebugTimerPrint (uint id)
        {
            if (!Debugging) {
                return;
            }
            
            DebugTimerPrint (id, "Operation duration: {0}");
        }
        
        public static void DebugTimerPrint (uint id, string message)
        {
            if (!Debugging) {
                return;
            }
            
            DateTime finish = DateTime.Now;
            
            if (!timers.ContainsKey (id)) {
                return;
            }
            
            TimeSpan duration = finish - timers[id];
            string d_message;
            if (duration.TotalSeconds < 60) {
                d_message = String.Format ("{0}s", duration.TotalSeconds);
            } else {
                d_message = duration.ToString ();
            }
            
            DebugFormat (message, d_message);
        }
        
        #endregion
        
        #region Public Information Methods
            
        public static void Information (string message)
        {
            Information (message, null);
        }
        
        public static void Information (string message, string details)
        {
            Information (message, details, false);
        }
        
        public static void Information (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Information, message, details, showUser);
        }
        
        public static void Information (string message, bool showUser)
        {
            Information (message, null, showUser);
        }
        
        #endregion
        
        #region Public Warning Methods
        
        public static void Warning (string message)
        {
            Warning (message, null);
        }
        
        public static void Warning (string message, string details)
        {
            Warning (message, details, true);
        }
        
        public static void Warning (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Warning, message, details, showUser);
        }
        
        public static void Warning (string message, bool showUser)
        {
            Warning (message, null, showUser);
        }
        
        #endregion
        
        #region Public Error Methods
        
        public static void Error (string message)
        {
            Error (message, null);
        }
        
        public static void Error (string message, string details)
        {
            Error (message, details, true);
        }
        
        public static void Error (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Error, message, details, showUser);
        }
        
        public static void Error (string message, bool showUser)
        {
            Error (message, null, showUser);
        }
        
        #endregion
    }
}
