/***************************************************************************
 *  Values.cs
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
using System.Collections.Generic;

namespace Abakos.Compiler
{
    public abstract class ValueSymbol : Symbol
    {
        private double value;

        protected ValueSymbol(string name) : this(name, 0.0)
        {
        }

        protected ValueSymbol(string name, double value) : base(name)
        {
            this.value = value;
        }

        public double Value {
            get { return value; }
        }
    }

    public class VariableSymbol : ValueSymbol
    {
        public VariableSymbol(string name) : base(name)
        {
        }

        public Symbol Resolve(IDictionary<string, double> table)
        {
            if(!table.ContainsKey(Name)) {
                throw new UnknownVariableException(Name);
            }

            return new NumberSymbol(table[Name]);
        }

        public new static VariableSymbol FromString(string token)
        {
            return new VariableSymbol(token);
        }
    }

    public class VoidSymbol : ValueSymbol
    {
        public VoidSymbol() : base("void")
        {
        }

        public static new VoidSymbol FromString(string token)
        {
            if(token == "void") {
                return new VoidSymbol();
            }

            throw new InvalidTokenException();
        }
    }

    public class NumberSymbol : ValueSymbol
    {
        public NumberSymbol(double value) : base(null, value)
        {
        }

        public override string ToString()
        {
            return Convert.ToString(Value);
        }

        public new static NumberSymbol FromString(string token)
        {
            return new NumberSymbol(Convert.ToDouble(token));
        }
    }
}
