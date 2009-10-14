// 
// MediaPanelContents.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;

using Hyena.Data.Gui;
using Banshee.Collection.Gui;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.PlayQueue;

namespace Banshee.Moblin
{
    public class MediaPanelContents : HBox
    {
        public MediaPanelContents () : base ()
        {
            BuildViews ();
            BorderWidth = 10;
            Spacing = 10;
        }
        
        private void BuildViews ()
        {
            var left = new VBox () { Spacing = 10 };
            left.PackStart (new SearchHeader (), false, false, 0);
            left.PackStart (new RecentAlbumsView (), false, false, 0);

            PackStart (left, true, true, 0);
            PackStart (new PlayQueueBox (), false, false, 0);

            ShowAll ();
        }
        
        protected override void OnParentSet (Widget previous)
        {
            base.OnParentSet (previous);
            
            if (Parent != null) {
                Parent.ModifyBg (StateType.Normal, Style.White);
            }
        }
    }
}
