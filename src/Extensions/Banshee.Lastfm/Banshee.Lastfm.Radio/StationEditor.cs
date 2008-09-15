//
// StationEditor.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Collections;
using Gtk;
using Glade;
using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;

using Banshee.Widgets;
using Banshee.Gui.Dialogs;

namespace Banshee.Lastfm.Radio
{
    public class StationEditor : GladeDialog
    {
        const string dialog_name = "StationSourceEditorDialog";
        const string dialog_resource = "lastfm.glade";

        private LastfmSource lastfm;
        private StationSource source;

        [Widget] private Gtk.ComboBox type_combo;
        [Widget] private Gtk.Entry arg_entry;
        [Widget] private Gtk.Label arg_label;
        [Widget] private Gtk.Button ok_button;

        public StationEditor (LastfmSource lastfm, StationSource source) : base (dialog_name, new Glade.XML (
            System.Reflection.Assembly.GetExecutingAssembly (), dialog_resource, dialog_name, Banshee.ServiceStack.Application.InternalName))
        {
            this.lastfm = lastfm;
            this.source = source;
            Arg = source.Arg;
            Initialize ();
            Dialog.Title = Catalog.GetString ("Edit Station");
        }
    
        public StationEditor (LastfmSource lastfm) : base (dialog_name, new Glade.XML (
            System.Reflection.Assembly.GetExecutingAssembly (), dialog_resource, dialog_name, Banshee.ServiceStack.Application.InternalName))
        {
            this.lastfm = lastfm;
            Initialize ();
            Dialog.Title = Catalog.GetString ("New Station");
        }

        private void Initialize ()
        {
            // Pressing enter should save and close the dialog
            //Dialog.DefaultResponse = Gtk.ResponseType.Ok;
            ok_button.HasDefault = true;

            Gdk.Geometry limits = new Gdk.Geometry ();
            limits.MinWidth = Dialog.SizeRequest ().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            Dialog.SetGeometryHints (Dialog, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);

            type_combo.RemoveText (0);
            int active_type = 0;
            int i = 0;
            foreach (StationType type in StationType.Types) {
                if (!type.SubscribersOnly || lastfm.Connection.Subscriber) {
                    type_combo.AppendText (type.Label);
                    if (source != null && type == source.Type) {
                        active_type = i;
                    }
                    i++;
                }
            }

            type_combo.Changed += HandleTypeChanged;
            type_combo.Active = active_type;
            ok_button.Sensitive = true;
            type_combo.GrabFocus ();
        }

        public void RunDialog ()
        {
            Run ();
            Dialog.Destroy ();
        }

        public override ResponseType Run ()
        {
            Dialog.ShowAll ();

            ResponseType response = (ResponseType)Dialog.Run ();

            if (response == ResponseType.Ok) {
                string name = SourceName;
                StationType type = Type;
                string arg = Arg;

                ThreadAssist.Spawn (delegate {
                    if (source == null) {
                        source = new StationSource (lastfm, name, type.Name, arg);
                        lastfm.AddChildSource (source);
                        //LastFMPlugin.Instance.Source.AddChildSource (source);
                        //ServiceManager.SourceManager.AddSource (source);
                    } else {
                        source.Rename (name);
                        source.Type = type;
                        source.Arg = arg;
                        source.Save ();
                        //source.Refresh ();
                    }
                });
            }

            return response;
        }

        private void HandleTypeChanged (object sender, EventArgs args)
        {
            StationType type = StationType.FindByLabel (type_combo.ActiveText);
            if (type == null)
                Console.WriteLine ("got null type for text: {0}", type_combo.ActiveText);
            else
                arg_label.Text = type.ArgLabel;
        }

        private string SourceName {
            get { return source != null ? source.Name : arg_entry.Text; }
        }

        private StationType Type {
            get { return StationType.FindByLabel (type_combo.ActiveText); }
        }

        private string Arg {
            get { return arg_entry.Text; }
            set { arg_entry.Text = value; }
        }
    }
}
