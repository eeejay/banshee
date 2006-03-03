/***************************************************************************
 *  ImportDialog.cs
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

using Banshee.Sources;
using Banshee.Base;

namespace Banshee.Gui
{
    public class ImportDialog : GladeDialog
    {
        private ComboBox source_combo_box;
        private ListStore source_model;
    
        public ImportDialog() : this(false)
        {
        }
        
        public ImportDialog(bool doNotShowAgainVisible) : base("ImportDialog")
        {
            DoNotShowAgainVisible = doNotShowAgainVisible;
            
            PopulateSourceList();
            
            SourceManager.SourceAdded += OnSourceAdded;
            SourceManager.SourceRemoved += OnSourceRemoved;
            SourceManager.SourceUpdated += OnSourceUpdated;
            
            Glade["MessageLabel"].Visible = Globals.Library.Tracks.Count == 0;
        }
        
        private void PopulateSourceList()
        {
            source_model = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(IImportSource));
            
            source_combo_box = new ComboBox();
            source_combo_box.Model = source_model;
            
            CellRendererPixbuf pixbuf_cr = new CellRendererPixbuf();
            CellRendererText text_cr = new CellRendererText();
            
            source_combo_box.PackStart(pixbuf_cr, false);
            source_combo_box.PackStart(text_cr, true);
            source_combo_box.SetAttributes(pixbuf_cr, "pixbuf", 0);
            source_combo_box.SetAttributes(text_cr, "text", 1);
            
            // Possibly register and instantiate "static" standalone import sources in case this is our first run
            ImportSources.Add(FolderImportSource.Instance);
            ImportSources.Add(FileImportSource.Instance);
            ImportSources.Add(HomeDirectoryImportSource.Instance);
            
            // Add the standalone sources (ImportSources is used in case plugins register a IImportSource)
            foreach(IImportSource source in ImportSources.Sources) {
                AddSource(source);
            }
            
            // Find active sources that implement IImportSource
            foreach(Source source in SourceManager.Sources) {
                if(source is IImportSource) {
                    AddSource((IImportSource)source);
                }
            }
            
            TreeIter active_iter;
            if(source_model.GetIterFirst(out active_iter)) {
                source_combo_box.SetActiveIter(active_iter);
            }
            
            (Glade["ComboVBox"] as Box).PackStart(source_combo_box, false, false, 0);
            source_combo_box.ShowAll();
        }
        
        private void AddSource(IImportSource source)
        {
            if(source == null) {
                return;
            }
            
            source_model.AppendValues(source.Icon, source.Name, source);
        }
        
        private void OnSourceAdded(SourceAddedArgs args)
        {
           if(args.Source is IImportSource) {
                AddSource((IImportSource)args.Source);
           }
        }
        
        private void OnSourceRemoved(SourceEventArgs args)
        {
            if(args.Source is IImportSource) {
                TreeIter iter;
                if(FindSourceIter(out iter, (IImportSource)args.Source)) {
                    source_model.Remove(ref iter);
                }
            }
        }
        
        private void OnSourceUpdated(SourceEventArgs args)
        {
            if(args.Source is IImportSource) {
                TreeIter iter;
                if(FindSourceIter(out iter, (IImportSource)args.Source)) {
                    source_model.SetValue(iter, 1, args.Source.Name);
                }
            }
        }
        
        private bool FindSourceIter(out TreeIter iter, IImportSource source)
        {
            iter = TreeIter.Zero;
            
            for(int i = 0, n = source_model.IterNChildren(); i < n; i++) {
                TreeIter _iter;
                if(source_model.IterNthChild(out _iter, i)) {
                    if(source == source_model.GetValue(_iter, 2)) {
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
                if(source_combo_box.GetActiveIter(out iter)) {
                    return (IImportSource)source_model.GetValue(iter, 2);
                }
                
                return null;
            }
        }
    }
}
