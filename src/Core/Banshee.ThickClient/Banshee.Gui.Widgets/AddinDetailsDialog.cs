//
// AddinDetailsDialog.cs
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
using System.Text;
using Mono.Addins;
using Mono.Addins.Setup;
using Mono.Addins.Description;
using Mono.Unix;
using Gtk;

using Hyena.Widgets;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui.Widgets
{
    public class AddinDetailsDialog : BansheeDialog
    {
        public AddinDetailsDialog (Addin addin, Window parent) : base (String.Empty, parent)
        {
            SetDefaultSize (400, -1);
            
            AddinHeader info = SetupService.GetAddinHeader (addin);
            
            HBox box = new HBox ();
            box.BorderWidth = 10;
            box.Spacing = 10;
            VBox.PackStart (box, false, false, 0);
            
            Image image = new Image ();
            image.IconName = "package-x-generic";
            image.IconSize = (int)IconSize.Dialog;
            image.Yalign = 0.0f;
            
            box.PackStart (image, false, false, 0);
            
            StringBuilder builder = new StringBuilder ();
            
            builder.AppendFormat ("<b><big>{0}</big></b>\n\n", GLib.Markup.EscapeText (addin.Name));
            builder.AppendFormat (GLib.Markup.EscapeText (addin.Description.Description)).Append ("\n\n");
            
            builder.Append ("<small>");
            
            builder.AppendFormat ("<b>{0}</b> {1}\n\n", Catalog.GetString ("Version:"), 
                GLib.Markup.EscapeText (addin.Description.Version));
            
            builder.AppendFormat ("<b>{0}</b> {1}\n\n", Catalog.GetString ("Authors:"), 
                GLib.Markup.EscapeText (addin.Description.Author));
            
            builder.AppendFormat ("<b>{0}</b> {1}\n\n", Catalog.GetString ("Copyright/License:"), 
                GLib.Markup.EscapeText (addin.Description.Copyright));
            
            if (info.Dependencies.Count > 0) {
                builder.AppendFormat ("<b>{0}</b>\n", Catalog.GetString ("Extension Dependencies:"));
                foreach (AddinDependency dep in info.Dependencies) {
                    builder.Append (GLib.Markup.EscapeText (dep.Name)).Append ('\n');
                }
            }
            
            builder.Append ("</small>");
            
            WrapLabel label = new WrapLabel ();
            label.Markup = builder.ToString ();
            box.PackStart (label, true, true, 0);
            
            VBox.ShowAll ();
        
            AddDefaultCloseButton ();
        }
    }
}
