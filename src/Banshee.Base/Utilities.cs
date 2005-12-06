/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Utilities.cs
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
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions; 
using Mono.Unix;
 
namespace Banshee.Base
{
    public static class UidGenerator
    {
        private static int uid = 0;
        
        public static int Next
        {
            get {
                return ++uid;
            }
        }
    }
    
    public static class Utilities
    {    
        [DllImport("libglib-2.0.so")]
        private static extern IntPtr g_get_real_name();

        public static string GetRealName()
        {
            try {
                string name = GLib.Marshaller.Utf8PtrToString(g_get_real_name());
                string [] parts = name.Split(' ');
                return parts[0].Replace(',', ' ').Trim();
            } catch(Exception) { 
                return null;
            }
        }
        
        public static string BytesToString(ulong bytes)
        {
            ulong mb = bytes / (1024 * 1024);

            if (mb > 1024)
                return String.Format(Catalog.GetString("{0} GB"), mb / 1024);
            else
                return String.Format(Catalog.GetString("{0} MB"), mb);
        }
    }
    
    public class DateTimeUtil
    {
        public static readonly DateTime LocalUnixEpoch = 
            new DateTime(1970, 1, 1).ToLocalTime();

        public static DateTime ToDateTime(long time)
        {
            return FromTimeT(time);
        }

        public static long FromDateTime(DateTime time)
        {
            return ToTimeT(time);
        }

        public static DateTime FromTimeT(long time)
        {
            return LocalUnixEpoch.AddSeconds(time);
        }

        public static long ToTimeT(DateTime time)
        {
            return (long)time.Subtract(LocalUnixEpoch).TotalSeconds;
        }
    }

    public class Timer : IDisposable
    {
        private DateTime start;
        private string label;
        
        public Timer(string label) 
        {
            this.label = label;
            start = DateTime.Now;
        }

        public TimeSpan ElapsedTime {
            get {
                return DateTime.Now - start;
            }
        }

        public void WriteElapsed(string message)
        {
            Console.WriteLine("{0} {1} {2}", label, message, ElapsedTime);
        }

        public void Dispose()
        {
            WriteElapsed("timer stopped:");
        }
    }
    
    public static class ThreadAssist
    {
        private static Thread main_thread;
        
        static ThreadAssist()
        {
            main_thread = Thread.CurrentThread;
        }
        
        public static bool InMainThread {
            get {
                return main_thread.Equals(Thread.CurrentThread);
            }
        }
        
        public static void ProxyToMain(EventHandler handler)
        {
            if(!InMainThread) {
                Gtk.Application.Invoke(handler);
            } else {
                handler(null, new EventArgs());
            }
        }
        
        public static Thread Spawn(ThreadStart threadedMethod, bool autoStart)
        {
            Thread thread = new Thread(threadedMethod);
            if(autoStart) {
                thread.Start();
            }
            return thread;
        }
        
        public static Thread Spawn(ThreadStart threadedMethod)
        {
            return Spawn(threadedMethod, true);
        }
    }
    
    /*public static class Event
    {
        public delegate EventArgs ArgumentRequestCallback();
       
        public static void Invoke(Delegate handler, object o)
        {
            Invoke(handler, o, new EventArgs());
        }
       
        public static void Invoke(Delegate handler, object o, EventArgs args)
        {
            Delegate tmp_handler = handler;
            if(tmp_handler != null) {
                tmp_handler.Method.Invoke(o, new object [] { o, args });
            }
        }

        public static void Invoke(Delegate handler, object o, ArgumentRequestCallback argumentCallback)
        {
            Delegate tmp_handler = handler;
            if(tmp_handler != null) {
                tmp_handler.Method.Invoke(o, new object [] { o, argumentCallback() });
            }
        }
        
        public static void InvokeOnMain(Delegate handler, object o)
        {
            ThreadAssist.ProxyToMain(delegate { Invoke(handler, o); });
        }
        
        public static void InvokeOnMain(Delegate handler, object o, EventArgs args)
        {
            ThreadAssist.ProxyToMain(delegate { Invoke(handler, o, args); });
        }
        
        public static void InvokeOnMain(Delegate handler, object o, ArgumentRequestCallback argumentCallback)
        {
            ThreadAssist.ProxyToMain(delegate { Invoke(handler, o, argumentCallback); });
        }
    }*/
    
    public static class StringUtil
    {
        public static string EntityEscape(string str)
        {
            if(str == null)
                return null;
                
            return GLib.Markup.EscapeText(str);
        }
    
        /*private static string RegexHexConvert(Match match)
        {
            int digit = Convert.ToInt32(match.Groups[1].ToString(), 16);
            return Convert.ToChar(digit).ToString();
        }   
                
        public static string UriEscape(string uri)
        {
            return Regex.Replace(uri, "%([0-9A-Fa-f][0-9A-Fa-f])", 
                new MatchEvaluator(RegexHexConvert));
        }
        
        public static string UriToFileName(string uri)
        {
            return new Uri(uri).LocalPath;
        }*/
        
        public static string UcFirst(string str)
        {
            return Convert.ToString(str[0]).ToUpper() + str.Substring(1);
        }
    }
    
    public static class PathUtil
    {
        private static char[] CharsToQuote = { ';', '?', ':', '@', '&', '=', '$', ',', '#' };
       
        public static Uri PathToFileUri(string path)
        {
            path = Path.GetFullPath(path);
            
            StringBuilder builder = new StringBuilder();
            builder.Append(Uri.UriSchemeFile);
            builder.Append(Uri.SchemeDelimiter);
            
            int i;
            while((i = path.IndexOfAny(CharsToQuote)) != -1) {
                if(i > 0) {
                    builder.Append(path.Substring(0, i));
                }
                
                builder.Append(Uri.HexEscape(path[i]));
                path = path.Substring(i + 1);
            }
            
            builder.Append(path);
            
            return new Uri(builder.ToString(), true);
        }
        
        public static string FileUriStringToPath(string uri)
        {
            return FileUriToPath(new Uri(uri));
        }
        
        public static string FileUriToPath(Uri uri)
        {
            return uri.LocalPath;
        }
        
        public static string MakeFileNameKey(Uri uri)
        {
            string path = uri.LocalPath;
            return Path.GetDirectoryName(path) + 
                Path.DirectorySeparatorChar + 
                Path.GetFileNameWithoutExtension(path);
        }
    }
    
    public class Resource
    {
        public static string GetFileContents(string name)
        {
            Assembly asm = Assembly.GetCallingAssembly();
            Stream stream = asm.GetManifestResourceStream(name);
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();    
        }
    }
}
