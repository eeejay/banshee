/***************************************************************************
 *  Expression.cs
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
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Abakos.Compiler
{
    public class Expression
    {
        private string raw_expression;
        private Queue<Symbol> infix_queue;
        private Queue<Symbol> postfix_queue;
        private Dictionary<string, double> variable_table = new Dictionary<string, double>();

        private static Regex tokenizer_regex = null;

        public Expression(string expression)
        {
            raw_expression = "(" + expression + ")";
            infix_queue = ToInfixQueue(raw_expression);
            postfix_queue = ToPostfixQueue(infix_queue);
        }

        public void DefineVariable(string variable, double value)
        {
            if(variable_table.ContainsKey(variable)) {
                variable_table[variable] = value;
            } else {
                variable_table.Add(variable, value);
            }
        }

        public Queue<Symbol> InfixQueue {
            get { return infix_queue; }
        }

        public Queue<Symbol> PostfixQueue {
            get { return postfix_queue; }
        }

        public string RawExpression {
            get { return raw_expression; }
        }

        public static Queue<Symbol> ToInfixQueue(string expression)
        {
            if(tokenizer_regex == null) {
                Queue<string> skipped = new Queue<string>();
                StringBuilder expr = new StringBuilder();
                expr.Append("([\\,");

                foreach(OperatorSymbol op in OperatorSymbol.Operators) {
                    if(op.Name.Length > 1){
                        skipped.Enqueue(op.Name);
                        continue;
                    }

                    expr.Append("\\");
                    expr.Append(op.Name);
                }

                foreach(GroupSymbol gp in GroupSymbol.GroupSymbols) {
                    expr.Append("\\");
                    expr.Append(gp.Name);
                }

                expr.Append("]{1}");

                while(skipped.Count > 0) {
                    expr.Append("|");
                    expr.Append(skipped.Dequeue());
                }

                expr.Append(")");

                tokenizer_regex = new Regex(expr.ToString(), RegexOptions.IgnorePatternWhitespace);
            }

            string [] tokens = tokenizer_regex.Split(expression);
            List<Symbol> symbol_list = new List<Symbol>();

            for(int i = 0; i < tokens.Length; i++) {
                string token = tokens[i].Trim();
                string next_token = null;

                if(token == String.Empty) {
                    continue;
                }

                if(i < tokens.Length - 1) {
                    next_token = tokens[i + 1].Trim();
                }

                Symbol symbol = Symbol.FromString(token, next_token);
                symbol_list.Add(symbol);

                // I'm too tired to fix this at the stack/execution level right now;
                // injecting void at the parsing level for zero-arg functions
                if(symbol_list.Count >= 3 && GroupSymbol.IsRight(symbol) &&
                    GroupSymbol.IsLeft(symbol_list[symbol_list.Count - 2]) &&
                    symbol_list[symbol_list.Count - 3] is FunctionSymbol) {
                    symbol_list.Insert(symbol_list.Count - 1, new VoidSymbol());
                }
            }

            Queue<Symbol> queue = new Queue<Symbol>();
            foreach(Symbol symbol in symbol_list) {
                queue.Enqueue(symbol);
            }

            return queue;
        }

        public static Queue<Symbol> ToPostfixQueue(Queue<Symbol> infix)
        {
            Stack<Symbol> stack = new Stack<Symbol>();
            Queue<Symbol> postfix = new Queue<Symbol>();
            Symbol temp_symbol;

            foreach(Symbol symbol in infix) {
                if(symbol is ValueSymbol) {
                    postfix.Enqueue(symbol);
                } else if(GroupSymbol.IsLeft(symbol)) {
                    stack.Push(symbol);
                } else if(GroupSymbol.IsRight(symbol)) {
                    while(stack.Count > 0) {
                        temp_symbol = stack.Pop();
                        if(GroupSymbol.IsLeft(temp_symbol)) {
                            break;
                        }

                        postfix.Enqueue(temp_symbol);
                    }
                } else {
                    if(stack.Count > 0) {
                        temp_symbol = stack.Pop();
                        while(stack.Count != 0 && OperationSymbol.Compare(temp_symbol, symbol)) {
                            postfix.Enqueue(temp_symbol);
                            temp_symbol = stack.Pop();
                        }

                        if(OperationSymbol.Compare(temp_symbol, symbol)) {
                            postfix.Enqueue(temp_symbol);
                        } else {
                            stack.Push(temp_symbol);
                        }
                    }

                    stack.Push(symbol);
                }
            }

            while(stack.Count > 0) {
                postfix.Enqueue(stack.Pop());
            }

            return postfix;
        }

        public Stack<Symbol> Evaluate()
        {
            return EvaluatePostfix(postfix_queue, variable_table);
        }

        public double EvaluateNumeric()
        {
            return ((NumberSymbol)Evaluate().Pop()).Value;
        }

        public static Stack<Symbol> EvaluatePostfix(Queue<Symbol> postfix, IDictionary<string, double> variableTable)
        {
            Stack<Symbol> stack = new Stack<Symbol>();

            foreach(Symbol current_symbol in postfix) {
                if(current_symbol is VariableSymbol) {
                    stack.Push((current_symbol as VariableSymbol).Resolve(variableTable));
                } else if(current_symbol is ValueSymbol || current_symbol is CommaSymbol) {
                    stack.Push(current_symbol);
                } else if(current_symbol is OperatorSymbol) {
                    Symbol a = stack.Pop();
                    Symbol b = stack.Pop();
                    stack.Push((current_symbol as OperatorSymbol).Evaluate(b, a));
                } else if(current_symbol is FunctionSymbol) {
                    Stack<Symbol> argument_stack = new Stack<Symbol>();

                    if(stack.Count > 0) {
                        Symbol argument = stack.Pop();

                        if(!(argument is CommaSymbol) && !(argument is VoidSymbol)) {
                            argument_stack.Push(argument);
                        } else if(argument is CommaSymbol) {
                            while(argument is CommaSymbol) {
                                argument = stack.Pop();
                                argument_stack.Push(argument);

                                argument = stack.Pop();
                                if(!(argument is CommaSymbol)) {
                                    argument_stack.Push(argument);
                                }
                            }
                        }
                    }

                    stack.Push(FunctionTable.Execute(current_symbol as FunctionSymbol, argument_stack));
                }
            }

            return stack;
        }
    }
}
