/***************************************************************************
 *  LogicFunctionSet.cs
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

namespace SExpEngine
{
    public class LogicFunctionSet : FunctionSet
    {
        [Function("if")]
        public virtual TreeNode OnIf(TreeNode [] args)
        {
            if(args == null || args.Length < 2 || args.Length > 3) {
                throw new ArgumentException("if accepts 2 or 3 arguments");
            } 
            
            TreeNode arg = Evaluate(args[0]);
            if(!(arg is BooleanLiteral)) {
                throw new ArgumentException("first if argument must be boolean");
            }
            
            BooleanLiteral conditional = (BooleanLiteral)arg;
            
            if(conditional.Value) {
                return args[1];
            } else if(args.Length == 3) {
                return args[2];
            }
            
            return new VoidLiteral();
        }
        
        [Function("not", "!")]
        public virtual TreeNode OnNot(TreeNode [] args)
        {
            if(args.Length != 1) {
                throw new ArgumentException("not must have only one argument");
            }
            
            TreeNode arg = Evaluate(args[0]);
            
            if(!(arg is BooleanLiteral)) {
                throw new ArgumentException("can only not a boolean");
            }
            
            return new BooleanLiteral(!(arg as BooleanLiteral).Value);
        }
        
        [Function("or", "|")]
        public virtual TreeNode OnOr(TreeNode [] args)
        {
            return OnAndOr(args, false);
        }
        
        [Function("and", "|")]
        public virtual TreeNode OnAnd(TreeNode [] args)
        {
            return OnAndOr(args, true);
        }
        
        private TreeNode OnAndOr(TreeNode [] args, bool and)
        {
            if(args.Length < 2) {
                throw new ArgumentException("must have two or more boolean arguments");
            }
            
            bool result = false;
            
            for(int i = 0; i < args.Length; i++) {
                TreeNode node = Evaluate(args[i]);
                if(!(node is BooleanLiteral)) {
                    throw new ArgumentException("arguments must be boolean");
                }
                
                BooleanLiteral arg = (BooleanLiteral)node;
                
                if(i == 0) {
                    result = arg.Value;
                    continue;
                }
                
                if(and) {
                    result &= arg.Value;
                } else {
                    result |= arg.Value;
                }
            }
            
            return new BooleanLiteral(result);
        }
    }
}
