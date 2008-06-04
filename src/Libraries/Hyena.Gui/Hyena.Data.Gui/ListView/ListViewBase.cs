//
// ListViewBase.cs
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

namespace Hyena.Data.Gui
{
    public class ListViewBase : Widget
    {
        private static TreeView tree_view;
        public static TreeView TreeViewStyleAdapter {
            get { return tree_view; }
            set { tree_view = value; }
        }
        
        public ListViewBase ()
        {
            if (TreeViewStyleAdapter != null) {
                TreeViewStyleAdapter.StyleSet += OnTreeViewStyleAdapterStyleSet;
            }
        }
        
        public override void Dispose ()
        {
            if (TreeViewStyleAdapter != null) {
                TreeViewStyleAdapter.StyleSet -= OnTreeViewStyleAdapterStyleSet;
            }
            
            base.Dispose ();
        }
        
        protected override void OnRealized ()
        {
            base.OnRealized ();
            AdaptTreeViewStyle (TreeViewStyleAdapter);
        }

        private void OnTreeViewStyleAdapterStyleSet (object o, StyleSetArgs args)
        {
            AdaptTreeViewStyle (TreeViewStyleAdapter);
        }
        
        public void AdaptTreeViewStyle (TreeView treeView)
        {
            if (treeView == null || !treeView.IsRealized) {
                return;
            }
            
            foreach (StateType state in Enum.GetValues (typeof (StateType))) {
                ModifyBg (state, treeView.Style.Background (state));
                ModifyFg (state, treeView.Style.Foreground (state));
                ModifyBase (state, treeView.Style.Base (state));
            }
        }
    }
}
