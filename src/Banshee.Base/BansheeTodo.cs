
/***************************************************************************
 *  BansheeTodo.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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

namespace Banshee.Base
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class BansheeTodo : Attribute 
    {
        private string comment;
        
        public BansheeTodo() : this("Unimplemented")
        {
        }

        public BansheeTodo(string comment) : base()
        {
            this.comment = comment;
        }

        public string Comment {
            get { 
                return comment; 
            }
        }
        
        public static void PrintReport()
        {
            PrintReport(Assembly.GetExecutingAssembly());
        }
        
        public static void PrintReport(Assembly asm)
        {
            Console.WriteLine("TODO Report for {0}", asm.FullName);
            
            foreach(Type type in asm.GetTypes()) {
                Attribute [] type_attrs = Attribute.GetCustomAttributes(type, typeof(BansheeTodo));

                foreach(BansheeTodo todo in type_attrs) {
                    Console.WriteLine("  {0}: {0}", type, todo.Comment);
                }
                
                foreach(MemberInfo member in type.GetMembers()) {
                    Attribute [] member_attrs = (Attribute [])member.GetCustomAttributes(typeof(BansheeTodo), false);
                    
                    foreach(BansheeTodo todo in member_attrs) {
                        Console.WriteLine("  {0}:{1} ({2}): {3}", type, member.Name, member.MemberType, todo.Comment);
                    }
                }
            }
        }
    }
}
