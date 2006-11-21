/***************************************************************************
 *  GenericCollectionController.cs
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

namespace Banshee.Base
{
    public class GenericCollectionController<T> where T : class
    {
        protected Stack<T> advance_stack = new Stack<T>();
        protected Stack<T> regress_stack = new Stack<T>();
        protected T current;
        protected int index;
        
        private IList<T> data;

        public GenericCollectionController()
        {
        }

        public void Reset()
        {
            index = 0;
            advance_stack.Clear();
            regress_stack.Clear();
        }

        public T Advance()
        {
            T result = null;
            
            if(advance_stack.Count > 0) {
                result = advance_stack.Pop();
            } else {
                result = AdvanceFromPresent();
            }

            if(current != null) {
                regress_stack.Push(current);
            }
            
            current = result;
            return result;
        }
        
        private T AdvanceFromPresent()
        {
            return data[index++];
        }
        
        public T Regress()
        {
            T result = null;
            
            if(regress_stack.Count > 0) {
                result = regress_stack.Pop();
            } else {
                result = RegressFromPresent();
            }
            
            if(current != null) {
                advance_stack.Push(current);
            }
            
            current = result;
            return result;
        }
        
        private T RegressFromPresent()
        {
            return data[index--];
        }
        
        public IList<T> Data {
            get { return data; }
            set { data = value; } 
        }
    }
}
