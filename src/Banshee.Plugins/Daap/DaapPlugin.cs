/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DaapPlugin.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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
using Mono.Unix;

using Banshee.Base;

namespace Banshee.Plugins.Daap
{
    public class DaapPlugin : Banshee.Plugins.Plugin
    {
        protected override string ConfigurationName { get { return "DaapPlugin"; } }
        public override string DisplayName { get { return "Music Sharing"; } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Allow browsing and listening to songs from music shares and share your Banshee " + 
                    "library with others. Works with other instances of Banshee, iTunes, and Rhythmbox."
                );
            }
        }
        
        public override string [] Authors {
            get {
                return new string [] { 
                    "Aaron Bockover",
                    "James Wilcox"
                };
            }
        }

        protected override void PluginInitialize()
        {
            DaapCore.Initialize();
        }
        
        protected override void PluginDispose()
        {
            DaapCore.Dispose();
        }
    }
}
