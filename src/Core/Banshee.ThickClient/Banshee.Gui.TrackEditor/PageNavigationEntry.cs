//
// PageNavigationEntry.cs
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
using Mono.Unix;
using Gtk;

namespace Banshee.Gui.TrackEditor
{
    public class PageNavigationEntry : HBox, IEditorField, ICanUndo
    {
        public event EventHandler Changed;
        
        private TextEntry entry;
        private TrackEditorDialog dialog;
        private Button forward_button;
        public Button ForwardButton {
            get { return forward_button; }
        }
        
        public PageNavigationEntry (TrackEditorDialog dialog) : this (dialog, null, null)
        {
        }
        
        public PageNavigationEntry (TrackEditorDialog dialog, string completionTable, string completionColumn)
        {
            this.dialog = dialog;
            entry = new TextEntry (completionTable, completionColumn);
            entry.Changed += OnChanged;
            entry.Activated += EditNextTitle;
            entry.KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
                if ((args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter) && 
                    (args.Event.State & Gdk.ModifierType.ControlMask) != 0 && dialog.CanGoBackward) {
                    dialog.NavigateBackward ();
                    entry.GrabFocus ();
                }
            };
            entry.Show ();
            
            Spacing = 1;
            PackStart (entry, true, true, 0);
            
            if (dialog.TrackCount > 1) {
                dialog.Navigated += delegate { 
                    forward_button.Sensitive = dialog.CanGoForward; 
                };
                forward_button = EditorUtilities.CreateSmallStockButton (Stock.GoForward);
                object tooltip_host = Hyena.Gui.TooltipSetter.CreateHost ();
                Hyena.Gui.TooltipSetter.Set (tooltip_host, forward_button, Catalog.GetString ("Advance to the next track and edit its title"));
                forward_button.Sensitive = dialog.CanGoForward;
                forward_button.Show ();
                forward_button.Clicked += EditNextTitle;
                PackStart (forward_button, false, false, 0);
            }
        }

        private void EditNextTitle (object o, EventArgs args)
        {
            if (dialog.CanGoForward) {
                dialog.NavigateForward ();
                entry.GrabFocus ();
            }
        }
        
        public void ConnectUndo (EditorTrackInfo track)
        {
            entry.ConnectUndo (track);
        }
        
        public void DisconnectUndo ()
        {
            entry.DisconnectUndo ();
        }
                
        private void OnChanged (object o, EventArgs args)
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        protected override bool OnMnemonicActivated (bool group_cycling) {
            return entry.MnemonicActivate(group_cycling);
        }
    
        public TextEntry Entry {
            get { return entry; }
        }
        
        public string Text {
            get { return entry.Text; }
            set { entry.Text = value; }
        }
    }
}
