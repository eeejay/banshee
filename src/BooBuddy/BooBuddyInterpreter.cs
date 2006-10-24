/***************************************************************************
 *  BooBuddyInterpreter.cs
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
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Boo.Lang.Compiler;
using Boo.Lang.Interpreter;

namespace BooBuddy
{
    public class InterpreterResult
    {
        private string message;
        private List<string> errors = new List<string>();
        
        public InterpreterResult()
        {
        }
        
        public InterpreterResult(string message)
        {
            this.message = message;
        }
        
        public void PushError(string error)
        {
            errors.Add(error);
        }
        
        public string Message {
            get { return message; }
            set { message = value; }
        }
        
        public IList<string> Errors {
            get { return errors; }
        }
    }

    public class BooBuddyInterpreter
    {
        private InteractiveInterpreter interpreter;
        
        public BooBuddyInterpreter()
        {
            interpreter = new InteractiveInterpreter();
            interpreter.RememberLastValue = true;
            interpreter.Ducky = true;
            
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                interpreter.References.Add(assembly);
            }
        }
        
        public InterpreterResult Interpret(string block)
        {
            CompilerContext context;
            InterpreterResult result = new InterpreterResult();
            
            try {
                StringWriter captured_out_stream = new StringWriter();
                TextWriter old_out_stream = Console.Out;
            
                Console.SetOut(captured_out_stream);
                context = interpreter.Eval(block);
                Console.SetOut(old_out_stream);
                
                if(context.Errors != null && context.Errors.Count > 0) {
                    foreach(CompilerError error in context.Errors) {
                        result.PushError(error.ToString());
                    }
                }
                
                result.Message = captured_out_stream.ToString();
            } catch(Exception e) {
                if(e.InnerException != null) {
                    result.PushError(e.Message.ToString());
                } else {
                    result.PushError(String.Format("Execution exception: {0}", e.Message));
                }
            }
            
            return result;
        }
        
        public InteractiveInterpreter Interpreter {
            get { return interpreter; }
        }
    }
}
