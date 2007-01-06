/***************************************************************************
 *  FunctionSet.cs
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

namespace SExpEngine
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : Attribute
    {
        private string [] names;
        
        public FunctionAttribute(params string [] names)
        {
            this.names = names;
        }
        
        public string [] Names {
            get { return names; }
        }
    }
    
    public abstract class FunctionSet
    {
        private EvaluatorBase evaluator;
        
        public void Load(EvaluatorBase evaluator)
        {
            this.evaluator = evaluator;
            
            foreach(MethodInfo method in GetType().GetMethods()) {
                string [] names = null;
                
                foreach(Attribute attr in method.GetCustomAttributes(false)) {
                    if(attr is FunctionAttribute) {
                        names = (attr as FunctionAttribute).Names;
                        break;
                    }
                }
                
                if(names == null || names.Length == 0) {
                    continue;
                }
                
                evaluator.RegisterFunction(this, method, names);
            }
        }
        
        public TreeNode Evaluate(TreeNode node)
        {
            return evaluator.Evaluate(node);
        }
        
        protected EvaluatorBase Evaluator {
            get { return evaluator; }
        }
    }
}
