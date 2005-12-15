/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Globals.cs
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
using GConf;

namespace Banshee.Base
{
    public static class Globals
    {
        private static GConf.Client gconf_client;
        private static string library_location;
        private static NetworkDetect network_detect;
        private static ActionManager action_manager;
        
        static Globals()
        {
            gconf_client = new GConf.Client();
            network_detect = NetworkDetect.Instance;
            action_manager = new ActionManager();
        }
        
        public static void Dispose()
        {
        }
        
        public static GConf.Client Configuration {
            get {
                return gconf_client;
            }
        }
        
        public static string LibraryLocation {
            get {
                return library_location;
            }
            
            set {
                library_location = value;
            }
        }
        
        public static NetworkDetect Network {
            get {
                return network_detect;
            }
        }
        
        public static ActionManager ActionManager {
            get {
                return action_manager;
            }
        }
    }
}
