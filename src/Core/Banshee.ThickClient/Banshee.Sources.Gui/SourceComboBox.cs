// SourceComboBox.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Felipe Almeida Lessa
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using Banshee.Sources;

namespace Banshee.Sources.Gui
{ 
    public class SourceComboBox : ComboBox
    {
        private SourceModel store;
        
        public SourceComboBox ()
        {
            SourceRowRenderer renderer = new SourceRowRenderer ();
            renderer.ParentWidget = this;
            PackStart (renderer, true);
            SetCellDataFunc (renderer, new CellLayoutDataFunc (SourceCellDataFunc));
            
            store = new SourceModel ();
            Model = store;
            
            ServiceManager.SourceManager.ActiveSourceChanged += delegate { 
                UpdateActiveSource (); 
            };
            
            ServiceManager.SourceManager.SourceUpdated += delegate {
                QueueDraw ();
            };
            
            store.Refresh ();
        }
        
        private void SourceCellDataFunc (CellLayout layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            SourceRowRenderer.CellDataHandler (cell, tree_model, iter);
        }
        
        public void UpdateActiveSource ()
        {
            lock (this) {
                TreeIter iter = store.FindSource (ServiceManager.SourceManager.ActiveSource);
                if (!iter.Equals (TreeIter.Zero)) {
                    SetActiveIter (iter);
                }
            }
        }

        protected override void OnChanged ()
        {
            lock (this) {
                TreeIter iter;
                
                if (GetActiveIter (out iter)) {
                    Source new_source = store.GetValue(iter, 0) as Source;
                    if (new_source != null && ServiceManager.SourceManager.ActiveSource != new_source) {
                        ServiceManager.SourceManager.SetActiveSource (new_source);
                        if (new_source is ITrackModelSource) {
                            ServiceManager.PlaybackController.NextSource = (ITrackModelSource)new_source;
                        }
                    }
                }
            }
        }
    } 
}
