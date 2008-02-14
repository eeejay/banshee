//
// QueryField.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
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
using System.Text;
using System.Collections.Generic;

namespace Hyena.Query
{
    public class QueryField : IAliasedObject
    {
        private Type value_type;
        public Type ValueType {
            get { return value_type; }
        }

        private string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        private string label;
        public string Label {
            get { return label; }
            set { label = value; }
        }

        private string [] aliases;
        public string [] Aliases {
            get { return aliases; }
        }

        public string PrimaryAlias {
            get { return aliases[0]; }
        }

        private string column;
        public string Column {
            get { return column; }
        }

        private bool is_default;
        public bool IsDefault {
            get { return is_default; }
        }

        public QueryField (string name, string label, string column, params string [] aliases)
            : this (name, label, column, false, aliases)
        {
        }

        public QueryField (string name, string label, string column, bool isDefault, params string [] aliases)
            : this (name, label, column, typeof(StringQueryValue), isDefault, aliases)
        {
        }

        public QueryField (string name, string label, string column, Type valueType, params string [] aliases)
            : this (name, label, column, valueType, false, aliases)
        {
        }

        public QueryField (string name, string label, string column, Type valueType, bool isDefault, params string [] aliases)
        {
            this.name = name;
            this.label = label;
            this.column = column;
            this.value_type = valueType;
            this.is_default = isDefault;
            this.aliases = aliases;

            QueryValue.AddValueType (value_type);
        }

        public QueryValue CreateQueryValue ()
        {
            return Activator.CreateInstance (ValueType) as QueryValue;
        }

        public string ToTermString (string op, string value)
        {
            return ToTermString (PrimaryAlias, op, value);
        }

        public string ToSql (Operator op, QueryValue qv)
        {
            string value = qv.ToSql ();
            if (op == null) op = qv.OperatorSet.First;
            if (Column.IndexOf ("{0}") == -1 && Column.IndexOf ("{1}") == -1) {
                if (ValueType == typeof(StringQueryValue)) {
                    // Match string values literally and against a lower'd version 
                    return String.Format ("({0} {1} OR LOWER({0}) {2})", Column,
                        String.Format (op.SqlFormat, value),
                        String.Format (op.SqlFormat, value.ToLower ())
                    );
                } else {
                    return String.Format ("{0} {1}", Column, String.Format (op.SqlFormat, value));
                }
            } else {
                return String.Format (
                    Column, String.Format (op.SqlFormat, value),
                    value, op.IsNot ? "NOT" : null
                );
            }
        }

        public static string ToTermString (string alias, string op, string value)
        {
            value = String.Format (
                "{1}{0}{1}", 
                value, value.IndexOf (" ") == -1 ? String.Empty : "\""
            );

            return String.IsNullOrEmpty (alias)
                ? value
                : String.Format ("{0}{1}{2}", alias, op, value);
        }
    }
}
