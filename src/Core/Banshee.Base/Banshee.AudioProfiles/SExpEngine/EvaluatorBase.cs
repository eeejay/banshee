/***************************************************************************
 *  EvaluatorBase.cs
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
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SExpEngine
{
    public delegate TreeNode SExpFunctionHandler(EvaluatorBase evaluator, TreeNode [] args);
    public delegate TreeNode SExpVariableResolutionHandler(TreeNode node);

    public class EvaluationException : ApplicationException
    {
        public EvaluationException(TreeNode node, string token, Exception inner) : base(String.Format(
            "Evaluation exception at token `{0} ({1})' [{2},{3}]", 
            node.GetType(), token, node.Line, node.Column), inner)
        {
        }
    }
    
    public class UnknownVariableException : ApplicationException
    {
        public UnknownVariableException(string var) : base(var)
        {
        }
    }

    public class EvaluatorBase
    {
        private class MethodInfoContainer
        {
            public object Object;
            public MethodInfo MethodInfo;
        }
    
        private TreeNode expression;
        private string input;
        private Dictionary<string, object> functions = new Dictionary<string, object>();
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private List<Exception> exceptions = new List<Exception>();
        
        public EvaluatorBase()
        {
        }
        
        public EvaluatorBase(TreeNode expression)
        {
            this.expression = expression;
        }
        
        public EvaluatorBase(string input)
        {
            this.input = input;
        }
        
        public void ClearVariables()
        {
            variables.Clear();
        }
        
        public void RegisterVariable(string name, string value)
        {
            variables.Add(name, value);
        }
        
        public void RegisterVariable(string name, bool value)
        {
            variables.Add(name, value);
        }
        
        public void RegisterVariable(string name, int value)
        {
            variables.Add(name, value);
        }
        
        public void RegisterVariable(string name, double value)
        {
            variables.Add(name, value);
        }
        
        public void RegisterVariable(string name, SExpVariableResolutionHandler value)
        {
            variables.Add(name, value);
        }
        
        public void RegisterFunction(SExpFunctionHandler handler, params string [] names)
        {
            foreach(string name in names) {
                if(functions.ContainsKey(name)) {
                    functions.Remove(name);
                }
                
                functions.Add(name, handler);
            }
        }
        
        public void RegisterFunction(object o, MethodInfo method, string [] names)
        {
            MethodInfoContainer container = new MethodInfoContainer();
            container.MethodInfo = method;
            container.Object = o;
        
            foreach(string name in names) {
                if(functions.ContainsKey(name)) {
                    functions.Remove(name);
                }
                
                functions.Add(name, container);
            }
        }
        
        public void RegisterFunctionSet(FunctionSet functionSet)
        {
            functionSet.Load(this);
        }
        
        public TreeNode EvaluateTree(TreeNode expression)
        {
            this.expression = expression;
            this.input = null;
            return Evaluate();
        }
        
        public TreeNode EvaluateString(string input)
        {
            this.expression = null;
            this.input = input;
            return Evaluate();
        }
        
        public TreeNode Evaluate()
        {
            exceptions.Clear();
            
            try {
                if(expression == null) {
                    Parser parser = new Parser(input);
                    expression = parser.Parse();
                }
                
                return Evaluate(expression);
            } catch(Exception e) {
                Exception next = e;
                
                do {
                    if(next != null) {
                        exceptions.Add(next);
                        next = next.InnerException;
                    }
                } while(next != null && next.InnerException != null);
            
                if(next != null) {
                    exceptions.Add(next);
                }
            }
            
            return null;
        }
        
        public bool Success {
            get { return exceptions.Count == 0; }
        }
        
        public TreeNode ExpressionTree {
            get { return expression; }
        }
        
        public ReadOnlyCollection<Exception> Exceptions {
            get { return new ReadOnlyCollection<Exception>(exceptions); }
        }
        
        public string ErrorMessage {
            get {
                if(exceptions.Count == 0) {
                    return null;
                } else if(exceptions.Count >= 2) {
                    return String.Format("{0}: {1}", exceptions[exceptions.Count - 2].Message, 
                        exceptions[exceptions.Count - 1].Message);
                }
                
                return exceptions[0].Message;
            }
        }
        
        internal TreeNode Evaluate(TreeNode node)
        {
            TreeNode result_node = node;
            
            while(result_node.HasChildren) {
                result_node = EvaluateChildren(result_node);
            }
            
            return result_node;
        }
        
        private TreeNode EvaluateChildren(TreeNode node)
        {
            TreeNode result_node = null;
            TreeNode first_child = node.Children[0];
            
            if(first_child is FunctionNode) {
                try {
                    result_node = EvaluateFunction(node, first_child as FunctionNode);
                } catch(Exception e) {
                    Exception ee = e;
                    if(e is TargetInvocationException) {
                        ee = e.InnerException;
                    }
                    
                    throw new EvaluationException(first_child, 
                        (first_child as FunctionNode).Function, ee);
                }
            } else if(first_child is VariableNode) {
                result_node = EvaluateVariable(first_child as VariableNode);
            } else if(first_child is LiteralNodeBase) {
                result_node = first_child;
            } else if(first_child.HasChildren) {
                result_node = EvaluateChildren(first_child);
            }
            
            return result_node;
        }
        
        private TreeNode EvaluateFunction(TreeNode parent, FunctionNode node)
        {
            TreeNode [] args = new TreeNode[parent.ChildCount - 1];
            for(int i = 0; i < args.Length; i++) {
                args[i] = parent.Children[i + 1];
                if(args[i] is VariableNode) {
                    args[i] = EvaluateVariable((VariableNode)args[i]);
                }
            }
            
            if(functions.ContainsKey(node.Function)) {
                object handler = functions[node.Function];
                
                if(handler is SExpFunctionHandler) {
                    return ((SExpFunctionHandler)handler)(this, args);
                } else if(handler is MethodInfoContainer) {
                    MethodInfoContainer container = (MethodInfoContainer)handler;
                    return (TreeNode)container.MethodInfo.Invoke(container.Object, new object [] { args });
                } else {
                    throw new InvalidFunctionException(String.Format(
                        "Unknown runtime method handler type {1}", handler.GetType()));
                }
            }
            
            throw new InvalidFunctionException(node.Function);
        }
        
        private TreeNode EvaluateVariable(VariableNode node)
        {
            if(!variables.ContainsKey(node.Variable)) {
                throw new UnknownVariableException(node.Variable);
            }
            
            object resolver = variables[node.Variable];
            
            if(resolver is string) {
                return new StringLiteral((string)resolver);
            } else if(resolver is double) {
                return new DoubleLiteral((double)resolver);
            } else if(resolver is int) {
                return new IntLiteral((int)resolver);
            } else if(resolver is bool) {
                return new BooleanLiteral((bool)resolver);
            } else if(resolver is string) {
                return ((SExpVariableResolutionHandler)resolver)(node);
            }
            
            throw new UnknownVariableException(String.Format("Unknown variable type `{0}' for variable `{1}'",
                resolver.GetType(), node.Variable));
        }
    }
}
