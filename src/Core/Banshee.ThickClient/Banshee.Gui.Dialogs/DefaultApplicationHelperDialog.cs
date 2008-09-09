//
// DefaultApplicationHelperDialog.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using Mono.Unix;
using Gtk;
using Banshee.Configuration;

namespace Banshee.Gui.Dialogs
{
    public class DefaultApplicationHelperDialog : Banshee.Widgets.HigMessageDialog
    {
        public static void RunIfAppropriate ()
        {
            // Don't need to do anything if we're already the default
            if (!DefaultApplicationHelper.NeverAsk && !DefaultApplicationHelper.IsDefault) {
                if (DefaultApplicationHelper.RememberChoiceSchema.Get ()) {
                    // Not default, but were told to remember 'make default', so just make it default w/o the dialog
                    DefaultApplicationHelper.MakeDefault ();
                } else {
                    Banshee.Gui.Dialogs.DefaultApplicationHelperDialog dialog = new Banshee.Gui.Dialogs.DefaultApplicationHelperDialog ();
                    try {
                        if (!DefaultApplicationHelper.EverAskedSchema.Get ()) {
                            dialog.Remember = true;
                        }
                        int resp = dialog.Run ();
                        DefaultApplicationHelper.RememberChoiceSchema.Set (dialog.Remember);
                        if (resp == (int)Gtk.ResponseType.Yes) {
                            DefaultApplicationHelper.MakeDefaultSchema.Set (true);
                            DefaultApplicationHelper.MakeDefault ();
                        } else {
                            DefaultApplicationHelper.MakeDefaultSchema.Set (false);
                        }
                        DefaultApplicationHelper.EverAskedSchema.Set (true);
                    } finally {
                        dialog.Destroy ();
                    }
                }
            }
        }
        
        private CheckButton remember;
        private DefaultApplicationHelperDialog () : base (null, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.None,
                                                         Catalog.GetString ("Make Banshee the default media player?"),
                                                         Catalog.GetString ("Currently another program is configured as the default media player.  Would you prefer Banshee to be the default?"))
        {
            remember = new CheckButton (Catalog.GetString ("Do not ask me this again"));
            remember.UseUnderline = true;
            remember.Active = DefaultApplicationHelper.EverAskedSchema.Get ()
                ? DefaultApplicationHelper.RememberChoiceSchema.Get () : false;
            remember.Show ();

            Alignment align = new Alignment (0f, 0f, 1f, 1f);
            align.TopPadding = 6;
            align.Child = remember;
            align.Show ();
            
            LabelVBox.PackEnd (align, false, false, 0);
            
            AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.No, false);
            AddCustomButton (Catalog.GetString ("Make Banshee the Default"), Gtk.ResponseType.Yes, true).HasFocus = true;
        }

        public bool Remember {
            get { return remember.Active; }
            set { remember.Active = value; }
        }
    }
}
