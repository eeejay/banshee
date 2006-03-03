/***************************************************************************
 *  GladeDialog.cs
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
using Gtk;
using Glade;

namespace Banshee.Gui
{
    public abstract class GladeDialog 
    {
        private string dialog_name;
        private Glade.XML glade;
        private Dialog dialog;
        
        protected GladeDialog()
        {
        }

        public GladeDialog(string name)
        {
            dialog_name = name;        
            glade = new Glade.XML(System.Reflection.Assembly.GetEntryAssembly(), "banshee.glade", name, "banshee");
            glade.Autoconnect(this);
        }

        public virtual ResponseType Run()
        {
            return (ResponseType)Dialog.Run();
        }
        
        public virtual void Destroy()
        {
            Dialog.Destroy();
        }

        protected Glade.XML Glade {
            get { return glade; }
        }

        public string DialogName {
            get { return dialog_name; }
        }
        
        public Dialog Dialog {
            get {
                if(dialog == null) {
                    dialog = (Dialog)glade.GetWidget(dialog_name);
                    Banshee.Base.IconThemeUtils.SetWindowIcon(dialog);
                }
                
                return dialog;
            }
        }
    }
}
