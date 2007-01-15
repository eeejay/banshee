/***************************************************************************
 *  SourceModel.cs
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

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Plugins.MiniMode
{ 
    public class SourceModel : Gtk.TreeStore
    {
        internal SourceModel() : base(typeof(Gdk.Pixbuf), typeof(string), typeof(Source))
        {          
            Clear();
            
            foreach(Source source in SourceManager.Sources) {
                AddSource(source);
            }
            
            
            SourceManager.SourceAdded += delegate(SourceAddedArgs args) {
                AddSource(args.Source, args.Position);
            };
            
            SourceManager.SourceRemoved += delegate(SourceEventArgs args) {
                RemoveSource(args.Source);
            };
        }

        private void SetSource(TreeIter iter, Source source) 
        {
            Gdk.Pixbuf icon = source.Icon;
            
            if(icon == null) {
                icon = IconThemeUtils.LoadIcon(22, "source-library");
            }
            
            SetValue(iter, 0, icon);
            SetValue(iter, 1, source.Name);
            SetValue(iter, 2, source);
        }
                
        private void AddSource(Source source)
        {
            AddSource(source, -1);
        }

        private void AddSource(Source source, int position)
        {
            if(FindSource(source).Equals(TreeIter.Zero)) {
                TreeIter iter = InsertNode(position);
                SetSource(iter, source);
                
                #if !BANSHEE_0_10
                    foreach(ChildSource child in source.Children) {
                        SetSource(AppendNode(iter), child);
                    }
            
                    source.ChildSourceAdded += delegate(SourceEventArgs e) {
                        SetSource(AppendNode(iter), e.Source);
                    };

                    source.ChildSourceRemoved += delegate(SourceEventArgs e) {
                        RemoveSource(e.Source);
                    };
                #endif
            }
        }

        private void RemoveSource(Source source)
        {
            TreeIter iter = FindSource(source);
            if(!iter.Equals(TreeIter.Zero)) {
                Remove(ref iter);
            }
        }
    
        // FIXME: This is lame and could use some recusrion instead (Lukas)
        internal TreeIter FindSource(Source source)
        {
            for(int i = 0, m = IterNChildren(); i < m; i++) {
                TreeIter iter = TreeIter.Zero;
                if(!IterNthChild(out iter, i)) {
                    continue;
                }
                
                if((GetValue(iter, 2) as Source) == source) {
                    return iter;
                }
        
                for(int j = 0, n = IterNChildren(iter); j < n; j++) {
                    TreeIter citer = TreeIter.Zero;
                    if(!IterNthChild(out citer, iter, j)) {
                        continue;
                    }
    
                    if((GetValue(citer, 2) as Source) == source) {
                        return citer;
                    }
                }
            }

            return TreeIter.Zero;
        }
    }
}
