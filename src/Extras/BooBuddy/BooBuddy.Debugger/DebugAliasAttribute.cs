/***************************************************************************
 *  DebugAliasAttribute.cs
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

namespace BooBuddy.Debugger
{
    public class DebugAliasAttribute : System.Attribute
    {
        private string alias;
        private string description;
        private string prefix;
        private MethodInfo method_info;
        private bool loaded;
        
        public DebugAliasAttribute(string alias) : this(alias, null)
        {
        }
        
        public DebugAliasAttribute(string alias, string description)
        {
            this.alias = alias;
            this.description = description;
        }
        
        public string Alias {
            get { return alias; }
            set { alias = value; }
        }
        
        public string Description {
            get { return description; }
            set { description = value; }
        }
        
        public string Prefix {
            get { return prefix; }
            set { prefix = value; }
        }
        
        public MethodInfo MethodInfo {
            get { return method_info; }
            set { method_info = value; }
        }
        
        public bool Loaded {
            get { return loaded; }
            set { loaded = value; }
        }
    }
    
    [AttributeUsage(AttributeTargets.Assembly)]
    public class DebugAliasAssemblyAttribute : System.Attribute
    {
    }
}
