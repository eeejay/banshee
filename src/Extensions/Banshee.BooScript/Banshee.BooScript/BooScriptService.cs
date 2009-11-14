//
// BooScriptService.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Reflection;

using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Pipelines;
using Boo.Lang.Interpreter;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.BooScript
{
    public class BooScriptService : IExtensionService
    {
        private static string scripts_directory = Path.Combine (Paths.ApplicationData, "boo-scripts");

        private bool initialized;

        void IExtensionService.Initialize ()
        {
            lock (this) {
                if (initialized) {
                    return;
                }

                Directory.CreateDirectory (scripts_directory);

                if (ApplicationContext.CommandLine.Contains ("run-scripts")) {
                    foreach (string file in ApplicationContext.CommandLine.Files) {
                        if (Path.GetExtension (file) == ".boo") {
                            RunBooScript (file);
                        }
                    }
                }

                foreach (string file in Directory.GetFiles (scripts_directory, "*.boo")) {
                    RunBooScript (file);
                }

                initialized = true;
            }
        }

        public void Dispose ()
        {
        }

#region Boo Scripting Engine

        private void RunBooScript (string file)
        {
            uint timer_id = Log.DebugTimerStart ();

            BooCompiler compiler = new BooCompiler ();
            compiler.Parameters.Ducky = true;
            compiler.Parameters.Pipeline = new CompileToMemory ();
            compiler.Parameters.Input.Add (new FileInput (file));

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies ()) {
                compiler.Parameters.References.Add (assembly);
            }

            CompilerContext context = compiler.Run ();

            if (context.GeneratedAssembly == null) {
                foreach (CompilerError error in context.Errors) {
                    Log.Warning (String.Format ("BooScript: compiler error: {0} ({1})",
                        error.ToString (), file), false);
                }

                return;
            }

            try {
                Type script_module = context.GeneratedAssembly.GetTypes ()[0];

                if (script_module == null) {
                    Log.Warning (String.Format ("BooScript: could not find module in script ({0})", file), false);
                    return;
                }

                MethodInfo main_entry = script_module.Assembly.EntryPoint;
                main_entry.Invoke (null, new object[main_entry.GetParameters ().Length]);

                Log.DebugTimerPrint (timer_id, "BooScript: compiled and invoked: {0}");
            } catch (Exception e) {
                Log.Exception ("BooScript: scripted failed", e);
            }
        }

#endregion

        string IService.ServiceName {
            get { return "BooScriptService"; }
        }
    }
}
