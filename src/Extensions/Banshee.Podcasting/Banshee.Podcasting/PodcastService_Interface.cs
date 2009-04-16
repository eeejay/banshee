/*************************************************************************** 
 *  PodcastService_Interface.cs
 *
 *  Copyright (C) 2008 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Gtk;
using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Gui;

using Migo.Syndication;

using Banshee.Web;
using Banshee.Base;
using Banshee.Sources;
using Banshee.Streaming;
using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.Widgets;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Podcasting.Gui;
using Banshee.Podcasting.Data;

namespace Banshee.Podcasting
{
    public partial class PodcastService
    {                 
        private PodcastActions actions = null;
         
        private void InitializeInterface ()
        {
            source = new PodcastSource ();

            ServiceManager.SourceManager.AddSource (source);
            actions = new PodcastActions (source);
        }
        
        private void DisposeInterface ()
        {
            if (source != null) {
                ServiceManager.SourceManager.RemoveSource (source);
                source = null;
            }
            
            if (actions != null) {
                actions.Dispose ();
                actions = null;
            }
        }
    }
}
