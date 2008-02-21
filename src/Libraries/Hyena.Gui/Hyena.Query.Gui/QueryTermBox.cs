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
    public class QueryTermBox
    {
        private Button add_button;
        private Button remove_button;

        public event EventHandler AddRequest;
        public event EventHandler RemoveRequest;

        private QueryField field;
        private QueryValueEntry value_entry;
        private Operator op;

        private QueryField [] sorted_fields;

        private ComboBox field_chooser;
        public ComboBox FieldChooser {
            get { return field_chooser; }
        }

        private ComboBox op_chooser;
        public ComboBox OpChooser {
            get { return op_chooser; }
        }

        private HBox value_box;
        public HBox ValueEntry {
            get { return value_box; }
        }

        private HBox button_box;
        public HBox Buttons {
            get { return button_box; }
        }

        public QueryTermBox (QueryField [] sorted_fields) : base ()
        {
            this.sorted_fields = sorted_fields;
            BuildInterface ();
        }

        private void BuildInterface ()
        {
            field_chooser = ComboBox.NewText ();
            field_chooser.Changed += HandleFieldChanged;
            
            op_chooser = ComboBox.NewText ();
            op_chooser.Changed += HandleOperatorChanged;
            
            value_box = new HBox ();

            remove_button = new Button (new Image ("gtk-remove", IconSize.Button));
            remove_button.Relief = ReliefStyle.None;
            remove_button.Clicked += OnButtonRemoveClicked;
            
            add_button = new Button (new Image ("gtk-add", IconSize.Button));
            add_button.Relief = ReliefStyle.None;
            add_button.Clicked += OnButtonAddClicked;

            button_box = new HBox ();
            button_box.PackStart (remove_button, false, false, 0);
            button_box.PackStart (add_button, false, false, 0);

            foreach (QueryField field in sorted_fields) {
                field_chooser.AppendText (field.Label);
            }

            Show ();
            field_chooser.Active = 0;
        }

        public void Show ()
        {
            field_chooser.ShowAll ();
            op_chooser.ShowAll ();
            value_box.ShowAll ();
            button_box.ShowAll ();
        }
        
        private bool first = true;
        private void HandleFieldChanged (object o, EventArgs args)
        {
            QueryField field = sorted_fields [field_chooser.Active];

            // Leave everything as is unless the new field is a different type
            if (this.field != null && (field == this.field || field.ValueType == this.field.ValueType)) {
                this.field = field;
                return;
            }

            op_chooser.Changed -= HandleOperatorChanged;

            this.field = field;

            if (first) {
                first = false;
            } else {
                value_box.Remove (value_box.Children [0]);
            }

            value_entry = QueryValueEntry.Create (this.field.CreateQueryValue ());
            value_box.PackStart (value_entry, false, true, 0);
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
        
        public QueryTermNode QueryNode {
            get {
                QueryTermNode node = new QueryTermNode ();
                node.Field = field;
                node.Operator = op;
                node.Value = value_entry.QueryValue;
                return node;
            }
            set {
                if (value == null) {
                    return;
                }

                field_chooser.Active = Array.IndexOf (sorted_fields, value.Field);
                value_entry.QueryValue = value.Value;
                op_chooser.Active = Array.IndexOf (value.Value.OperatorSet.Objects, value.Operator);
            }
        }
    }
}
