//
// QueryParser.cs
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
using System.IO;
using System.Text;

namespace Hyena.Data.Query
{
    public class QueryParser
    {
        private StreamReader reader;

        private QueryListNode root;
        private QueryListNode current_parent;
        
        private char peek;
        private int current_column;
        private int current_line;
        private int token_start_column;
        private int token_start_line;
        private bool eos_consumed;

        public QueryParser()
        {
            Reset();
        }

        public QueryParser(string inputQuery) : this(new MemoryStream(Encoding.UTF8.GetBytes(inputQuery)))
        {
        }

        public QueryParser(Stream stream) : this(new StreamReader(stream))
        {
        }

        public QueryParser(StreamReader reader) : this()
        {
            InputReader = reader;
        }

        public QueryListNode BuildTree()
        {
            current_parent = root = new QueryListNode();
            
            while(true) {
                QueryToken token = Scan();

                if(token.ID == TokenID.Unknown) {
                    break;
                }

                token.Column = token_start_column;
                token.Line = token_start_line;
                
                ParseToken(token);
            }
            
            return root;
        }
        
        private void DepthPush()
        {
            current_parent = new QueryListNode(current_parent);
        }
        
        private void DepthPop()
        {
            current_parent = current_parent.Parent;
        }
        
        private void NodePush(QueryNode node)
        {
            current_parent.AddChild(node);
        }
        
        private void ParseToken(QueryToken token)
        {
            switch(token.ID) {
                case TokenID.OpenParen:
                    DepthPush();
                    break;
                case TokenID.CloseParen:
                    DepthPop();
                    break;
                case TokenID.Not:
                    QueryNode left_sibling = current_parent.LastChild;
                    if(left_sibling != null && !(left_sibling is QueryKeywordNode)) {
                        NodePush(new QueryKeywordNode(Keyword.And));
                    }
                    
                    NodePush(new QueryKeywordNode(Keyword.Not));
                    break;
                case TokenID.Or:
                    NodePush(new QueryKeywordNode(Keyword.Or));
                    break;
                case TokenID.And:
                    NodePush(new QueryKeywordNode(Keyword.And));
                    break;
                case TokenID.Term:
                    NodePush(new QueryTermNode(token.Term));
                    break;
            }
        }

        private QueryToken Scan()
        {
            if (reader.EndOfStream) {
                if (eos_consumed)
                    return new QueryToken(TokenID.Unknown);
                else
                    eos_consumed = true;
            }
            
            for(; ; ReadChar()) {
                if(Char.IsWhiteSpace(peek) && peek != '\n') {
                    continue;
                } else if(peek == '\n') {
                    current_line++;
                    current_column = 0;
                } else {
                    break;
                }
            }

            token_start_column = current_column;
            token_start_line = current_line;

            if(peek == '(') {
                ReadChar();
                return new QueryToken(TokenID.OpenParen);
            } else if(peek == ')') {
                ReadChar();
                return new QueryToken(TokenID.CloseParen);
            } else if(peek == '-') {
                ReadChar();
                return new QueryToken(TokenID.Not);
            } else {
                string token = ScanString();

                if (reader.EndOfStream)
                    eos_consumed = true;
                
                switch(token) {
                    case "or": 
                    case "OR": 
                        return new QueryToken(TokenID.Or);
                    case "NOT":
                        return new QueryToken(TokenID.Not);
                    default:
                        return new QueryToken(token);
                }
            }
        }

        private bool IsStringTerminationChar(char ch, bool allow_whitespace)
        {
            return (!allow_whitespace && Char.IsWhiteSpace(ch)) || ch == '(' || ch == ')';
        }

        private string ScanString()
        {
            StringBuilder buffer = new StringBuilder();
            bool in_string = false;

            while(true) {
                if(IsStringTerminationChar(peek, in_string)) {
                    break;
                } else if(!in_string && peek == '"') {
                    in_string = true;
                } else if(in_string && peek == '"') {
                    in_string = false;
                } else {
                    buffer.Append(peek);
                    
                    if(reader.EndOfStream) {
                        break;
                    }
                }

                ReadChar();

                if(reader.EndOfStream) {
                    if(!IsStringTerminationChar(peek, false) && peek != '"') {
                        buffer.Append(peek);
                    }

                    break;
                }
            }
            
            return buffer.ToString();
        }

        public void Reset()
        {
            peek = ' ';
            current_column = 0;
            current_line = 0;
            token_start_column = 0;
            token_start_line = 0;
        }

        private void ReadChar()
        {
            if(peek == Char.MinValue) {
                return;
            }
            
            peek = (char)reader.Read();
            current_column++;
        }

        public StreamReader InputReader {
            get { return reader; }
            set { reader = value; }
        }
        
        public QueryListNode ParseTree {
             get { return root; }
        }
    }
}
