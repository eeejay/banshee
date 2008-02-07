//
// QueryBox.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.Text;

using Gtk;
using Hyena;
using Hyena.Query;

namespace Hyena.Query.Gui
{
    public class QueryBox : VBox
    {
        private QueryFieldSet field_set;

        private QueryTermBox first_row = null;
        public QueryTermBox FirstRow {
            get { return first_row; }
        }
        
        public QueryBox (QueryFieldSet fieldSet) : base()
        {
            this.field_set = fieldSet;
            CreateRow (false);
        }

        public QueryNode QueryNode {
            get {
                QueryListNode and = new QueryListNode (Keyword.And);
                for (int i = 0, n = Children.Length; i < n; i++) {
                    QueryTermBox term_box = Children [i] as QueryTermBox;
                    and.AddChild (term_box.QueryNode);
                }
                return and.Trim ();
            }
            set {
                if (value is QueryListNode) {
                    // type = value.Keyword
                    foreach (QueryNode child in (value as QueryListNode).Children) {
                        AddNode (child);
                    }
                } else {
                    // type = 'and'
                    AddNode (value);
                }
            }
        }

        private void AddNode (QueryNode node)
        {
            if (node is QueryTermNode) {
                QueryTermBox box = CreateRow (false);
                box.QueryNode = node as QueryTermNode;
            } else {
                Console.WriteLine ("Query Gui cannot handle child node: {0}", node.ToString ());
            }
        }

        public QueryTermBox CreateRow (bool canDelete)
        {
            QueryTermBox row = new QueryTermBox (field_set);
            row.Show();
            PackStart (row, false, false, 0);
            row.CanDelete = canDelete;
            row.AddRequest += OnRowAddRequest;
            row.RemoveRequest += OnRowRemoveRequest;

            if (first_row == null) {
                first_row = row;
                //row.FieldBox.GrabFocus();
            }
            return row;
        }
        
        public void OnRowAddRequest(object o, EventArgs args)
        {
            CreateRow (true);
            UpdateCanDelete ();
        }
        
        public void OnRowRemoveRequest(object o, EventArgs args)
        {
            Remove (o as Widget);
            UpdateCanDelete ();
        }
        
        public void UpdateCanDelete()
        {
            ((QueryTermBox) Children[0]).CanDelete = Children.Length > 1;
        }
        
    }
}
