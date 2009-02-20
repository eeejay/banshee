//
// BpmEntry.cs
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
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Gui;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection.Database;

using Banshee.Gui;
using Banshee.Gui.TrackEditor;

namespace Banshee.Bpm
{
    public class BpmEntry : HBox, ICanUndo, IEditorField
    {
        private SpinButton bpm_entry;
        private Button detect_button;
        private BpmTapAdapter tap_adapter;
        private EditorTrackInfo track;
        private IBpmDetector detector;
        private EditorEditableUndoAdapter<Entry> undo_adapter = new EditorEditableUndoAdapter<Entry> ();

        public event EventHandler Changed;
        
        public BpmEntry ()
        {
            detector = BpmDetectJob.GetDetector ();
            if (detector != null) {
                detector.FileFinished += OnFileFinished;
            }

            BuildWidgets ();

            Destroyed += delegate {
                if (detector != null) {
                    detector.Dispose ();
                    detector = null;
                }
            };
        }

        private void BuildWidgets ()
        {
            Spacing = 6;

            bpm_entry = new SpinButton (0, 9999, 1);
            bpm_entry.MaxLength = bpm_entry.WidthChars = 4;
            bpm_entry.Digits = 0;
            bpm_entry.Numeric = true;
            bpm_entry.ValueChanged += OnChanged;
            Add (bpm_entry);

            if (detector != null) {
                detect_button = new Button (Catalog.GetString ("D_etect"));
                detect_button.Clicked += OnDetectClicked;
                Add (detect_button);
            }

            Image play = new Image ();
            play.IconName = "media-playback-start";;
            play.IconSize = (int)IconSize.Menu;
            Button play_button = new Button (play);
            play_button.Clicked += OnPlayClicked;
            Add (play_button);

            Button tap_button = new Button (Catalog.GetString ("T_ap"));
            tap_adapter = new BpmTapAdapter (tap_button);
            tap_adapter.BpmChanged += OnTapBpmChanged;
            Add (tap_button);

            object tooltip_host = TooltipSetter.CreateHost ();

            TooltipSetter.Set (tooltip_host, detect_button,
                Catalog.GetString ("Have Banshee attempt to auto-detect the BPM of this song"));

            TooltipSetter.Set (tooltip_host, play_button, Catalog.GetString ("Play this song"));

            TooltipSetter.Set (tooltip_host, tap_button,
                Catalog.GetString ("Tap this button to the beat to set the BPM for this song manually"));

            ShowAll ();
        }
        
        public void DisconnectUndo ()
        {
            undo_adapter.DisconnectUndo ();
        }
        
        public void ConnectUndo (EditorTrackInfo track)
        {
            this.track = track;
            tap_adapter.Reset ();
            undo_adapter.ConnectUndo (bpm_entry, track);
        }
        
        public int Bpm {
            get { return bpm_entry.ValueAsInt; }
            set { bpm_entry.Value = value; }
        }

        protected override bool OnMnemonicActivated (bool group_cycling) {
            return bpm_entry.MnemonicActivate(group_cycling);
        }

        private void OnChanged (object o, EventArgs args)
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void OnTapBpmChanged (int bpm)
        {
            Console.WriteLine ("Got Tap Bpm changed: {0}", bpm);
            Bpm = bpm;
            OnChanged (null, null);
        }

        private void OnDetectClicked (object o, EventArgs args)
        {
            if (track != null) {
                detect_button.Sensitive = false;
                detector.ProcessFile (track.Uri);
            }
        }

        private void OnFileFinished (SafeUri uri, int bpm)
        {
            ThreadAssist.ProxyToMain (delegate {
                detect_button.Sensitive = true;

                if (track.Uri != uri || bpm == 0) {
                    return;
                }

                Log.DebugFormat ("Detected BPM of {0} for {1}", bpm, uri);
                Bpm = bpm;
                OnChanged (null, null);
            });
        }

        private void OnPlayClicked (object o, EventArgs args)
        {
            if (track != null) {
                ServiceManager.PlayerEngine.Open (track);
                Gtk.ActionGroup actions = ServiceManager.Get<InterfaceActionService> ().PlaybackActions;
                (actions["StopWhenFinishedAction"] as Gtk.ToggleAction).Active = true;
            }
        }
    }
}
