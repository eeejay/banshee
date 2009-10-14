// 
// MoblinPanel.cs
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
using Mutter;
using Hyena;
using Banshee.Base;

namespace Banshee.Moblin
{
    public class MoblinPanel : IDisposable
    {
        public static MoblinPanel Instance { get; private set; }
        
        public PanelGtk ToolbarPanel { get; private set; }
        public Container ParentContainer { get; private set; }
        
        public uint ToolbarPanelWidth { get; private set; }
        public uint ToolbarPanelHeight { get; private set; }

        public MoblinPanel ()
        {
            if (Instance != null) {
                throw new ApplicationException ("Only one MoblinPanel instance can exist");
            }
            
            if (ApplicationContext.CommandLine.Contains ("mutter-panel")) {
                BuildPanel ();
                Instance = this;
            }
        }
        
        public void Dispose ()
        {
        }
        
        private void BuildPanel ()
        {
            try {
                ToolbarPanel = new PanelGtk ("media", "media", null, "media-button", true);
                ParentContainer = ToolbarPanel.Window;
                ParentContainer.ModifyBg (StateType.Normal, ParentContainer.Style.White);
                ToolbarPanel.SetSizeEvent += (o, e) => {
                    ToolbarPanelWidth = e.Width;
                    ToolbarPanelHeight = e.Height;
                };
            } catch (Exception e) {
                Log.Exception ("Could not bind to Moblin panel", e);
                
                var window = new Gtk.Window ("Moblin Media Panel");
                window.SetDefaultSize (1000, 500);
                window.WindowPosition = Gtk.WindowPosition.Center;
                ParentContainer = window;
            }
            
            ParentContainer.ShowAll ();
        }
    }
}
