//
// TimeSpanQueryValue.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

using Mono.Unix;

using Hyena;

namespace Hyena.Query
{
    public enum TimeFactor {
        Second = 1,
        Minute = 60,
        Hour   = 3600,
        Day    = 3600*24,
        Week   = 3600*24*7,
        Month  = 3600*24*30,
        Year   = 3600*24*365
    }

    public class TimeSpanQueryValue : IntegerQueryValue
    {
        /*public static readonly Operator Equal              = new Operator ("equals", "= {0}", "==", "=", ":");
        public static readonly Operator NotEqual           = new Operator ("notEqual", "!= {0}", true, "!=", "!:");
        public static readonly Operator LessThanEqual      = new Operator ("lessThanEquals", "<= {0}", "<=");
        public static readonly Operator GreaterThanEqual   = new Operator ("greaterThanEquals", ">= {0}", ">=");
        public static readonly Operator LessThan           = new Operator ("lessThan", "< {0}", "<");
        public static readonly Operator GreaterThan        = new Operator ("greaterThan", "> {0}", ">");*/

        protected long offset = 0;
        protected TimeFactor factor = TimeFactor.Second;

        public override string XmlElementName {
            get { return "timespan"; }
        }

        //protected static AliasedObjectSet<Operator> operators = new AliasedObjectSet<Operator> (Equal, NotEqual, LessThan, GreaterThan, LessThanEqual, GreaterThanEqual);
        protected static AliasedObjectSet<Operator> ops = new AliasedObjectSet<Operator> (LessThan, GreaterThan, LessThanEqual, GreaterThanEqual);
        public override AliasedObjectSet<Operator> OperatorSet {
            get { return operators; }
        }

        public override object Value {
            get { return offset; }
        }

        public virtual long Offset {
            get { return offset; }
        }

        public TimeFactor Factor {
            get { return factor; }
        }

        public long FactoredValue {
            get { return Offset / (long) factor; }
        }

        private static Regex number_regex = new Regex ("\\d+", RegexOptions.Compiled);
        public override void ParseUserQuery (string input)
        {
            Match match = number_regex.Match (input);
            if (match != Match.Empty && match.Groups.Count > 0) {
                int val = Convert.ToInt32 (match.Groups[0].Captures[0].Value);
                foreach (TimeFactor factor in Enum.GetValues (typeof(TimeFactor))) {
                    if (input == FactorString (factor, val)) {
                        SetUserRelativeValue ((long) val, factor);
                        return;
                    }
                }
            }
            IsEmpty = true;
        }

        public override string ToUserQuery ()
        {
            return FactorString (factor, (int) FactoredValue);
        }

        public virtual void SetUserRelativeValue (long offset, TimeFactor factor)
        {
            SetRelativeValue (offset, factor);
        }

        public void SetRelativeValue (long offset, TimeFactor factor)
        {
            this.factor = factor;
            this.offset = offset * (long) factor;
            IsEmpty = false;
        }

        public void LoadString (string val)
        {
            try {
                SetRelativeValue (Convert.ToInt64 (val), TimeFactor.Second);
                DetermineFactor ();
            } catch {
                IsEmpty = true;
            }
        }

        protected void DetermineFactor ()
        {
            long val = Math.Abs (offset);
            foreach (TimeFactor factor in Enum.GetValues (typeof(TimeFactor))) {
                if (val >= (long) factor) {
                    this.factor = factor;
                }
            }
        }

        public override void ParseXml (XmlElement node)
        {
            try {
                LoadString (node.InnerText);
            } catch {
                IsEmpty = true;
            }
        }

        public override string ToSql ()
        {
            return Convert.ToString (offset * 1000);
        }

        protected virtual string FactorString (TimeFactor factor, int count)
        {
            string translated = null;
            switch (factor) {
                case TimeFactor.Second: translated = Catalog.GetPluralString ("{0} second", "{0} seconds", count); break;
                case TimeFactor.Minute: translated = Catalog.GetPluralString ("{0} minute", "{0} minutes", count); break;
                case TimeFactor.Hour:   translated = Catalog.GetPluralString ("{0} hour",   "{0} hours", count); break;
                case TimeFactor.Day:    translated = Catalog.GetPluralString ("{0} day",    "{0} days", count); break;
                case TimeFactor.Week:   translated = Catalog.GetPluralString ("{0} week",   "{0} weeks", count); break;
                case TimeFactor.Month:  translated = Catalog.GetPluralString ("{0} month",  "{0} months", count); break;
                case TimeFactor.Year:   translated = Catalog.GetPluralString ("{0} year",   "{0} years", count); break;
                default: return null;
            }

            return String.Format (translated, count);
        }
    }
}
