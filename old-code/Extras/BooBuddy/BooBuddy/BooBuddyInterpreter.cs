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

using Boo.Lang;
using Boo.Lang.Compiler;
using Boo.Lang.Interpreter;

using BooBuddy.Debugger;

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
    
    public delegate void InterpreterResultHandler(InterpreterResult result);

    public class BooBuddyInterpreter : InteractiveInterpreter
    {
        private DebugAliasBuilder alias_builder;
        private static string compiler_version;
        
        public event InterpreterResultHandler HaveInterpreterResult;
    
        public BooBuddyInterpreter()
        {
            if(compiler_version == null) {
                compiler_version = Builtins.BooVersion.ToString();
            }
        
            RememberLastValue = true;
            Ducky = true;
            
            alias_builder = new DebugAliasBuilder();
            
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                References.Add(assembly);
                alias_builder.LoadFromAssembly(assembly);
            }
            
            Assembly asm = typeof(BooBuddyInterpreter).Assembly;
            Stream stream = asm.GetManifestResourceStream("AliasMacro.boo");
            
            if(stream != null) {
                using(StreamReader reader = new StreamReader(stream)) {
                    Interpret(reader.ReadToEnd());
                }
            }
        }
        
        public void InitializeDebugging()
        {
            string procname = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            procname = Convert.ToString(procname[0]).ToUpper() + procname.Substring(1);
            
            alias_builder.BuildAliases(this);
            Interpret(String.Format(
                "print \"Welcome to BooBuddy for {0}. Run p_help() for debugging " + 
                "help or start writing Boo code against all loaded assembly APIs\\n" + 
                "Boo Version {1}\"",
                procname, compiler_version), true);
        }
        
        public InterpreterResult Interpret(string block)
        {
            return Interpret(block, false);
        }
        
        public InterpreterResult Interpret(string block, bool raise)
        {
            CompilerContext context;
            InterpreterResult result = new InterpreterResult();
            
            try {
                StringWriter captured_out_stream = new StringWriter();
                TextWriter old_out_stream = Console.Out;
            
                Console.SetOut(captured_out_stream);
                context = Eval(block);
                Console.SetOut(old_out_stream);
                
                if(context.Errors != null && context.Errors.Count > 0) {
                    foreach(CompilerError error in context.Errors) {
                        result.PushError(error.ToString());
                    }
                }
                
                result.Message = captured_out_stream.ToString();
            } catch(Exception e) {
                if(e.InnerException != null) {
                    result.PushError(e.InnerException.Message.ToString());
                } else {
                    result.PushError(String.Format("Execution exception: {0}", e.Message));
                }
            }
            
            if(raise) {
                InterpreterResultHandler handler = HaveInterpreterResult;
                if(handler != null) {
                    handler(result);
                }
            }
            
            return result;
        }
    }
}
