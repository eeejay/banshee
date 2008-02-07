//
// QueryTermBox.cs
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
    public class QueryTermBox : HBox
    {
        private ComboBox field_chooser;
        private ComboBox op_chooser;
        private HBox value_box;

        private Button add_button;
        private Button remove_button;
        
        public event EventHandler AddRequest;
        public event EventHandler RemoveRequest;

        private QueryField field;
        private QueryValueEntry value_entry;
        private Operator op;

        private QueryField [] sorted_fields;
        private Operator [] operators;

        public QueryTermBox (QueryFieldSet fieldSet) : base ()
        {
            this.sorted_fields = fieldSet.Fields;
            //this.field_set = fieldSet;
            BuildInterface ();
        }

        private void BuildInterface ()
        {
            Spacing = 5;

            field_chooser = ComboBox.NewText ();
            field_chooser.Changed += HandleFieldChanged;
            PackStart (field_chooser, false, false, 0);
            
            op_chooser = ComboBox.NewText ();
            op_chooser.Changed += HandleOperatorChanged;
            PackStart (op_chooser, false, false, 0);
            
            value_box = new HBox ();
            PackStart (value_box, false, false, 0);

            remove_button = new Button (new Image ("gtk-remove", IconSize.Button));
            remove_button.Relief = ReliefStyle.None;
            remove_button.Clicked += OnButtonRemoveClicked;
            PackEnd (remove_button, false, false, 0);
            
            add_button = new Button (new Image ("gtk-add", IconSize.Button));
            add_button.Relief = ReliefStyle.None;
            add_button.Clicked += OnButtonAddClicked;
            PackEnd (add_button, false, false, 0);

            foreach (QueryField field in sorted_fields) {
                field_chooser.AppendText (field.Label);
            }

            field_chooser.Active = 0;

            ShowAll ();
        }
        
        private bool first = true;
        private void HandleFieldChanged (object o, EventArgs args)
        {
            QueryField field = sorted_fields [field_chooser.Active];
            Console.WriteLine ("Changing to field {0}", field.Name);
            // Leave everything as is unless the new field is a different type
            if (this.field != null && (field == this.field || field.ValueType == this.field.ValueType))
                return;

            op_chooser.Changed -= HandleOperatorChanged;

            this.field = field;

            if (first) {
                first = false;
            } else {
                value_box.Remove (value_box.Children [0]);
            }

            value_entry = QueryValueEntry.Create (this.field.CreateQueryValue ());

            if (value_entry == null) {
                Console.WriteLine ("Value entry is NULL!, field.ValueType == {0}", this.field.ValueType);
            }
            value_box.Add (value_entry);
            value_entry.ShowAll ();

            // Remove old type's operators
            while (op_chooser.Model.IterNChildren () > 0) {
                op_chooser.RemoveText (0);
            }

            // Add new type's operators
            foreach (Operator op in value_entry.QueryValue.OperatorSet) {
                op_chooser.AppendText (op.Label);
            }
            
            // TODO: If we have the same operator that was previously selected, select it
            op_chooser.Changed += HandleOperatorChanged;
            op_chooser.Active = 0;
        }
        
        private void HandleOperatorChanged (object o, EventArgs args)
        {
            this.op = value_entry.QueryValue.OperatorSet.Objects [op_chooser.Active];
            Console.WriteLine ("Operator {0} selected", op.Name);

            //value_entry = new QueryValueEntry <field.ValueType> ();
        }
        
        private void OnButtonAddClicked (object o, EventArgs args)
        {
            EventHandler handler = AddRequest;
            if (handler != null)
                handler (this, new EventArgs ());
        }
        
        private void OnButtonRemoveClicked (object o, EventArgs args)
        {
            EventHandler handler = RemoveRequest;
            if (handler != null)
                handler (this, new EventArgs ());
        }
        
        public bool CanDelete {
            get { return remove_button.Sensitive; }
            set { remove_button.Sensitive = value; }
        }
        
        public QueryNode GetTermNode ()
        {
            QueryTermNode node = new QueryTermNode ();
            node.Field = field;
            node.Operator = op;
            node.Value = value_entry.QueryValue;
            return node;
        }
    }
}
