/***************************************************************************
 *  Reflection.cs
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
    [DebugAlias("rfl")]
    public static class ReflectionDebugger
    {
        public static Assembly [] GetLoadedAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
        
        [DebugAlias("p_asms", "Print loaded application assemblies")]
        public static void PrintLoadedAssemblies()
        {
            PrintLoadedAssemblies(false, false);
        }
        
        [DebugAlias("p_asms_gac", "Print loaded assemblies, including GAC")]
        public static void PrintLoadedAssembliesGac()
        {
            PrintLoadedAssemblies(true, false);
        }
        
        [DebugAlias("p_asms_dynamic", "Print loaded assemblies, including dynamic")]
        public static void PrintLoadedAssembliesDynamic()
        {
            PrintLoadedAssemblies(false, true);
        }
        
        [DebugAlias("p_asms_gac_dynamic", "Print loaded assemblies, including GAC and dynamic")]
        public static void PrintLoadedAssembliesGacDynamic()
        {
            PrintLoadedAssemblies(true, true);
        }
        
        private static void PrintLoadedAssemblies(bool gac, bool dynamic)
        {
            foreach(Assembly assembly in GetLoadedAssemblies()) {
                if(assembly.GlobalAssemblyCache && !gac) {
                    continue;
                }
            
                try {
                    if(assembly.Location == null) {
                        if(!dynamic) {
                            continue;
                        }
                        
                        throw new Exception();
                    }
                    
                    Console.WriteLine(assembly.Location);
                } catch {
                    if(dynamic) {
                        Console.WriteLine("<dynamic> {0}", assembly.FullName);
                    }
                }
            }
        }
    }
}
