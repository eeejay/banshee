/***************************************************************************
 *  DebugAliasBuilder.cs
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
using System.Collections.Generic;

[assembly:BooBuddy.Debugger.DebugAliasAssembly]
    
namespace BooBuddy.Debugger
{
    public class DebugAliasBuilder
    {
        private static List<DebugAliasAttribute> aliases = new List<DebugAliasAttribute>();
        private static List<Assembly> assemblies = new List<Assembly>();
        
        public DebugAliasBuilder()
        {
        }
    
        public void LoadFromAssembly(Assembly assembly)
        {
            if(assemblies.Contains(assembly)) {
                return;
            }
        
            foreach(Attribute attr in assembly.GetCustomAttributes(false)) {
                if(attr is DebugAliasAssemblyAttribute) {
                    SetupDebugAliasMethods(assembly);
                }
            }
            
            assemblies.Add(assembly);
        }
        
        public void BuildAliases(BooBuddyInterpreter interpreter)
        {
            foreach(DebugAliasAttribute attr in aliases) {
                if(attr.Loaded) {
                    continue;
                }
                Type class_type = attr.MethodInfo.DeclaringType;
                string prefix = String.Empty;
                
                foreach(Attribute class_attr in class_type.GetCustomAttributes(false)) {
                    if(class_attr is DebugAliasAttribute) {
                        prefix = (class_attr as DebugAliasAttribute).Alias + "_";
                        break;
                    }
                }
                
                attr.Prefix = prefix;
            
                string block = 
                    "def " + attr.Prefix + attr.Alias + "():\n" +
                    "\t" + class_type.FullName + "." + attr.MethodInfo.Name + "()\n"
                ;
                
                if(interpreter.Interpret(block).Errors.Count == 0) {
                    attr.Loaded = true;
                }
            }
        }
    
        private void SetupDebugAliasMethods(Assembly assembly)
        {
            foreach(Type type in assembly.GetTypes()) {
                foreach(MethodInfo method_info in type.GetMethods()) {
                    foreach(Attribute attr in method_info.GetCustomAttributes(false)) {
                        if(attr is DebugAliasAttribute) {
                            DebugAliasAttribute dbg_attr = attr as DebugAliasAttribute;
                            dbg_attr.MethodInfo = method_info;
                            aliases.Add(dbg_attr);
                        }
                    }
                }
            }
        }
        
        [DebugAlias("p_help", "Prints this help")]
        public static void Help()
        {
            foreach(DebugAliasAttribute attr in aliases) {
                Console.WriteLine("{0}{1}(): {2}", attr.Prefix, attr.Alias, attr.Description);
            }
        }
    }
}
