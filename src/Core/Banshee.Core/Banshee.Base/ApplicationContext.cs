//
// ApplicationContext.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

namespace Banshee.Base
{
    public static class ApplicationContext
    {
        private static ArgumentQueue argument_queue;
        public static ArgumentQueue ArgumentQueue {
            set { argument_queue = value; }
            get { return argument_queue; }
        }
        
        private static bool? debugging = null;
        public static bool Debugging {
            get {
                if(debugging == null) {
                    debugging = ArgumentQueue.Contains("debug");
                    string debug_env = Environment.GetEnvironmentVariable("BANSHEE_DEBUG");
                    debugging |= debug_env != null && debug_env != String.Empty;
                }
                
                return debugging.Value;
            }
        }
        
        public static bool EnvironmentIsSet(string env)
        {
            string env_val = Environment.GetEnvironmentVariable(env);
            return env_val != null && env_val != String.Empty;
        }
        
        private static System.Globalization.CultureInfo culture_info = new System.Globalization.CultureInfo("en-US");
        public static System.Globalization.CultureInfo InternalCultureInfo {
            get { return culture_info; }
        }
    }
}
