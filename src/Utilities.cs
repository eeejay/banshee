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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions; 
using Mono.Unix;

namespace Banshee
{
    public class PathUtil
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
    }
    
    public class StringUtil
    {
        public static string EntityEscape(string str)
        {
            if(str == null)
                return null;
                
            return GLib.Markup.EscapeText(str);
        }
    
        private static string RegexHexConvert(Match match)
        {
            int digit = Convert.ToInt32(match.Groups[1].ToString(), 16);
            return Convert.ToChar(digit).ToString();
        }    
                
        /*public static string UriEscape(string uri)
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
    
    public class Resource
    {
        public static string GetFileContents(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream(name);
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();    
        }
    }    
    
    public class Utilities
    {
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
    
    public sealed class UnixFileUtil
    {
        [Flags]
        public enum OpenFlags : int {
            //
            // One of these
            //
            O_RDONLY    = 0x00000000,
            O_WRONLY    = 0x00000001,
            O_RDWR      = 0x00000002,

            //
            // Or-ed with zero or more of these
            //
            O_CREAT     = 0x00000040,
            O_EXCL      = 0x00000080,
            O_NOCTTY    = 0x00000100,
            O_TRUNC     = 0x00000200,
            O_APPEND    = 0x00000400,
            O_NONBLOCK  = 0x00000800,
            O_SYNC      = 0x00001000,

            //
            // These are non-Posix.  Using them will result in errors/exceptions on
            // non-supported platforms.
            //
            // (For example, "C-wrapped" system calls -- calls with implementation in
            // MonoPosixHelper -- will return -1 with errno=EINVAL.  C#-wrapped system
            // calls will generate an exception in NativeConvert, as the value can't be
            // converted on the target platform.)
            //

            O_NOFOLLOW  = 0x00020000,
            O_DIRECTORY = 0x00010000,
            O_DIRECT    = 0x00004000,
            O_ASYNC     = 0x00002000,
            O_LARGEFILE = 0x00008000
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
}

