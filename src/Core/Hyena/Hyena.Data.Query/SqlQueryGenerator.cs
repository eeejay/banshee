//
// SqlQueryGenerator.cs
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
using System.Text;
using System.Collections.Generic;

namespace Hyena.Data.Query
{
    public enum FieldType
    {
        Text,
        Numeric
    }

    public class Field
    {
        public string Name;
        public string [] Aliases;
        public string Column;
        public bool Default;
        public FieldType FieldType;

        public Field (string name, string column, FieldType type, params string [] aliases) : this (name, column, type, false, aliases)
        {
        }

        public Field (string name, string column, FieldType type, bool isDefault, params string [] aliases)
        {
            Name = name;
            Column = column;
            FieldType = type;
            Default = isDefault;
            Aliases = aliases;
        }
    }

    public class FieldSet
    {
        private Dictionary<string, Field> map = new Dictionary<string, Field> ();
        private IEnumerable<Field> fields;

        public FieldSet (params Field [] fields)
        {
            this.fields = fields;
            foreach (Field field in fields)
                foreach (string alias in field.Aliases)
                    map[alias.ToLower ()] = field;
        }

        public IEnumerable<Field> Fields {
            get { return fields; }
        }

        public Dictionary<string, Field> Map {
            get { return map; }
        }
    }

    public class SqlQueryGenerator
    {
        private QueryListNode root;
        private FieldSet field_set;
        
        private Stack<StringBuilder> builder_stack = new Stack<StringBuilder>();
        private StringBuilder builder;
        
        public SqlQueryGenerator()
        {
        }
        
        public SqlQueryGenerator(FieldSet fieldSet, QueryListNode query)
        {
            this.field_set = fieldSet;
            this.root = query;
        }
        
        public string GenerateQuery()
        {
            builder = new StringBuilder();
            builder_stack.Clear();
            builder_stack.Push(builder);
            
            Visit(root);
            return builder.ToString().Trim();
        }
        
        private void Visit(QueryNode node)
        {
            if(node is QueryKeywordNode) {
                VisitKeyword((QueryKeywordNode)node);
            } else if(node is QueryTermNode) {
                VisitTerm((QueryTermNode)node);
            } else if(node is QueryListNode) {
                QueryListNode list_node = (QueryListNode)node;
                
                builder_stack.Push(new StringBuilder());
                
                foreach(QueryNode child_node in list_node.Children) {
                    Visit(child_node);
                }
                
                StringBuilder branch_builder = builder_stack.Pop();
                string branch = branch_builder.ToString().Trim();
                
                if(String.IsNullOrEmpty(branch)) {
                    return;
                }
                
                if(list_node.ChildCount > 1) {
                    EmitOpenParen();
                    builder_stack.Peek().Append(branch);
                    EmitCloseParen();
                } else {
                    builder_stack.Peek().AppendFormat(" {0} ", branch);
                }
            }
        }
        
        private void VisitKeyword(QueryKeywordNode node)
        {
            if(!CheckLogicEmit(node)) {
                Console.WriteLine ("failed logic check");
                return;
            }
            
            switch(node.Keyword) {
                case Keyword.Not:
                    EmitNot();
                    break;
                case Keyword.Or:
                    EmitOr();
                    break;
                case Keyword.And:
                    EmitAnd();
                    break;
            }
        }
        
        private void VisitTerm(QueryTermNode node)
        {
            QueryNode left = node.Parent.GetLeftSibling(node); 
            if(left != null && left is QueryListNode) {
                EmitAnd();
            }

            string alias = node.Field;
            
            if(alias != null) {
                alias = alias.ToLower();
            }
            
            if(alias == null || !field_set.Map.ContainsKey(alias)) {
                EmitOpenParen();
                int emitted = 0, i = 0;
                
                foreach(Field field in field_set.Fields) {
                    if (field.Default)
                        if (EmitTermMatch (field, node.Value, emitted > 0))
                            emitted++;
                }
                
                EmitCloseParen();
            } else if(alias != null && field_set.Map.ContainsKey(alias)) {
                EmitTermMatch (field_set.Map[alias], node.Value, false);
            }
            
            QueryNode right = node.Parent.GetRightSibling(node); 
            if(right != null && !(right is QueryKeywordNode)) {
                QueryListNode grandparent = node.Parent.Parent;
                if(grandparent == null) {
                    EmitAnd();
                    return;
                }
                
                if(grandparent.LastChild != null) {
                    EmitAnd();
                }
            }
        }
        
