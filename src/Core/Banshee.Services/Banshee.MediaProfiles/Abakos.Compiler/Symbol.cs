/***************************************************************************
 *  Symbol.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Reflection;

namespace Abakos.Compiler
{
    public abstract class Symbol
    {
        private struct SymbolResolve
        {
            public Type Type;
            public MethodInfo Method;

            public SymbolResolve(Type type)
            {
                Type = type;
                Method = null;
            }
        }

        private static SymbolResolve [] symbol_resolution_table = {
            new SymbolResolve(typeof(OperatorSymbol)),
            new SymbolResolve(typeof(NumberSymbol)),
            new SymbolResolve(typeof(GroupSymbol)),
            new SymbolResolve(typeof(CommaSymbol)),
            new SymbolResolve(typeof(VoidSymbol)),
            new SymbolResolve(typeof(FunctionSymbol)),
            new SymbolResolve(typeof(VariableSymbol))
        };

        private string name;

        protected Symbol(string name)
        {
            this.name = name;
        }

        public string Name {
            get { return name; }
        }

        public override string ToString()
        {
            return name;
        }

        public static Symbol FromString(string token, string nextToken)
        {
            for(int i = 0; i < symbol_resolution_table.Length; i++) {
                SymbolResolve sr = symbol_resolution_table[i];

                if(sr.Method == null) {
                    sr.Method = sr.Type.GetMethod("FromString");
                    if(sr.Method == null) {
                        throw new ApplicationException(sr.Type + " does not have a FromString method");
                    }

                    symbol_resolution_table[i].Method = sr.Method;
                }

                if(nextToken != null) {
                    Symbol next_symbol = FromString(nextToken, null);
                    if(sr.Type == typeof(FunctionSymbol)) {
                        if((next_symbol is GroupSymbol && (next_symbol as GroupSymbol).Direction ==
                            GroupSymbol.GroupDirection.Right) || !(next_symbol is GroupSymbol)) {
                            continue;
                        }
                    }
                }

                try {
                    return (Symbol)sr.Method.Invoke(null, new object [] { token });
                } catch {
                }
            }

            throw new InvalidTokenException(token);
        }
    }
}
