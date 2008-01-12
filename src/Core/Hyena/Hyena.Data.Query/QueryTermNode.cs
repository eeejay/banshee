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
    public class Operator
    {
        public string Name;
        public string UserOperator;
        
        private static List<Operator> operators = new List<Operator> ();
        private static Dictionary<string, Operator> by_op = new Dictionary<string, Operator> ();
        private static Dictionary<string, Operator> by_name = new Dictionary<string, Operator> ();

        static Operator () {
            // Note, order of these is important since if = was before ==, the value of the
            // term would start with the second =, etc.
            Add (new Operator ("equals", "=="));
            Add (new Operator ("lessThanEquals", "<="));
            Add (new Operator ("greaterThanEquals", ">="));
            Add (new Operator ("startsWith", "="));
            Add (new Operator ("contains", ":"));
            Add (new Operator ("lessThan", "<"));
            Add (new Operator ("greaterThan", ">"));
        }

        public static IEnumerable<Operator> Operators {
            get { return operators; }
        }

        private static void Add (Operator op)
        {
            operators.Add (op);
            by_op.Add (op.UserOperator, op);
            by_name.Add (op.Name, op);
        }

        public static Operator GetByUserOperator (string op)
        {
            return (by_op.ContainsKey (op)) ? by_op [op] : null;
        }

        public static Operator GetByName (string name)
        {
            return (by_name.ContainsKey (name)) ? by_name [name] : null;
        }

        public Operator (string name, string userOp)
        {
            Name = name;
            UserOperator = userOp;
        }
    }

    public class QueryTermNode : QueryNode
    {
        private string field;
        private string field_value;
        private Operator op;

        //private static string [] operators = new string [] {":", "==", "<=", ">=", "=", "<", ">"};

        public QueryTermNode() : base ()
        {
        }

        public QueryTermNode(string value) : base()
        {
            int field_separator = 0;
            foreach (Operator op in Operator.Operators) {
                field_separator = value.IndexOf (op.UserOperator);
                if (field_separator != -1) {
                    this.op = op;
                    break;
                }
            }

            if(field_separator > 0) {
                field = value.Substring(0, field_separator);
                this.field_value = value.Substring(field_separator + op.UserOperator.Length);
            } else {
                this.field_value = value;
                this.op = Operator.GetByUserOperator (":");
            }
        }

        public override QueryNode Trim ()
        {
            if ((field_value == null || field_value == String.Empty) && Parent != null)
                Parent.RemoveChild (this);
            return this;
        }
        
        public override string ToString()
        {
            if(field != null) {
                return String.Format("[{0}]=\"{1}\"", field, field_value);
            } else {
                return String.Format("\"{0}\"", Value);
            }
        }

        public override void AppendUserQuery (StringBuilder sb)
        {
            if (Field != null)
                sb.AppendFormat ("{0}{1}", Field, Operator.UserOperator);
            sb.Append (Value);
        }

        public override void AppendXml (XmlDocument doc, XmlNode parent)
        {
            XmlElement op_node = doc.CreateElement (op.Name);
            parent.AppendChild (op_node);

            if (Field != null) {
                XmlElement field = doc.CreateElement ("field");
                field.SetAttribute ("name", Field);
                op_node.AppendChild (field);
            }

            XmlElement val_node = doc.CreateElement ("string");
            val_node.InnerText = Value;
            op_node.AppendChild (val_node);
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
                return EmitStringMatch(sb, field, emit_or);
            else
                return EmitNumericMatch(sb, field, emit_or);
        }

        private bool EmitStringMatch(StringBuilder sb, QueryField field, bool emit_or)
        {
            string safe_value = (field.Modifier == null ? Value : field.Modifier (Value)).Replace("'", "''");
            
            if (emit_or)
                sb.Append (" OR ");

            switch (Operator.UserOperator) {
                case "=": // Starts with
                    sb.AppendFormat("{0} LIKE '{1}%'", field.Column, safe_value);
                    break;
                    
                case "==": // Equal to
                    sb.AppendFormat("{0} LIKE '{1}'", field.Column, safe_value);
                    break;

                case ":": // Contains
                default:
                    sb.AppendFormat("{0} LIKE '%{1}%'", field.Column, safe_value);
                    break;
            }

            return true;
        }

        private bool EmitNumericMatch(StringBuilder sb, QueryField field, bool emit_or)
        {
            long num = 0;
            
            if (!Int64.TryParse (field.Modifier == null ? Value : field.Modifier (Value), out num)) {
                return false;
            }
            
            if (emit_or) {
                sb.Append (" OR ");
            }
            
            switch (Operator.UserOperator) {
                case ":":
                case "=":
                case "==":
                    sb.AppendFormat("{0} = {1}", field.Column, num);
                    break;

                default:
                    sb.AppendFormat("{0} {2} {1}", field.Column, num, Operator.UserOperator);
                    break;
            }
            
            return true;
        }
        
        public string Value {
            get { return field_value; }
            set { field_value = value; }
        }

        public Operator Operator {
            get { return op; }
            set { op = value; }
        }
        
        public string Field {
            get { return field; }
            set { field = value; }
        }
    }
}
