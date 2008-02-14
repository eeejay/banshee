//
// W3CDateTime.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
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
using System.Text.RegularExpressions;

namespace Media.Playlists.Xspf
{
    public struct W3CDateTime: IComparable
    {
        public static readonly W3CDateTime MaxValue = new W3CDateTime(DateTime.MaxValue, TimeSpan.Zero);
        public static readonly W3CDateTime MinValue = new W3CDateTime(DateTime.MinValue, TimeSpan.Zero);
        
        public static TimeSpan LocalUtcOffset {
            get { 
                DateTime now = DateTime.Now;
                return now - now.ToUniversalTime();
            }
        }

        public static W3CDateTime Now {
            get { return new W3CDateTime(DateTime.Now); }
        }
        
        public static W3CDateTime UtcNow {
            get { return new W3CDateTime(DateTime.UtcNow); }
        }

        public static W3CDateTime Today {
            get { return new W3CDateTime(DateTime.Today); }
        }
        
        private DateTime datetime;
        private TimeSpan offset;
        
        public W3CDateTime(DateTime datetime, TimeSpan offset)
        {
            this.datetime = datetime;
            this.offset = offset;
        }
        
        public W3CDateTime(DateTime datetime) : this(datetime, LocalUtcOffset)
        {
        }
        
        public DateTime DateTime {
            get { return datetime; }
        }
        
        public DateTime LocalTime {
            get { return UtcTime + LocalUtcOffset; }
        }
        
        public TimeSpan UtcOffset {
            get { return offset; }
        }
        
        public DateTime UtcTime {
            get { return datetime - offset; }
        }
        
        public W3CDateTime Add(TimeSpan value)
        {
            return new W3CDateTime(datetime + value, offset);
        }

        public W3CDateTime AddDays(double value)
        {
            return new W3CDateTime(datetime.AddDays(value), offset);
        }

        public W3CDateTime AddHours(double value)
        {
            return new W3CDateTime(datetime.AddHours(value), offset);
        }

        public W3CDateTime AddMilliseconds(double value)
        {
            return new W3CDateTime(datetime.AddMilliseconds(value), offset);
        }

        public W3CDateTime AddMinutes(double value)
        {
            return new W3CDateTime(datetime.AddMinutes(value), offset);
        }

        public W3CDateTime AddMonths(int value)
        {
        return new W3CDateTime(datetime.AddMonths(value), offset);
        }

        public W3CDateTime AddSeconds(double value)
        {
            return new W3CDateTime(datetime.AddSeconds(value), offset);
        }

        public W3CDateTime AddTicks(long value)
        {
            return new W3CDateTime(datetime.AddTicks(value), offset);
        }

        public W3CDateTime AddYears(int value)
        {
            return new W3CDateTime(datetime.AddYears(value), offset);
        }

        public override bool Equals(object o)
        {
            if(o == null || !(o is W3CDateTime)) {
                return false;
            }
            
            return DateTime.Equals(UtcTime, ((W3CDateTime)o).UtcTime);
        }

        public static bool Equals(W3CDateTime a, W3CDateTime b)
        {
            return DateTime.Equals(a.UtcTime, b.UtcTime);
        }

        public override int GetHashCode()
        {
            return datetime.GetHashCode() ^ offset.GetHashCode();
        }

        public TimeSpan Subtract(W3CDateTime value)
        {
            return UtcTime.Subtract(value.UtcTime);
        }

        public W3CDateTime Subtract(TimeSpan value)
        {
            return new W3CDateTime(datetime.Subtract(value), offset);
        }

        public W3CDateTime ToLocalTime(TimeSpan offset)
        {
            return new W3CDateTime(UtcTime - offset, offset);
        }

        public W3CDateTime ToLocalTime()
        {
            return ToLocalTime(LocalUtcOffset);
        }

        public W3CDateTime ToUniversalTime()
        {
            return new W3CDateTime(UtcTime, TimeSpan.Zero);
        }
    
        public string ToString(string format)
        {
            switch(format) {
                case "X":
                    return datetime.ToString(@"ddd, dd MMM yyyy HH\:mm\:ss ") + FormatOffset(offset, "");
                case "W":
                    return datetime.ToString(@"yyyy-\MM\-ddTHH\:mm\:ss") + FormatOffset(offset, ":");
                default:
                    throw new FormatException("Unknown format specified. Use X (RFC822) or W (W3C).");
            }
        }

        public override string ToString()
        {
            return ToString("W");
        }

        public static int Compare(W3CDateTime a, W3CDateTime b)
        {
            return DateTime.Compare(a.UtcTime, b.UtcTime);
        }

