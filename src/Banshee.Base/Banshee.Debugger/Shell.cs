/***************************************************************************
 *  Shell.cs
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

using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Pipelines;

using Gtk;

namespace Banshee.Debugger
{
    public class Shell : Gtk.Window
    {
        private TextView editor;
        private TextView output;
        
        public Shell() : base("Boo Compiler")
        {
            BorderWidth = 10;
            SetSizeRequest(400, 300);
            
            VPaned pane = new VPaned();
            
            VBox box = new VBox();
            box.Spacing = 10;
            
            ScrolledWindow editor_sw = new ScrolledWindow();
            ScrolledWindow output_sw = new ScrolledWindow();
            
            editor_sw.ShadowType = ShadowType.EtchedIn;
            output_sw.ShadowType = ShadowType.EtchedIn;
            
            output_sw.SetSizeRequest(-1, 80);
            
            editor = new TextView();
            output = new TextView();
            output.Editable = false;
            
            editor_sw.Add(editor);
            output_sw.Add(output);
            
            Button button = new Button("Run");
            button.Clicked += OnRunClicked;
            
            box.PackStart(editor_sw, true, true, 0);
            box.PackStart(button, false, false, 0);
            
            pane.Add1(box);
            pane.Add2(output_sw);
            
            Add(pane);
            pane.ShowAll();
        }
        
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
        
        private void CompilerOutput(string message, params object [] args)
        {
            output.Buffer.Text += String.Format(message, args) + "\n";
            output.ScrollToIter(output.Buffer.EndIter, 0.0, false, 0.0, 0.0);
        }
    }
}
