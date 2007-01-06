/***************************************************************************
 *  Parser.cs
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
using System.Text.RegularExpressions;

namespace SExpEngine
{
    public class ParserException : ApplicationException
    {
        public ParserException(string token, int line, int col, Exception inner) : base(String.Format(
            "Parser exception at token `{0}' [{1},{2}]", 
            token, line, col), inner)
        {
        }
    }

    public class Parser
    {
        private bool in_string;
        private string input;
        
        private TreeNode current_parent;
        private TreeNode root_node;
        private int line;
        private int col;
        
        private string current_token;
        
        public Parser(string input)
        {
            root_node = new TreeNode();
            current_parent = root_node;

            this.input = input;
        }

        public TreeNode Parse()
        {
            try {
                return InnerParse();
            } catch(Exception e) {
                throw new ParserException(current_token, line, col, e);
            }
        }

        private TreeNode InnerParse()
        {   
            for(int i = 0; i < input.Length; i++) {
                switch(input[i]) {
                    case '(':
                        ParseToken();
                        current_parent = new TreeNode(current_parent);
                        break;
                    case ')':
                        ParseToken();
                        current_parent = current_parent.Parent;
                        break;
                    case '"':
                        if(in_string) {
                            if(current_token == null) {
                                current_token = String.Empty;
                            }
                            
                            in_string = false;
                            ParseToken(true);
                        } else {
                            in_string = true;
                        }
                        break;
                    case '\\':
                        char next = input[i + 1];
                        if(next == '"' || next == '(' || next == ')') {
                            current_token += next;
                            i++;
                        }
                        break;
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        if(in_string) {
                            current_token += input[i];
                        } else {
                            ParseToken();
                        } 
                        continue;
                    default:
                        current_token += input[i];
                        break;    
                }
                
                if(input[i] == '\n') {
                    line++;
                    col = 0;
                } else {
                    col++;
                }
            }

            ParseToken();

            return root_node;
        }

        private void ParseToken()
        {
            ParseToken(false);
        }

        private void ParseToken(bool is_string)
        {
            if(current_token == null) {
                return;
            }
            
            TreeNode node;
            
            if(is_string) {
                node = new StringLiteral(current_token);
            } else if(Regex.IsMatch(current_token, @"^[A-Za-z_\\+\\\-\\*\\/\\%\\!\\=\\<\\>]+$") && 
                current_parent.ChildCount == 0) {
                node = new FunctionNode(current_token);
            } else if(current_token == "#t") {
                node = new BooleanLiteral(true);
            } else if(current_token == "#f") {
                node = new BooleanLiteral(false);
            } else if(current_token.StartsWith("$")) {
                node = new VariableNode(current_token.Substring(1).Trim());
            } else if(current_token.Contains(".")) {
                node = new DoubleLiteral(Double.Parse(current_token));
            } else {
                node = new IntLiteral(Int32.Parse(current_token));
            } 
            
            node.Line = line;
            node.Column = col - current_token.Length;

            current_parent.AddChild(node);
            current_token = null;
        }
    }
}
