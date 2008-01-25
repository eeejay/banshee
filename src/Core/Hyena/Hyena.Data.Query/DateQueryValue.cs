//
// DateQueryValue.cs
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
using System.Xml;
using System.Text;

using Hyena;

namespace Hyena.Data.Query
{
    public enum RelativeDateFactor {
        Second = 1,
        Minute = 60,
        Hour   = 3600,
        Day    = 3600*24,
        Week   = 3600*24*7,
        Month  = 3600*24*30,
        Year   = 3600*24*365
    }

    public class DateQueryValue : QueryValue
    {
        protected DateTime value;
        protected bool relative = false;
        protected long offset = 0;

        public override string XmlElementName {
            get { return "date"; }
        }

        public override object Value {
            get { return value; }
        }

        public bool Relative {
            get { return relative; }
            set { relative = value; }
        }

        public long RelativeOffset {
            get { return offset; }
            set { offset = value; IsEmpty = false; }
        }

        public override void ParseUserQuery (string input)
        {
            // TODO: Add support for relative strings like "yesterday", "3 weeks ago", "5 days ago"
            try {
                value = DateTime.Parse (input);
                IsEmpty = false;
            } catch {
                IsEmpty = true;
            }
        }

        public override string ToUserQuery ()
        {
            if (value.Hour == 0 && value.Minute == 0 && value.Second == 0) {
                return value.ToString ("yyyy-MM-dd");
            } else {
                return value.ToString ();
            }
        }

        public override void ParseXml (XmlElement node)
        {
            try {
                if (node.HasAttribute ("type") && node.GetAttribute ("type") == "rel") {
                    relative = true;
                    offset = Convert.ToInt64 (node.InnerText);
                } else {
                    value = DateTime.Parse (node.InnerText);
                }
                IsEmpty = false;
            } catch {
                IsEmpty = true;
            }
        }

        public override void AppendXml (XmlElement node)
        {
            if (relative) {
                node.SetAttribute ("type", "rel");
                node.InnerText = RelativeOffset.ToString ();
            } else {
                base.AppendXml (node);
            }
        }

        public override string ToSql ()
        {
            return DateTimeUtil.FromDateTime (
                (relative ? DateTime.Now + TimeSpan.FromSeconds ((double) offset) : value)
            ).ToString ();
        }

        public DateTime DateTime {
            get { return value; }
        }
    }
}
