/***************************************************************************
 *  SourceComboBox.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 *             Felipe Almeida Lessa
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

using Banshee.ServiceStack;
using Banshee.Sources;

namespace Banshee.MiniMode
{ 
    public class SourceComboBox : Gtk.ComboBox
    {
        public SourceComboBox()
        {
            Clear();
            
            CellRendererPixbuf image = new CellRendererPixbuf();
            PackStart(image, false);
            AddAttribute(image, "pixbuf", 0);
            
            CellRendererText text = new CellRendererText();
            PackStart(text, true);
            AddAttribute(text, "text", 1);
            
            Model = new SourceModel();
            
            ServiceManager.SourceManager.ActiveSourceChanged += delegate { 
                UpdateActiveSource(); 
            };
            
            ServiceManager.SourceManager.SourceUpdated += delegate {
                QueueDraw();
            };
        }
        
        public void UpdateActiveSource()
        {
            lock(this) {
                TreeIter iter = Model.FindSource(ServiceManager.SourceManager.ActiveSource);
                if(!iter.Equals(TreeIter.Zero)) {
                    SetActiveIter(iter);
                }
            }
        }

        protected override void OnChanged()
        {
            lock(this) {
                TreeIter iter;
                
                if(GetActiveIter(out iter)) {
                    Source new_source = Model.GetValue(iter, 2) as Source;
                    if(new_source != null && ServiceManager.SourceManager.ActiveSource != new_source) {
                        ServiceManager.SourceManager.SetActiveSource(new_source);
                        if(new_source is ITrackModelSource) {
                            ServiceManager.PlaybackController.Source = (ITrackModelSource)new_source;
                        }
                    }
                }
            }
        }
                
        public new SourceModel Model { 
            get { return base.Model as SourceModel; }
            set { base.Model = value; }
        }
    } 
}