        public static W3CDateTime Parse(string s)
        {
            const string Rfc822DateFormat = 
                @"^((Mon|Tue|Wed|Thu|Fri|Sat|Sun), *)?(?<day>\d\d?) +" +
                @"(?<month>Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) +" +
                @"(?<year>\d\d(\d\d)?) +" +
                @"(?<hour>\d\d):(?<min>\d\d)(:(?<sec>\d\d))? +" +
                @"(?<ofs>([+\-]?\d\d\d\d)|UT|GMT|EST|EDT|CST|CDT|MST|MDT|PST|PDT)$";
        
            const string W3CDateFormat =
                @"^(?<year>\d\d\d\d)" +
                @"(-(?<month>\d\d)(-(?<day>\d\d)(T(?<hour>\d\d):" + 
                @"(?<min>\d\d)(:(?<sec>\d\d)(?<ms>\.\d+)?)?" +
                @"(?<ofs>(Z|[+\-]\d\d:\d\d)))?)?)?$"; 

            string combined_format = String.Format(
                @"(?<rfc822>{0})|(?<w3c>{1})", Rfc822DateFormat, W3CDateFormat);

            Regex reDate = new Regex(combined_format);
            Match m = reDate.Match(s);
            
            if(!m.Success) {
                throw new FormatException("Input is not a valid W3C or RFC822 date");
            } 
            
            try {
                bool isRfc822 = m.Groups["rfc822"].Success;
                int year = Int32.Parse(m.Groups["year"].Value);
                
                if(year < 1000) {
                    year += 2000 - (year < 50 ? 0 : 1);
                }

                int month;
                if(isRfc822) {
                    month = ParseRfc822Month(m.Groups["month"].Value);
                } else {
                    month = m.Groups["month"].Success ? Int32.Parse(m.Groups["month"].Value) : 1;
                }

                int day = m.Groups["day"].Success ? Int32.Parse(m.Groups["day"].Value) : 1;
                int hour = m.Groups["hour"].Success ? Int32.Parse(m.Groups["hour"].Value) : 0;
                int min = m.Groups["min"].Success ? Int32.Parse(m.Groups["min"].Value) : 0;
                int sec = m.Groups["sec"].Success ? Int32.Parse(m.Groups["sec"].Value) : 0;
                int ms = m.Groups["ms"].Success ? (int)Math.Round(1000 * Double.Parse(m.Groups["ms"].Value)) : 0;

                TimeSpan offset = TimeSpan.Zero;
                if(m.Groups["ofs"].Success) {
                    offset = isRfc822 
                        ? ParseRfc822Offset(m.Groups["ofs"].Value)
                        : ParseW3COffset(m.Groups["ofs"].Value);
                }
        
        
                return new W3CDateTime(new DateTime(year, month, day, hour, min, sec, ms), offset);
            } catch(Exception e) {
                throw new FormatException("Input is not a valid W3C or RFC822 date", e);
            }
        }

        private static readonly string [] MonthNames = new string [] {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun", 
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        private static int ParseRfc822Month(string monthName)
        {
            for(int i = 0; i < 12; i++) {
                if(monthName == MonthNames[i]) {
                    return i + 1;
                }
            }
        
            throw new ApplicationException("Invalid month name: " + monthName);
        }

        private static TimeSpan ParseRfc822Offset(string s)
        {
            if(s == string.Empty) {
                return TimeSpan.Zero;
            }
            
            int hours = 0;
            
            switch(s) {
                case "UT":
                case "GMT": break;
                case "EDT": hours = -4; break;
                case "EST": 
                case "CDT": hours = -5; break;
                case "CST": 
                case "MDT": hours = -6; break;
                case "MST":
                case "PDT": hours = -7; break;
                case "PST": hours = -8; break;
                default:
                    if(s[0] == '+') {
                        string sfmt = s.Substring(1, 2) + ":" + s.Substring(3, 2);
                        return TimeSpan.Parse(sfmt);
                    } else {
                        return TimeSpan.Parse(s.Insert(s.Length - 2, ":"));
                    } 
            }
            
            return TimeSpan.FromHours(hours);
        }

        private static TimeSpan ParseW3COffset(string s)
        {
            if(s == String.Empty || s == "Z") {
                return TimeSpan.Zero;
            } else if(s[0] == '+') {
                return TimeSpan.Parse(s.Substring(1));
            } else {
                return TimeSpan.Parse(s);
            }
        }
        
        private static string FormatOffset(TimeSpan offset, string separator)
        {
            string s = String.Empty;
            
            if(offset >= TimeSpan.Zero) {
                s = "+";
            }
            
            return s + offset.Hours.ToString("00") + separator + offset.Minutes.ToString("00");
        }

        public static bool operator ==(W3CDateTime a, W3CDateTime b)
        {
            return Equals(a, b);
        }

        public static bool operator >(W3CDateTime a, W3CDateTime b)
        {
            return Compare(a, b) > 0;
        }

        public static bool operator >=(W3CDateTime a, W3CDateTime b)
        {
            return Compare(a, b) >= 0;
        }

        public static bool operator !=(W3CDateTime a, W3CDateTime b)
        {
            return !Equals(a, b);
        }

        public static bool operator <(W3CDateTime a, W3CDateTime b)
        {
            return Compare(a, b) < 0;
        }

        public static bool operator <=(W3CDateTime a, W3CDateTime b)
        {
            return Compare(a, b) <= 0;
        }

        public static TimeSpan operator -(W3CDateTime a, W3CDateTime b)
        {
            return a.Subtract(b);
        }

        public static W3CDateTime operator +(W3CDateTime datetime, TimeSpan timespan)
        {
            return datetime.Add(timespan);
        }
        
        public static W3CDateTime operator -(W3CDateTime datetime, TimeSpan timespan)
        {
            return datetime.Subtract(timespan);
        }
        
        public int CompareTo(object o)
        {
            if(o == null) {
                return 1;
            } else if(o is W3CDateTime) {
                return Compare(this, (W3CDateTime)o);
            }
            
            throw new ArgumentException("Must be a W3CDateTime");
        }
    }
}
