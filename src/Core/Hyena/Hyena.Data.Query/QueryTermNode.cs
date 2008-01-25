//
// QueryTermNode.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;

namespace Hyena.Data.Query
{
    public class QueryTermNode : QueryNode
    {
        private QueryField field;
        private Operator op = Operator.Default;
        private QueryValue qvalue;

        public static QueryTermNode ParseUserQuery (QueryFieldSet field_set, string token)
        {
            QueryTermNode node = new QueryTermNode ();
            int field_separator = 0;
            foreach (Operator op in Operator.Operators) {
                field_separator = token.IndexOf (op.UserOperator);
                if (field_separator != -1) {
                    node.Operator = op;
                    break;
                }
            }

            if (field_separator > 0) {
                node.Field = field_set[token.Substring (0, field_separator)];
                if (node.Field != null) {
                    token = token.Substring (field_separator + node.Operator.UserOperator.Length);
                }
            }

            node.Value = QueryValue.CreateFromUserQuery (token, node.Field);

            return node;
        }

        public QueryTermNode () : base ()
        {
        }

        public override QueryNode Trim ()
        {
            if ((qvalue == null || qvalue.IsEmpty) && Parent != null)
                Parent.RemoveChild (this);
            return this;
        }
        
        public override void AppendUserQuery (StringBuilder sb)
        {
            sb.Append (Field == null ? Value.ToUserQuery () : Field.ToTermString (Operator.UserOperator, Value.ToUserQuery ()));
        }

        public override void AppendXml (XmlDocument doc, XmlNode parent, QueryFieldSet fieldSet)
        {
            XmlElement op_node = doc.CreateElement (op.Name);
            parent.AppendChild (op_node);

            QueryField field = Field;
            if (field != null) {
                XmlElement field_node = doc.CreateElement ("field");
                field_node.SetAttribute ("name", field.Name);
                op_node.AppendChild (field_node);
            }

            XmlElement val_node = doc.CreateElement (Value.XmlElementName);
            Value.AppendXml (val_node);
            op_node.AppendChild (val_node);
        }

        public override void AppendSql (StringBuilder sb, QueryFieldSet fieldSet)
        {
            if (Field == null) {
                sb.Append ("(");
                int emitted = 0;
                
                foreach (QueryField field in fieldSet.Fields) {
                    if (field.IsDefault)
                        if (EmitTermMatch (sb, field, emitted > 0))
                            emitted++;
                }
                
                sb.Append (")");
            } else {
                EmitTermMatch (sb, Field, false);
            }
        }

        private bool EmitTermMatch (StringBuilder sb, QueryField field, bool emit_or)
        {
            if (Value.IsEmpty)
                return false;

            if (field.ValueType == typeof(StringQueryValue)) {
                return EmitStringMatch (sb, field, emit_or);
            } else {
                return EmitNumericMatch (sb, field, emit_or);
            }
        }

        private bool EmitStringMatch (StringBuilder sb, QueryField field, bool emit_or)
        {
            string safe_value = Value.ToSql ().Replace("'", "''");
            
            if (emit_or)
                sb.Append (" OR ");

            string format = null;
            switch (Operator.UserOperator) {
                case "=": // Starts with
                    format = "LIKE '{0}%'";
                    break;

                case ":=": // Ends with
                    format = "LIKE '%{0}'";
                    break;
                    
                case "==": // Equal to
                    format = "= '{0}'";
                    break;

                case "!=": // Not equal to
                    format = "!= '{0}'";
                    break;

                case "!:": // Doesn't contain
                    format = "NOT LIKE '%{0}%'";
                    break;

                case ":": // Contains
                default:
                    format = "LIKE '%{0}%'";
                    break;
            }

            sb.Append (field.FormatSql (format, Operator, safe_value));
            return true;
        }

        private bool EmitNumericMatch (StringBuilder sb, QueryField field, bool emit_or)
        {
            string format;
            switch (Operator.UserOperator) {
                // Operators that don't make sense for numeric types
                case "!:":
                case ":=":
                    return false;

                case ":":
                case "=":
                case "==":
                    format = "= {0}";
                    break;

                default:
                    format = Operator.UserOperator + " {0}";
                    break;
            }

            if (emit_or) {
                sb.Append (" OR ");
            }

            sb.Append (field.FormatSql (format,
                Operator, Value.ToSql ()
            ));

            return true;
        }
        
        public QueryField Field {
            get { return field; }
            set { field = value; }
        }

        public Operator Operator {
            get { return op; }
            set { op = value; }
        }
        
        public QueryValue Value {
            get { return qvalue; }
            set { qvalue = value; }
        }
    }
}
