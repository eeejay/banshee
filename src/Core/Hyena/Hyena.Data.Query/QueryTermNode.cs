//
// QueryTermNode.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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

namespace Hyena.Data.Query
{
    public class QueryTermNode : QueryNode
    {
        private string field;
        private string value;
        
        public QueryTermNode(string value) : base()
        {
            int field_separator = value.IndexOf(':');
            if(field_separator > 0) {
                field = value.Substring(0, field_separator);
                this.value = value.Substring(field_separator + 1);
            } else {
                this.value = value;
            }
        }
        
        public override string ToString()
        {
            if(field != null) {
                return String.Format("[{0}]=\"{1}\"", field, value);
            } else {
                return String.Format("\"{0}\"", Value);
            }
        }
        
        public string Value {
            get { return value; }
        }
        
        public string Field {
            get { return field; }
        }
    }
}
