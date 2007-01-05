/***************************************************************************
 *  ScriptCore.cs
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
using System.Collections.Generic;
using System.Reflection;
using Mono.Unix;

using Banshee.Base;

using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Pipelines;
using Boo.Lang.Interpreter;

namespace Banshee.Plugins
{
    public delegate void ScriptInvocationHandler();
    
    public class ScriptInvocationEntry
    {
        public string Name;
        public string StockIcon;
        public string IconThemeName;
        public string Accelerator;
        public ScriptInvocationHandler Activate;
        
        public ScriptInvocationEntry()
        {
            Name = null;
            Activate = null;
            Accelerator = null;
            StockIcon = null;
            IconThemeName = null;
        }
        
        public ScriptInvocationEntry(string name) : this()
        {
            Name = name;
        }
        
        public ScriptInvocationEntry(string name, ScriptInvocationHandler activate) : this(name)
        {
            Activate = activate;
        }
        
        public ScriptInvocationEntry(string name, string accelerator, 
            ScriptInvocationHandler activate) : this(name, activate)
        {
            Accelerator = accelerator;
        }
        
        public ScriptInvocationEntry(string name, string accelerator) : this(name, accelerator, null)
        {
        }
    }
        
    public static class ScriptCore 
    {
        
        private static List<ScriptInvocationEntry> script_invocations = new List<ScriptInvocationEntry>();
    
        public static void Initialize()
        {
            try {
                Globals.ActionManager["ScriptsAction"].Visible = false;
                foreach(string file in Directory.GetFiles(Path.Combine(
                    Paths.ApplicationData, "scripts"), "*.boo")) {
                    RunBooScript(file);
                } 
            } catch {
            }
            
            Globals.UIManager.Initialized += OnInterfaceInitialized;
        }
        
        private static void RunBooScript(string file)
        {
            BooCompiler compiler = new BooCompiler();
            DateTime start = DateTime.Now;
            
            compiler.Parameters.Input.Add(new FileInput(file));
            compiler.Parameters.Pipeline = new CompileToMemory();
            compiler.Parameters.Ducky = true;

            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                compiler.Parameters.References.Add(assembly);
            }
            
            CompilerContext context = compiler.Run();
            
            if(context.GeneratedAssembly != null) {
                try {
                    Type script_module = context.GeneratedAssembly.GetTypes()[0];
                    if(script_module == null) {
                        BooScriptOutput(file, "Could not find module in script");
                    } else {
                        MethodInfo main_entry = script_module.Assembly.EntryPoint;
                        PluginCore.Factory.LoadPluginsFromAssembly(script_module.Assembly);
                        main_entry.Invoke(null, new object[main_entry.GetParameters().Length]);
                        
                        TimeSpan stop = DateTime.Now - start;
                        BooScriptOutput(file, String.Format("compiled/invoked ok {0}", stop));
                    }
                } catch(Exception e) {
                    BooScriptOutput(file, e.ToString());
                }
            } else {
                foreach(CompilerError error in context.Errors) {
                    BooScriptOutput(file, error.ToString());
                }
            }
        }
        
        private static void BooScriptOutput(string file, string output)
        {
            Console.WriteLine("BooCompiler: {0}: {1}", Path.GetFileName(file), output);
        }
        
        public static void InstallInvocation(string name, ScriptInvocationHandler handler)
        {
            InstallInvocation(new ScriptInvocationEntry(name, handler));
        }
        
        public static void InstallInvocation(ScriptInvocationEntry invocation)
        {
            if(invocation.Activate == null || invocation.Name == null) {
                throw new ArgumentNullException("Invocation must have a non null Name and Handler");
            }
            
            script_invocations.Add(invocation);
        }
        
        private static void OnInterfaceInitialized(object o, EventArgs args)
        {
            Gtk.MenuItem parent = Globals.ActionManager.GetWidget("/MainMenu/MusicMenu/Scripts") as Gtk.MenuItem;
            if(parent == null || script_invocations.Count <= 0) {
                return;
            }
            
            Globals.ActionManager["ScriptsAction"].Visible = true;
            Gtk.Menu scripts_menu = new Gtk.Menu();
            parent.Submenu = scripts_menu;
            
            foreach(ScriptInvocationEntry invocation in script_invocations) {
                Gtk.MenuItem item;
                
                if(invocation.StockIcon != null || invocation.IconThemeName != null) {
                    Gtk.Image image = new Gtk.Image();
                    
                    if(invocation.StockIcon != null) {
                        image.SetFromStock(invocation.StockIcon, Gtk.IconSize.Menu);
                    }
                    
                    if(invocation.IconThemeName != null) {
                        image.SetFromIconName(invocation.IconThemeName, Gtk.IconSize.Menu);
                    }
                    
                    item = new Gtk.ImageMenuItem(invocation.Name);
                    (item as Gtk.ImageMenuItem).Image = image;
                } else {
                    item = new Gtk.MenuItem(invocation.Name);
                }
                
                if(invocation.Accelerator != null) {
                    uint modifier_key;
                    Gdk.ModifierType modifier_type;
                    
                    Gtk.Accelerator.Parse(invocation.Accelerator, out modifier_key, out modifier_type);
                    item.AddAccelerator("activate", Globals.ActionManager.UI.AccelGroup, modifier_key, 
                        modifier_type, Gtk.AccelFlags.Visible);
                }
                    
                item.Activated += delegate { invocation.Activate(); };
                scripts_menu.Append(item);
            }
            
            scripts_menu.ShowAll();
        }
    }
}
