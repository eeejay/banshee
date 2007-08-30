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
    public class SqlQueryGenerator
    {
        private QueryListNode root;
        private Dictionary<string, string> field_map = new Dictionary<string, string>();
        
        private Stack<StringBuilder> builder_stack = new Stack<StringBuilder>();
        private StringBuilder builder;
        
        public SqlQueryGenerator()
        {
        }
        
        public SqlQueryGenerator(Dictionary<string, string> fieldMap, QueryListNode query)
        {
            this.field_map = fieldMap;
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
            string field = node.Field;
            
            if(field != null) {
                field = field.ToLower();
            }
            
            if(field == null || !field_map.ContainsKey(field)) {
                EmitOpenParen();
                int i = 0;
                
                foreach(KeyValuePair<string, string> map_item in field_map) {
                    EmitStringMatch(map_item.Value, node.Value);
                    if(i++ < field_map.Count - 1) {
                        EmitOr();
                    }
                }
                
                EmitCloseParen();
            } else if(field != null && field_map.ContainsKey(field)) {
                EmitStringMatch(field_map[field], node.Value);
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
        
        private bool CheckLogicEmit(QueryNode node)
        {
            if(node is QueryKeywordNode && ((QueryKeywordNode)node).Keyword == Keyword.Not) {
                int index = node.Parent.IndexOfChild(node);
                if(index < 2) {
                    return false;
                }
                
                QueryNode left = node.Parent.Children[index - 1];
                QueryNode left_left = node.Parent.Children[index - 2];
                QueryNode right = node.Parent.GetRightSibling(node);
                
                return left is QueryKeywordNode && left_left != null && !(left_left is QueryKeywordNode) &&
                    right != null && !(right is QueryKeywordNode);
            }
            
            QueryNode _left = node.Parent.GetLeftSibling(node);
            QueryNode _right = node.Parent.GetRightSibling(node);
            
            QueryListNode grandparent = node.Parent.Parent;
                
            
            bool mah = _left != null && !(_left is QueryKeywordNode) && _right != null && !(_right is QueryKeywordNode);
                if(mah) {
                    return true;
                }
                
                if(grandparent != null && grandparent.LastChild != null) {
                    return true;
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
        
        private void EmitStringMatch(string field, string value)
        {
            string safe_value = value.Replace('"', '\0');
            safe_value = value.Replace('\'', '\0');
            
            builder_stack.Peek().AppendFormat(" {0} LIKE \"%{1}%\" ", field, safe_value);
        }
        
        public Dictionary<string, string> FieldMap {
            get { return field_map; }
            set { field_map = value; }
        }
        
        public QueryListNode Query {
            get { return root; }
            set { root = value; }
        }
    }
}
