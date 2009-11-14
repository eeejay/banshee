//
// ColumnCellDateTime.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Hyena.Data.Gui;

namespace Banshee.Collection.Gui
{
    public enum DateTimeFormat
    {
        Long,
        ShortDate,
        LongDate,
        ShortTime,
        LongTime
    }

    public class ColumnCellDateTime : ColumnCellText
    {
        public ColumnCellDateTime (string property, bool expand) : base (property, expand)
        {
        }

        private DateTimeFormat format = DateTimeFormat.Long;
        public DateTimeFormat Format {
            get { return format; }
            set { format = value; }
        }

        protected override string GetText (object obj)
        {
            if (obj == null) {
                return String.Empty;
            }

            DateTime dt = (DateTime) obj;

            if (dt == DateTime.MinValue) {
                return String.Empty;
            }

            switch (Format) {
            case DateTimeFormat.Long:         return dt.ToString ();
            case DateTimeFormat.ShortDate:    return dt.ToShortDateString ();
            case DateTimeFormat.LongDate:     return dt.ToLongDateString ();
            case DateTimeFormat.ShortTime:    return dt.ToShortTimeString ();
            case DateTimeFormat.LongTime:     return dt.ToLongTimeString ();
            }

            return String.Empty;
        }
    }
}
