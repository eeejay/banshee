//
// SqliteUtils.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
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
using System.Text;

namespace Hyena.Data.Sqlite
{
    internal static class SqliteUtils
    {
        public static string GetType (Type type)
        {
            if (type == typeof (string)) {
                return "TEXT";
            } else if (type == typeof (int) || type == typeof (long)
                || type == typeof (DateTime) || type == typeof (TimeSpan)) {
                return "INTEGER";
            } else {
                throw new Exception (String.Format (
                    "The type {0} cannot be bound to a database column.", type.Name));
            }
        }
        
        public static object ToDbFormat (Type type, object value)
        {
            if (type == typeof (DateTime)) {
                if (DateTime.MinValue.Equals (value)) {
                    value = "NULL";
                } else { 
                    value = DateTimeUtil.FromDateTime ((DateTime)value);
                }
            } else if (type == typeof (TimeSpan)) {
                value = ((TimeSpan)value).TotalMilliseconds;
            }
            return value;
        }
        
        public static object FromDbFormat (Type type, object value)
        {
            if (type == typeof (DateTime)) {
                value = Int64.MinValue.Equals (value) ? DateTime.MinValue : DateTimeUtil.ToDateTime ((long) value);
            } else if (type == typeof (TimeSpan)) {
                value = TimeSpan.FromMilliseconds ((long)value);
            }
            return value;
        }
        
        public static string BuildColumnSchema (string type,
                                                string name,
                                                string default_value,
                                                DatabaseColumnConstraints constraints)
        {
            StringBuilder builder = new StringBuilder ();
            builder.Append (name);
            builder.Append (' ');
            builder.Append (type);
            if ((constraints & DatabaseColumnConstraints.NotNull) > 0) {
                builder.Append (" NOT NULL");
            }
            if ((constraints & DatabaseColumnConstraints.Unique) > 0) {
                builder.Append (" UNIQUE");
            }
            if ((constraints & DatabaseColumnConstraints.PrimaryKey) > 0) {
                builder.Append (" PRIMARY KEY");
            }
            if (default_value != null) {
                builder.Append (" DEFAULT ");
                builder.Append (default_value);
            }
            return builder.ToString ();
        }
    }
}
