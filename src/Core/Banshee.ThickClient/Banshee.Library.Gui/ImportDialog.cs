// 
// ImportDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using Glade;

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.Gui.Dialogs;

namespace Banshee.Library.Gui
{
    public class ImportDialog : GladeDialog
    {
        private ComboBox source_combo_box;
        private ListStore source_model;
        private AccelGroup accel_group;
        
        public ImportDialog () : this (false)
        {
        }
        
        public ImportDialog (bool doNotShowAgainVisible) : base ("ImportDialog")
        {
            accel_group = new AccelGroup ();

            if (ServiceManager.Contains ("GtkElementsService")) {
                Dialog.TransientFor = ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow;
            }
            
            Dialog.WindowPosition = WindowPosition.CenterOnParent;
            Dialog.AddAccelGroup (accel_group);
            Dialog.DefaultResponse = ResponseType.Ok;
		    
            DoNotShowAgainVisible = doNotShowAgainVisible;
            
            PopulateSourceList ();
            
            ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            ServiceManager.SourceManager.SourceRemoved += OnSourceRemoved;
            ServiceManager.SourceManager.SourceUpdated += OnSourceUpdated;
            
            Glade["MessageLabel"].Visible = ServiceManager.SourceManager.DefaultSource.Count == 0;
            
            (Glade["ImportButton"] as Button).AddAccelerator ("activate", accel_group, 
                (uint)Gdk.Key.Return, 0, AccelFlags.Visible);
            
            Dialog.StyleSet += delegate {
                UpdateIcons ();
            };
        }
        
        private void PopulateSourceList ()
        {
            source_model = new ListStore (typeof (Gdk.Pixbuf), typeof (string), typeof (IImportSource));
            
            source_combo_box = new ComboBox ();
            source_combo_box.Model = source_model;
            
            CellRendererPixbuf pixbuf_cr = new CellRendererPixbuf ();
            CellRendererText text_cr = new CellRendererText ();
            
            source_combo_box.PackStart (pixbuf_cr, false);
            source_combo_box.PackStart (text_cr, true);
            source_combo_box.SetAttributes (pixbuf_cr, "pixbuf", 0);
            source_combo_box.SetAttributes (text_cr, "text", 1);
            
            TreeIter active_iter = TreeIter.Zero;
            TreeIter migration_iter = TreeIter.Zero;
            
            // Add the standalone import sources
            foreach (IImportSource source in ServiceManager.Get<ImportSourceManager> ("ImportSourceManager")) {
                AddSource (source);
            }
            
            // Find active sources that implement IImportSource
            foreach (Source source in ServiceManager.SourceManager.Sources) {
                if (source is IImportSource) {
                    AddSource ((IImportSource)source);
                }
            }
            
            if (!active_iter.Equals(TreeIter.Zero) || (active_iter.Equals (TreeIter.Zero) && 
                source_model.GetIterFirst (out active_iter))) {
                source_combo_box.SetActiveIter (active_iter);
            } 
            
            (Glade["ComboVBox"] as Box).PackStart (source_combo_box, false, false, 0);
            source_combo_box.ShowAll ();
        }
        
        private void UpdateIcons ()
        {
            for (int i = 0, n = source_model.IterNChildren (); i < n; i++) {
                TreeIter iter;
                if (source_model.IterNthChild (out iter, i)) {
                    object o = source_model.GetValue (iter, 0);
                    IImportSource source = (IImportSource)source_model.GetValue (iter, 2);
                    if (o != null) {
                        ((Gdk.Pixbuf)o).Dispose ();
                    }
                    
                    source_model.SetValue (iter, 0, GetIcon (source));
                }
            }
        }
        
        private Gdk.Pixbuf GetIcon (IImportSource source)
        {
            return IconThemeUtils.LoadIcon (22, source.IconNames);
        }
        
        private TreeIter AddSource (IImportSource source)
        {
            if (source == null) {
                return TreeIter.Zero;
            }
            
            return source_model.AppendValues (GetIcon (source), source.Name, source);
        }
        
        private void OnSourceAdded (SourceAddedArgs args)
        {
            if(args.Source is IImportSource) {
                AddSource ((IImportSource)args.Source);
            }
        }
        
        private void OnSourceRemoved (SourceEventArgs args)
        {
            if (args.Source is IImportSource) {
                TreeIter iter;
                if (FindSourceIter (out iter, (IImportSource)args.Source)) {
                    source_model.Remove (ref iter);
                }
            }
        }
        
        private void OnSourceUpdated (SourceEventArgs args)
        {
            if (args.Source is IImportSource) {
                TreeIter iter;
                if(FindSourceIter (out iter, (IImportSource)args.Source)) {
                    source_model.SetValue (iter, 1, args.Source.Name);
                }
            }
        }
        
        private bool FindSourceIter (out TreeIter iter, IImportSource source)
        {
            iter = TreeIter.Zero;
            
            for (int i = 0, n = source_model.IterNChildren (); i < n; i++) {
                TreeIter _iter;
                if (source_model.IterNthChild (out _iter, i)) {
                    if (source == source_model.GetValue (_iter, 2)) {
                        iter = _iter;
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public bool DoNotShowAgainVisible {
            get { return Glade["DoNotShowCheckBox"].Visible; }
            set { Glade["DoNotShowCheckBox"].Visible = value; }
        }
        
        public bool DoNotShowAgain {
            get { return (Glade["DoNotShowCheckBox"] as CheckButton).Active; }
        }
        
        public IImportSource ActiveSource {
            get {
                TreeIter iter; 
                if (source_combo_box.GetActiveIter (out iter)) {
                    return (IImportSource)source_model.GetValue (iter, 2);
                }
                
                return null;
            }
        }
    }
}
