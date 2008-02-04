//
// QueryFieldSet.cs
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
    public class QueryFieldSet
    {
        private Dictionary<string, QueryField> map = new Dictionary<string, QueryField> ();
        private QueryField [] fields;

        public QueryFieldSet (params QueryField [] fields)
        {
            this.fields = fields;
            foreach (QueryField field in fields) {
                map[field.Name.ToLower ()] = field;
                foreach (string alias in field.Aliases)
                    if (!String.IsNullOrEmpty (alias) && alias.IndexOf (" ") == -1)
                        map[alias.ToLower ()] = field;
            }
        }

        public QueryField [] Fields {
            get { return fields; }
        }

        public QueryField GetByAlias (string alias)
        {
            if (!String.IsNullOrEmpty (alias) && map.ContainsKey (alias.ToLower ()))
                return map[alias.ToLower ()];
            return null;
        }

        public QueryField this [string alias] {
            get { return GetByAlias (alias); }
        }
    }
}
