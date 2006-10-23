/***************************************************************************
 *  BooBuddyWindow.cs
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
using System.Reflection;

using Gtk;

using Boo.Lang.Compiler;
using Boo.Lang.Interpreter;

namespace BooBuddy
{
    public class BooBuddyWindow : Window
    {
        private BooBuddyShell interpreter_shell;
        private InteractiveInterpreter interpreter = new InteractiveInterpreter();
        
        public BooBuddyWindow() : base("Boo Buddy")
        {
            interpreter.Ducky = true;
            
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                interpreter.References.Add(assembly);
            }
            
            BorderWidth = 5;
            SetSizeRequest(600, 250);
            WindowPosition = WindowPosition.Center;
            
            Notebook notebook = new Notebook();
            
            ScrolledWindow interpreter_sw = new ScrolledWindow();
            interpreter_sw.ShadowType = ShadowType.In;
            
            interpreter_shell = new BooBuddyShell();
            interpreter_shell.ProcessInput += OnProcessInput;
            interpreter_sw.Add(interpreter_shell);
            
            notebook.AppendPage(interpreter_sw, new Label("Interpreter"));
            
            Add(notebook);
            notebook.ShowAll();
        }
        
        private void OnProcessInput(object o, ProcessInputArgs args)
        {
            CompilerContext context;
            
            try {            
                context = interpreter.Eval(args.Input);
                if(context.Errors != null && context.Errors.Count > 0) {
                    StringBuilder error_builder = new StringBuilder();

                    foreach(CompilerError error in context.Errors) {
                        error_builder.Append(error.ToString() + "\n");
                    }
                    
                    interpreter_shell.SetResult(error_builder.ToString(), true);
                } else {
                    interpreter_shell.SetResult();
                }
            } catch(Exception e) {
                interpreter_shell.SetResult("Exception: " + e.Message + ". See stderr for stacktrace.");
                Console.Error.WriteLine(e);
            }
        }
        
        /*
        
        TODO: Use this as a start for the scripted/compiled Boo Buddy pane
        
        private void OnRunClicked(object o, EventArgs args)
        {
            BooCompiler compiler = new BooCompiler();
            
            compiler.Parameters.Input.Add(new StringInput("Script", editor.Buffer.Text));
            compiler.Parameters.Pipeline = new CompileToMemory();
            compiler.Parameters.Ducky = true;
            compiler.Parameters.References.Add(Assembly.GetExecutingAssembly());
            
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                compiler.Parameters.References.Add(assembly);
            }
            
            CompilerContext context = compiler.Run();
            
            if(context.GeneratedAssembly != null) {
                try {
                    Type script_module = context.GeneratedAssembly.GetType("ScriptModule");
                    MethodInfo main_entry = script_module.GetMethod("Main");
                    if(main_entry == null) {
                        CompilerOutput("Module is missing static Main method");
                    } else {
                        main_entry.Invoke(null, null);
                    }
                } catch(Exception e) {
                    CompilerOutput(e.ToString());
                }
            } else {
                foreach(CompilerError error in context.Errors) {
                    CompilerOutput(error.ToString());
                }
            }
        }
        
        */
    }
}
