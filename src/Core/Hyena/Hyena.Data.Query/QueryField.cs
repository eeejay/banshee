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

namespace Hyena.Data.Query
{
    public enum QueryFieldType
    {
        Text,
        Numeric
    }
    
    public class QueryField
    {
        public delegate string ModifierHandler (string input);

        public string Name;
        public string [] Aliases;
        public string Column;
        public bool Default;
        public QueryFieldType QueryFieldType;
        public ModifierHandler Modifier;

        public QueryField (string name, string column, QueryFieldType type, params string [] aliases) 
            : this (name, column, type, false, null, aliases)
        {
        }
        
        public QueryField (string name, string column, QueryFieldType type, ModifierHandler modifier, params string [] aliases) 
            : this (name, column, type, false, modifier, aliases)
        {
        }
        
        public QueryField (string name, string column, QueryFieldType type, bool isDefault, params string [] aliases)
            : this (name, column, type, isDefault, null, aliases)
        {
        }
        
        public QueryField (string name, string column, QueryFieldType type, bool isDefault, 
            ModifierHandler modifier, params string [] aliases)
        {
            Name = name;
            Column = column;
            QueryFieldType = type;
            Default = isDefault;
            Modifier = modifier;
            Aliases = aliases;
        }

        public string PrimaryAlias {
            get { return Aliases [0]; }
        }

        public string ToTermString (string value)
        {
            return String.Format ("{0}:{2}{1}{2}",
                PrimaryAlias, value, value.IndexOf (" ") == -1 ? "" : "\""
            );
        }
    }

    public class QueryFieldSet
    {
        private Dictionary<string, QueryField> map = new Dictionary<string, QueryField> ();
        private QueryField [] fields;

        public QueryFieldSet (params QueryField [] fields)
        {
            this.fields = fields;
            foreach (QueryField field in fields)
                foreach (string alias in field.Aliases)
                    map[alias.ToLower ()] = field;
        }

        public QueryField [] Fields {
            get { return fields; }
        }

        public Dictionary<string, QueryField> Map {
            get { return map; }
        }
    }
}
