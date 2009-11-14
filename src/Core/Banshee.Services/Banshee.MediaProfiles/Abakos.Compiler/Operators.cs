/***************************************************************************
 *  Operators.cs
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
    public abstract class OperatorSymbol : OperationSymbol
    {
        private readonly static OperatorSymbol [] symbol_table = {
            new PowOperatorSymbol(),
            new DivOperatorSymbol(),
            new MulOperatorSymbol(),
            new ModOperatorSymbol(),
            new AddOperatorSymbol(),
            new SubOperatorSymbol(),
            new LShiftOperatorSymbol(),
            new RShiftOperatorSymbol()
        };

        public OperatorSymbol(string name, int precedence) : base(name, precedence)
        {
        }

        public virtual Symbol Evaluate(Symbol a, Symbol b)
        {
            if(a is VoidSymbol || b is VoidSymbol) {
                throw new InvalidOperandException(a, b);
            }

            try {
                return Evaluate((ValueSymbol)a, (ValueSymbol)b);
            } catch {
                throw new InvalidOperandException(a, b);
            }
        }

        protected virtual Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol(0);
        }

        public static OperatorSymbol [] Operators {
            get { return symbol_table; }
        }

        public static new OperatorSymbol FromString(string token)
        {
            foreach(OperatorSymbol symbol in symbol_table) {
                if(symbol.Name == token) {
                    return symbol;
                }
            }

            throw new InvalidTokenException(token);
        }
    }

    public class PowOperatorSymbol : OperatorSymbol
    {
        public PowOperatorSymbol() : base("^", 3)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol(Math.Pow(a.Value, b.Value));
        }
    }

    public class DivOperatorSymbol : OperatorSymbol
    {
        public DivOperatorSymbol() : base("/", 2)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol(a.Value / b.Value);
        }
    }

    public class MulOperatorSymbol : OperatorSymbol
    {
        public MulOperatorSymbol() : base("*", 2)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol(a.Value * b.Value);
        }
    }

    public class ModOperatorSymbol : OperatorSymbol
    {
        public ModOperatorSymbol() : base("%", 2)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol((int)a.Value % (int)b.Value);
        }
    }

    public class AddOperatorSymbol : OperatorSymbol
    {
        public AddOperatorSymbol() : base("+", 1)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol(a.Value + b.Value);
        }
    }

    public class SubOperatorSymbol : OperatorSymbol
    {
        public SubOperatorSymbol() : base("-", 1)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol(a.Value - b.Value);
        }
    }

    public class LShiftOperatorSymbol : OperatorSymbol
    {
        public LShiftOperatorSymbol() : base("<<", 1)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol((int)a.Value << (int)b.Value);
        }
    }

    public class RShiftOperatorSymbol : OperatorSymbol
    {
        public RShiftOperatorSymbol() : base(">>", 1)
        {
        }

        protected override Symbol Evaluate(ValueSymbol a, ValueSymbol b)
        {
            return new NumberSymbol((int)a.Value >> (int)b.Value);
        }
    }
}

