//
// QueryOperator.cs
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

namespace Hyena.Query
{
    public class Operator
    {
        public string Name;
        public string UserOperator;

        public Operator Dual {
            get {
                string op = UserOperator.Replace ('>', '^');
                op = op.Replace ('<', '>');
                op = op.Replace ('^', '<');
                return GetByUserOperator (op);
            }
        }

        private bool is_not;
        public bool IsNot {
            get { return is_not; }
        }
        
        private static List<Operator> operators = new List<Operator> ();
        private static Dictionary<string, Operator> by_op = new Dictionary<string, Operator> ();
        private static Dictionary<string, Operator> by_name = new Dictionary<string, Operator> ();


        protected Operator (string name, string userOp) : this (name, userOp, false)
        {
        }

        protected Operator (string name, string userOp, bool is_not)
        {
            Name = name;
            UserOperator = userOp;
            this.is_not = is_not;
        }

        static Operator () {
            // Note, order of these is important since if = was before ==, the value of the
            // term would start with the second =, etc.
            Add (new Operator ("equals", "=="));
            Add (new Operator ("lessThanEquals", "<="));
            Add (new Operator ("greaterThanEquals", ">="));
            Add (new Operator ("notEqual", "!=", true));
            Add (new Operator ("endsWith", ":="));
            Add (new Operator ("startsWith", "="));
            Add (new Operator ("doesNotContain", "!:", true));
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

        public static Operator Default {
            get { return Operator.GetByUserOperator (":"); }
        }
    }
}
