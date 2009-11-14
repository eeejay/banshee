/***************************************************************************
 *  Operations.cs
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

namespace Abakos.Compiler
{
    public abstract class OperationSymbol : Symbol
    {
        private int precedence;

        protected OperationSymbol(string name, int precedence) : base(name)
        {
            this.precedence = precedence;
        }

        public int Precedence {
            get { return precedence; }
        }

        public static bool Compare(Symbol a, Symbol b)
        {
            OperationSymbol op_a = a as OperationSymbol;
            OperationSymbol op_b = b as OperationSymbol;

            if(op_a == null || op_b == null) {
                return false;
            }

            return !(op_a is GroupSymbol) && op_a.Precedence >= op_b.Precedence;
        }
    }

    public class GroupSymbol : OperationSymbol
    {
        public enum GroupDirection {
            Left,
            Right
        }

        public enum GroupType {
            Parenthesis,
            Brace,
            Bracket
        }

        private static readonly GroupSymbol [] symbol_table = {
            new GroupSymbol("(", GroupType.Parenthesis, GroupDirection.Left),
            new GroupSymbol("{", GroupType.Brace, GroupDirection.Left),
            new GroupSymbol("[", GroupType.Bracket, GroupDirection.Left),
            new GroupSymbol(")", GroupType.Parenthesis, GroupDirection.Right),
            new GroupSymbol("}", GroupType.Brace, GroupDirection.Right),
            new GroupSymbol("]", GroupType.Bracket, GroupDirection.Right)
        };

        private GroupDirection direction;
        private GroupType type;

        public GroupSymbol(string name, GroupType type, GroupDirection direction) : base(name, 5)
        {
            this.direction = direction;
            this.type = type;
        }

        public GroupDirection Direction {
            get { return direction; }
        }

        public GroupType Type {
            get { return type; }
        }

        public static GroupSymbol [] GroupSymbols {
            get { return symbol_table; }
        }

        public static new GroupSymbol FromString(string token)
        {
            foreach(GroupSymbol symbol in symbol_table) {
                if(symbol.Name == token) {
                    return symbol;
                }
            }

            throw new InvalidTokenException(token);
        }

        public static bool IsLeft(Symbol symbol)
        {
            GroupSymbol group = symbol as GroupSymbol;
            return group != null && group.Direction == GroupSymbol.GroupDirection.Left;
        }

        public static bool IsRight(Symbol symbol)
        {
            GroupSymbol group = symbol as GroupSymbol;
            return group != null && group.Direction == GroupSymbol.GroupDirection.Right;
        }
    }

    public class CommaSymbol : OperationSymbol
    {
        private static readonly CommaSymbol symbol = new CommaSymbol();

        public CommaSymbol() : base(",", 0)
        {
        }

        public static new CommaSymbol FromString(string token)
        {
            if(token == symbol.Name) {
                return symbol;
            }

            throw new InvalidTokenException(token);
        }
    }
}
