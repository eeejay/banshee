//
// BansheeDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Gtk;

using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.Gui.Dialogs
{
    public class BansheeDialog : Gtk.Dialog
    {
        private AccelGroup accel_group;
        protected AccelGroup AccelGroup {
            get { return accel_group; }
        }
        
        public BansheeDialog (string title) : this (title, null)
        {
        }
        
        public BansheeDialog (string title, Window parent) : base ()
        {
            Title = title;
            BorderWidth = 5;
            Visible = false;
            HasSeparator = false;
            
            if (parent == null) {
                GtkElementsService service = ServiceManager.Get<GtkElementsService> ();
                if (service != null) {
                    TransientFor = service.PrimaryWindow;
                }
            } else {
                TransientFor = parent;
            }
            
            WindowPosition = WindowPosition.CenterOnParent;
            DestroyWithParent = true;
            
            accel_group = new AccelGroup ();
            AddAccelGroup (accel_group);
        }
        
        public void AddDefaultCloseButton ()
        {
            AddStockButton (Stock.Close, ResponseType.Close);
        }
        
        public new void AddButton (string message, ResponseType response)
        {
            AddButton (message, response, false);
        }
        
        public new void AddStockButton (string stock, ResponseType response)
        {
            AddStockButton (stock, response, false);
        }

        public new void AddButton (string message, ResponseType response, bool isDefault)
        {
            AddButton (message, response, isDefault, false);
        }

        public void AddStockButton (string stock, ResponseType response, bool isDefault)
        {
            AddButton (stock, response, isDefault, true);
        }
        
        public new void AddButton (string message, ResponseType response, bool isDefault, bool isStock)
        {
            Button button = new Button (message);
            button.CanDefault = true;
            button.UseStock = isStock;
            button.Show ();
            AddButton (button, response, isDefault);
        }

        public void AddButton (Button button, ResponseType response, bool isDefault)
        {
            AddActionWidget (button, response);

            if (isDefault) {
                DefaultResponse = response;
                button.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.Return, 0, AccelFlags.Visible);
            }
        }
    }
}
