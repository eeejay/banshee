/***************************************************************************
 *  TestSource.cs
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

using Banshee.Base;

namespace Banshee.Sources
{
    public class TestChildSource : ChildSource
    {
        private static int count;
        
        public TestChildSource() : base("Child " + Next(), 1200)
        {
            GLib.Timeout.Add(2000, Populate);
        }
        
        private bool Populate()
        {
            if(Children.Count >= 5 || count >= 500) {
                return false;
            }
            
            AddChildSource(new TestChildSource());
            return true;
        }
        
        private static string Next()
        {
            count++;
            return count.ToString();
        }
        
        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, Gtk.Stock.MediaRecord);
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
    }
    
    public class TestSource : Source
    {
        public TestSource() : base("Debug Test", 1000)
        {
        }
   
        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, Gtk.Stock.Execute);
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
        
        public static void AddSingle()
        {
            SourceManager.AddSource(new TestSource());
        }
        
        public static void AddRecursive()
        {
            TestSource parent = new TestSource();
            SourceManager.AddSource(parent);
            parent.AddChildSource(new TestChildSource());
            parent.AddChildSource(new TestChildSource());
            parent.AddChildSource(new TestChildSource());
        }
        
        public static void RemoveAll()
        {
            SourceManager.RemoveSource(typeof(TestSource));
        }
    }
}

