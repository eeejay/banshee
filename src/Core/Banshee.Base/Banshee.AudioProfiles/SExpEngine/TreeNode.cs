/***************************************************************************
 *  TreeNode.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SExpEngine
{
    public class TreeNode
    {
        private List<TreeNode> children = new List<TreeNode>();
        private TreeNode parent;
        private int column;
        private int line;
        
        public TreeNode()
        {
        }

        public TreeNode(TreeNode parent)
        {
            this.parent = parent;
            parent.AddChild(this);
        }

        public void AddChild(TreeNode child)
        {
            child.Parent = this;
            children.Add(child);
        }
        
        public void Replace(TreeNode original, TreeNode node)
        {
            int index = children.IndexOf(original);
            if(index < 0) {
                children.Add(node);
            } else {
                children[index] = node;
            }
        }

        public TreeNode Parent {
            get { return parent; }
            set { parent = value; }
        }

        public int ChildCount {
            get { return children.Count; }
        }
        
        public bool HasChildren {
            get { return ChildCount > 0; }
        }
        
        public int Line {
            get { return line; }
            set { line = value; }
        }
        
        public int Column {
            get { return column; }
            set { column = value; }
        }

        public ReadOnlyCollection<TreeNode> Children {
            get { return new ReadOnlyCollection<TreeNode>(children); }
        }

        public static void DumpTree(TreeNode node)
        {
            DumpTree(node, 0);
        }

        private static void DumpTree(TreeNode node, int depth)
        {
            if(node is LiteralNodeBase || node is FunctionNode || node is VariableNode) {
                PrintIndent(depth, node);
            } else if(node != null) {
                foreach(TreeNode child in node.Children) {
                    DumpTree(child, depth + 1);
                }
            }
        }

        private static void PrintIndent(int depth, TreeNode node)
        {
            for(int i = 0; i < depth; i++) {
                Console.Write(" ");
            }

            if(node is FunctionNode) {
                Console.WriteLine("({0})[{1}]", (node as FunctionNode).Function,
                    node.Parent.ChildCount - 1);
            } else if(node is VariableNode) {
                Console.WriteLine("<{0}>", (node as VariableNode).Variable);
            } else {
                Console.WriteLine(node);
            }
        }
    }
}
