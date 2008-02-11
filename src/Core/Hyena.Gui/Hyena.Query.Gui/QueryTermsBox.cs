//
// QueryTermsBox.cs
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
using System.Collections.Generic;

using Gtk;
using Hyena;
using Hyena.Query;

namespace Hyena.Query.Gui
{
    public class QueryTermsBox : HBox
    {
        private QueryField [] sorted_fields;
        private List<QueryTermBox> terms = new List<QueryTermBox> ();
        private VBox field_box, op_box, entry_box, button_box;

        public QueryTermBox FirstRow {
            get { return terms.Count > 0 ? terms[0] : null; }
        }
        
        public QueryTermsBox (QueryFieldSet fieldSet) : base ()
        {
            // Sort the fields alphabetically by their label
            sorted_fields = new QueryField [fieldSet.Fields.Length];
            Array.Copy (fieldSet.Fields, sorted_fields, sorted_fields.Length);
            Array.Sort<QueryField> (sorted_fields, delegate (QueryField a, QueryField b) { return a.Label.CompareTo (b.Label); });

            Spacing = 5;

            field_box = new VBox ();
            op_box = new VBox ();
            entry_box = new VBox ();
            button_box = new VBox ();

            field_box.Spacing = 5;
            op_box.Spacing = 5;
            entry_box.Spacing = 5;
            button_box.Spacing = 5;

            PackStart (field_box,   false, false, 0);
            PackStart (op_box,      false, false, 0);
            PackStart (entry_box,   false, true, 0);
            PackStart (button_box,  false, false, 0);

            CreateRow (false);
        }

        public List<QueryNode> QueryNodes {
            get {
                List<QueryNode> nodes = new List<QueryNode> ();
                foreach (QueryTermBox term_box in terms) {
                    nodes.Add (term_box.QueryNode);
                }
                return nodes;
            }
            set {
                foreach (QueryNode child in value) {
                    AddNode (child);
                }
            }
        }

        private bool first_add_node = true;
        private void AddNode (QueryNode node)
        {
            if (node is QueryTermNode) {
                QueryTermBox box = first_add_node ? FirstRow : CreateRow (false);
                box.QueryNode = node as QueryTermNode;
                first_add_node = false;
            } else {
                Console.WriteLine ("Query Gui cannot handle child node: {0}", node.ToString ());
            }
        }

        public QueryTermBox CreateRow (bool canDelete)
        {
            QueryTermBox row = new QueryTermBox (sorted_fields);

            row.ValueEntry.HeightRequest = 31;
            row.Buttons.HeightRequest = 31;

            field_box.PackStart  (row.FieldChooser, false, false, 0);
            op_box.PackStart     (row.OpChooser, false, false, 0);
            entry_box.PackStart  (row.ValueEntry, false, false, 0);
            button_box.PackStart (row.Buttons, false, false, 0);

            row.Show ();

            row.CanDelete = canDelete;
            row.AddRequest += OnRowAddRequest;
            row.RemoveRequest += OnRowRemoveRequest;

            if (terms.Count == 0) {
                //row.FieldBox.GrabFocus ();
            }

            terms.Add (row);

            return row;
        }
        
        public void OnRowAddRequest (object o, EventArgs args)
        {
            CreateRow (true);
            UpdateCanDelete ();
        }
        
        public void OnRowRemoveRequest (object o, EventArgs args)
        {
            QueryTermBox row = o as QueryTermBox;

            field_box.Remove (row.FieldChooser);
            op_box.Remove (row.OpChooser);
            entry_box.Remove (row.ValueEntry);
            button_box.Remove (row.Buttons);

            terms.Remove (row);
            UpdateCanDelete ();
        }
        
        public void UpdateCanDelete ()
        {
            FirstRow.CanDelete = terms.Count > 1;
        }
    }
}
