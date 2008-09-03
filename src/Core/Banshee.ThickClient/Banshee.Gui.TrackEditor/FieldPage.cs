//
// FieldPage.cs
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
using System.Collections.Generic;

using Gtk;

using Banshee.Collection;

namespace Banshee.Gui.TrackEditor
{
    public class FieldPage : VBox
    {
        public delegate string FieldLabelClosure (EditorTrackInfo track, Widget widget);
        public delegate void FieldValueClosure (EditorTrackInfo track, Widget widget);
        
        private TrackEditorDialog dialog;
        protected TrackEditorDialog Dialog {
            get { return dialog; }
        }
        
        private EditorTrackInfo current_track;
        protected EditorTrackInfo CurrentTrack {
            get { return current_track; }
        }
        
        private struct FieldSlot
        {
            public Widget Label;
            public Widget Field;
            public Button SyncButton;
            public FieldLabelClosure LabelClosure;
            public FieldValueClosure ReadClosure;
            public FieldValueClosure WriteClosure;
        }
        
        private List<FieldSlot> field_slots = new List<FieldSlot> ();
        
        public FieldPage ()
        {
            Spacing = EditorUtilities.RowSpacing;
        }
        
        public void Initialize (TrackEditorDialog dialog)
        {
            this.dialog = dialog;
            AddFields ();
        }
        
        protected virtual void AddFields ()
        {
        }
        
        public virtual bool MultipleTracks {
            get { return dialog.TrackCount > 1; }
        }
        
        public virtual Widget Widget {
            get { return this; }
        }
        
        public Gtk.Widget TabWidget {
            get { return null; }
        }
        
        public virtual PageType PageType { 
            get { return PageType.Edit; }
        }
    
        public void AddField (Box parent, Widget field, FieldLabelClosure labelClosure, 
            FieldValueClosure readClosure, FieldValueClosure writeClosure)
        {
            AddField (parent, EditorUtilities.CreateLabel (String.Empty), field, 
                labelClosure, readClosure, writeClosure, FieldOptions.None);
        }
        
        public void AddField (Box parent, Widget field, FieldLabelClosure labelClosure, 
            FieldValueClosure readClosure, FieldValueClosure writeClosure, FieldOptions options)
        {
            AddField (parent, EditorUtilities.CreateLabel (String.Empty), field, 
                labelClosure, readClosure, writeClosure, options);
        }
        
        public void AddField (Box parent, Widget label, Widget field, FieldLabelClosure labelClosure, 
            FieldValueClosure readClosure, FieldValueClosure writeClosure)
        {
            AddField (parent, label, field, labelClosure, readClosure, writeClosure, FieldOptions.None);
        }
        
        public void AddField (Box parent, Widget label, Widget field, FieldLabelClosure labelClosure, 
            FieldValueClosure readClosure, FieldValueClosure writeClosure, FieldOptions options)
        {
            FieldSlot slot = new FieldSlot ();
            
            slot.Label = label;
            slot.Field = field;
            slot.LabelClosure = labelClosure;
            slot.ReadClosure = readClosure;
            slot.WriteClosure = writeClosure;
            if (MultipleTracks && (options & FieldOptions.NoSync) == 0) {
                slot.SyncButton = new SyncButton ();
                slot.SyncButton.Clicked += delegate {
                    dialog.ForeachNonCurrentTrack (delegate (EditorTrackInfo track) {
                        slot.WriteClosure (track, slot.Field);
                    });
                };
            }
            
            field_slots.Add (slot);
            
            Table table = new Table (1, 1, false);
            table.ColumnSpacing = 1;
            
            table.Attach (field, 0, 1, 1, 2, 
                AttachOptions.Expand | AttachOptions.Fill, 
                AttachOptions.Fill, 0, 0);
                
            IEditorField editor_field = field as IEditorField;
            if (editor_field != null) {
                editor_field.Changed += delegate {
                    if (CurrentTrack != null) {
                        slot.WriteClosure (CurrentTrack, slot.Field);
                    }
                };
            }
            
            if (slot.SyncButton != null) {
                table.Attach (slot.SyncButton, 1, 2, 1, 2, 
                    AttachOptions.Fill, 
                    AttachOptions.Fill, 0, 0);
            }
            
            table.Attach (label, 0, table.NColumns, 0, 1,
                AttachOptions.Fill | AttachOptions.Expand, 
                AttachOptions.Fill, 0, 0);
                
            table.ShowAll ();
            
            if ((options & FieldOptions.Shrink) == 0) {
                parent.PackStart (table, false, false, 0);
            } else {
                HBox shrink = new HBox ();
                shrink.Show ();
                shrink.PackStart (table, false, false, 0);
                parent.PackStart (shrink, false, false, 0);
            }
        }
        
        public virtual void LoadTrack (EditorTrackInfo track)
        {
            current_track = null;
            
            foreach (FieldSlot slot in field_slots) {
                UpdateLabel (track, slot);
                slot.ReadClosure (track, slot.Field);
            }
            
            current_track = track;
        }
        
        private void UpdateLabel (EditorTrackInfo track, FieldSlot slot)
        {
            string value = slot.LabelClosure (track, slot.Label);
            Label label = slot.Label as Label;
            if (value != null && label != null) {
                label.Text = value;
            }
        }
    }
}
