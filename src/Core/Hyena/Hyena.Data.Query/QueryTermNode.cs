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
using System.Xml;
using System.Text;

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

        public override QueryNode Trim ()
        {
            if (value == null || value == String.Empty)
                Parent.RemoveChild (this);
            return this;
        }
        
        public override string ToString()
        {
            if(field != null) {
                return String.Format("[{0}]=\"{1}\"", field, value);
            } else {
                return String.Format("\"{0}\"", Value);
            }
        }

        public override void AppendXml (XmlDocument doc, XmlNode parent)
        {
            XmlElement node = doc.CreateElement ("term");
            if (Field != null)
                node.SetAttribute ("field", Field);
            node.SetAttribute ("value", Value);
            parent.AppendChild (node);
        }

        public override void AppendSql (StringBuilder sb, QueryFieldSet fieldSet)
        {
            string alias = Field;
            
            if(alias != null) {
                alias = alias.ToLower();
            }
            
            if(alias == null || !fieldSet.Map.ContainsKey(alias)) {
                sb.Append ("(");
                int emitted = 0, i = 0;
                
                foreach(QueryField field in fieldSet.Fields) {
                    if (field.Default)
                        if (EmitTermMatch (sb, field, Value, emitted > 0))
                            emitted++;
                }
                
                sb.Append (")");
            } else if(alias != null && fieldSet.Map.ContainsKey(alias)) {
                EmitTermMatch (sb, fieldSet.Map[alias], Value, false);
            }
        }

        private bool EmitTermMatch (StringBuilder sb, QueryField field, string value, bool emit_or)
        {
            if (field.QueryFieldType == QueryFieldType.Text)
                return EmitStringMatch(sb, field.Column, value, emit_or);
            else
                return EmitNumericMatch(sb, field.Column, value, emit_or);
        }

        private bool EmitStringMatch(StringBuilder sb, string field, string value, bool emit_or)
        {
            string safe_value = value.Replace("'", "''");
            
            if (emit_or)
                sb.Append (" OR ");

            sb.AppendFormat(" {0} LIKE '%{1}%' ", field, safe_value);
            return true;
        }

        private bool EmitNumericMatch(StringBuilder sb, string field, string value, bool emit_or)
        {
            try {
                int num = Convert.ToInt32 (value);
                if (emit_or)
                    sb.Append (" OR ");

                sb.AppendFormat(" {0} = {1} ", field, num);
                return true;
            } catch {}
            return false;
        }
        
        public string Value {
            get { return value; }
        }
        
        public string Field {
            get { return field; }
        }
    }
}
