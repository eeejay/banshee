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
        private string op;

        /*public struct Operator
        {
            public string [] UserQueryOperators;
            public string Label;
            public string Name;
            public string SqlFormat;

            public Operator (string name, string label, string sqlFormat, string [] operators)
            {
                Name = name;
                Label = label;
                SqlFormat = sqlFormat;
                UserQueryOperators = operators;
            }

            public static Operator 
        }*/

        // Note, order of these is important since if = was before ==, the value of the
        // term would start with the second =, etc.
        private static string [] operators = new string [] {":", "==", "<=", ">=", "=", "<", ">"};

        public QueryTermNode(string value) : base()
        {
            int field_separator = 0;
            foreach (string op in operators) {
                field_separator = value.IndexOf (op);
                if (field_separator != -1) {
                    this.op = op;
                    break;
                }
            }

            if(field_separator > 0) {
                field = value.Substring(0, field_separator);
                this.value = value.Substring(field_separator + op.Length);
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
            node.SetAttribute ("op", Operator);
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
                        if (EmitTermMatch (sb, field, emitted > 0))
                            emitted++;
                }
                
                sb.Append (")");
            } else if(alias != null && fieldSet.Map.ContainsKey(alias)) {
                EmitTermMatch (sb, fieldSet.Map[alias], false);
            }
        }

        private bool EmitTermMatch (StringBuilder sb, QueryField field, bool emit_or)
        {
            if (field.QueryFieldType == QueryFieldType.Text)
                return EmitStringMatch(sb, field.Column, emit_or);
            else
                return EmitNumericMatch(sb, field.Column, emit_or);
        }

        private bool EmitStringMatch(StringBuilder sb, string field, bool emit_or)
        {
            string safe_value = Value.Replace("'", "''");
            
            if (emit_or)
                sb.Append (" OR ");

            switch (Operator) {
                case "=": // Treat as starts with
                case "==":
                    sb.AppendFormat("{0} LIKE '{1}%'", field, safe_value);
                    break;

                case ":": // Contains
                default:
                    sb.AppendFormat("{0} LIKE '%{1}%'", field, safe_value);
                    break;
            }

            return true;
        }

        private bool EmitNumericMatch(StringBuilder sb, string field, bool emit_or)
        {
            try {
                // TODO could add a handler to the Field class so we could preprocess
                // date queries (and turn them into numbers), etc..
                int num = Convert.ToInt32 (Value);
                if (emit_or)
                    sb.Append (" OR ");

                switch (Operator) {
                    case ":":
                    case "=":
                    case "==":
                        sb.AppendFormat("{0} = {1}", field, num);
                        break;

                    default:
                        sb.AppendFormat("{0} {2} {1}", field, num, Operator);
                        break;
                }
                return true;
            } catch {}
            return false;
        }
        
        public string Value {
            get { return value; }
        }

        public string Operator {
            get { return op; }
        }
        
        public string Field {
            get { return field; }
        }
    }
}