        private bool CheckLogicEmit(QueryKeywordNode node)
        {
            QueryNode left = node.Parent.GetLeftSibling (node);
            QueryNode right = node.Parent.GetRightSibling (node);
            bool right_ok, left_ok;

            switch (node.Keyword) {
            case Keyword.Not:
                int index = node.Parent.IndexOfChild(node);
                if(index == node.Parent.ChildCount - 1) {
                    return false;
                }
                
                QueryNode left_left = (index < 2) ? null : node.Parent.Children[index - 2];

                // If we have a left sibling, it must be a keyword and it's left sibling must not be
                left_ok = (left == null) || (left is QueryKeywordNode && left_left != null && !(left_left is QueryKeywordNode));

                // Our right sibling cannot be a keyword
                right_ok = right != null && !(right is QueryKeywordNode);
                
                return left_ok && right_ok;

            case Keyword.And:
            case Keyword.Or:
                // We must have a non-keyword left sibling
                left_ok = (left != null && !(left is QueryKeywordNode));

                // We must have a right sibling that is either not a keyword or is the Not keyword (and it's right sibling isn't a keyword)
                QueryNode right_right = (right != null) ? node.Parent.GetRightSibling (right) : null;
                right_ok = (right != null && (!(right is QueryKeywordNode) ||
                           (right as QueryKeywordNode).Keyword == Keyword.Not && right_right != null && !(right_right is QueryKeywordNode)));

                if (left_ok && right_ok)
                    return true;
                
                // Not sure what this is for
                QueryListNode grandparent = node.Parent.Parent;
                if(grandparent != null && grandparent.LastChild != null) {
                    return true;
                }
                break;
            }
            
            return false;
        }
        
        private void EmitNot()
        {
            builder_stack.Peek().Append(" NOT ");
        }
        
        private void EmitOr()
        {
            builder_stack.Peek().Append(" OR ");
        }
        
        private void EmitAnd()
        {
            builder_stack.Peek().Append(" AND ");
        }
        
        private void EmitOpenParen()
        {
            builder_stack.Peek().Append(" ( ");
        }
        
        private void EmitCloseParen()
        {
            builder_stack.Peek().Append(" ) ");
        }
        
        private bool EmitTermMatch (Field field, string value, bool emit_or)
        {
            if (field.FieldType == FieldType.Text)
                return EmitStringMatch(field.Column, value, emit_or);
            else
                return EmitNumericMatch(field.Column, value, emit_or);
        }

        private bool EmitStringMatch(string field, string value, bool emit_or)
        {
            string safe_value = value.Replace('"', '\0');
            safe_value = value.Replace('\'', '\0');
            
            if (emit_or)
                EmitOr();
            builder_stack.Peek().AppendFormat(" {0} LIKE \"%{1}%\" ", field, safe_value);
            return true;
        }

        private bool EmitNumericMatch(string field, string value, bool emit_or)
        {
            try {
                int num = Convert.ToInt32 (value);
                if (emit_or)
                    EmitOr();
                builder_stack.Peek().AppendFormat(" {0} = {1} ", field, num);
                return true;
            } catch {}
            return false;
        }
        
        public FieldSet FieldSet {
            get { return field_set; }
            set { field_set = value; }
        }
        
        public QueryListNode Query {
            get { return root; }
            set { root = value; }
        }
    }
}
