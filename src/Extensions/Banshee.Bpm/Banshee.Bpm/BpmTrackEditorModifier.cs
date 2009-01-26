//
// BpmTrackEditorModifier.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using Mono.Unix;
using Gtk;

using Hyena;
using Banshee.Base;
using Banshee.Gui.TrackEditor;

namespace Banshee.Bpm
{
    public class BpmTrackEditorModifier : ITrackEditorModifier
    {
        public BpmTrackEditorModifier ()
        {
        }

        public void Modify (TrackEditorDialog dialog)
        {
            for (int i = 0; i < dialog.Notebook.NPages; i++) {
                Widget page = dialog.Notebook.GetNthPage (i);
                ExtraTrackDetailsPage extra_page = page as ExtraTrackDetailsPage;
                if (extra_page != null) {
                    foreach (FieldPage.FieldSlot slot in extra_page.FieldSlots) {
                        // TODO this ia hacky - what if another Modifier added a 2nd SpinButton?
                        if (slot.Field is Gtk.SpinButton) {
                            // Remove it
                            (slot.Parent as Container).Remove (slot.Container);
                            extra_page.RemoveField (slot);

                            // Add our new and improved version back
                            extra_page.AddField (slot.Parent as Gtk.Box,
                                new BpmEntry (), null,
                                slot.LabelClosure,
                                delegate (EditorTrackInfo track, Widget widget) {
                                    ((BpmEntry)widget).Bpm = track.Bpm; },
                                delegate (EditorTrackInfo track, Widget widget) {
                                    track.Bpm = ((BpmEntry)widget).Bpm; },
                                FieldOptions.Shrink | FieldOptions.NoSync
                            );
                            break;
                        }
                    }

                    break;
                }
            }
        }
    }
}
