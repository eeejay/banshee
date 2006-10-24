/***************************************************************************
 *  DaapContainer.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Scott Peterson <lunchtimemama@gmail.com>
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
using System.Collections;
using Mono.Unix;
using DAAP;

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Plugins.Daap
{
    public class DaapContainerSource : Source
    {
        public DaapContainerSource() : base(Catalog.GetString("Shared Music"), 300)
        {
        }

        public override int Count {
            get { return DaapCore.ServerCount; }
        }

        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, 
            "applications-internet", "network-server", Gtk.Stock.Network);

        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
        
        public override bool AutoExpand {
            get { return false; }
        }
        
        public override bool CanActivate {
            get { return false; }
        }
    }
}
