//
// TrackEditorDialog.cs
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

using Mono.Unix;
using Mono.Addins;
using Gtk;

using Hyena.Gui;
using Hyena.Widgets;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;

using Banshee.Gui.Dialogs;
using Banshee.Collection.Gui;

namespace Banshee.Gui.TrackEditor
{
    public class TrackEditorDialog : BansheeDialog
    {
        public static void RunEdit (TrackListModel model)
        {
            Run (model, EditorMode.Edit);
        }
        
        public static void RunView (TrackListModel model)
        {
            Run (model, EditorMode.View);
        }
        
        public static void Run (TrackListModel model, EditorMode mode)
        {
            TrackEditorDialog track_editor = new TrackEditorDialog (model, mode);
            try {
                track_editor.Run ();
            } finally {
                track_editor.Destroy ();
            }
        }
    
        public delegate void EditorTrackOperationClosure (EditorTrackInfo track);
    
        private VBox main_vbox;
    
        private Frame header_image_frame;
        private Image header_image;
        private Label header_title_label;
        private Label header_artist_label;
        private Label header_album_label;
        private Label edit_notif_label;
        private object tooltip_host;
        
        private Notebook notebook;
        public Notebook Notebook {
            get { return notebook; }
        }
    
        private Button nav_backward_button;
        private Button nav_forward_button;
        private PulsingButton sync_all_button;
        
        private EditorMode mode;
        
        private List<ITrackEditorPage> pages = new List<ITrackEditorPage> ();
        
        public event EventHandler Navigated;
        
        public TrackEditorDialog (TrackListModel model, EditorMode mode) : base (Catalog.GetString ("Track Editor"))
        {
            this.mode = mode;
            
            LoadTrackModel (model);
        
            BorderWidth = 6;
            
            if (mode == EditorMode.Edit) {
                WidthRequest = 525;
                AddStockButton (Stock.Cancel, ResponseType.Cancel);
                AddStockButton (Stock.Save, ResponseType.Ok);
            } else {
                AddStockButton (Stock.Close, ResponseType.Close, true);
                SetSizeRequest (400, 500);
            }
            
            tooltip_host = TooltipSetter.CreateHost ();
            
            AddNavigationButtons ();
            
            main_vbox = new VBox ();
            main_vbox.Spacing = 10;
            main_vbox.BorderWidth = 6;
            main_vbox.Show ();
            VBox.PackStart (main_vbox, true, true, 0);
            
            BuildHeader ();
            BuildNotebook ();
            BuildFooter ();
            
            LoadTrackToEditor ();
        }
        
#region UI Building

        private void AddNavigationButtons ()
        {
            if (TrackCount <= 1) {
                return;
            }
            
            nav_backward_button = new Button (Stock.GoBack);
            nav_backward_button.UseStock = true;
            nav_backward_button.Clicked += delegate { NavigateBackward (); };
            nav_backward_button.Show ();
            TooltipSetter.Set (tooltip_host, nav_backward_button, Catalog.GetString ("Show the previous track"));
            
            nav_forward_button = new Button (Stock.GoForward);
            nav_forward_button.UseStock = true;
            nav_forward_button.Clicked += delegate { NavigateForward (); };
            nav_forward_button.Show ();
            TooltipSetter.Set (tooltip_host, nav_forward_button, Catalog.GetString ("Show the next track"));
            
            ActionArea.PackStart (nav_backward_button, false, false, 0);
            ActionArea.PackStart (nav_forward_button, false, false, 0);
            ActionArea.SetChildSecondary (nav_backward_button, true);
            ActionArea.SetChildSecondary (nav_forward_button, true);
        }
        
        private void BuildHeader ()
        {
            Table header = new Table (3, 3, false);
            header.ColumnSpacing = 5;
            
            header_image_frame = new Frame ();
            
            header_image = new Image ();
            header_image.IconName = "media-optical";
            header_image.PixelSize = 64;
            header_image_frame.Add (header_image);
            
            header.Attach (header_image_frame, 0, 1, 0, 3,
                AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
            
            AddHeaderRow (header, 0, Catalog.GetString ("Title:"), out header_title_label);
            AddHeaderRow (header, 1, Catalog.GetString ("Artist:"), out header_artist_label);
            AddHeaderRow (header, 2, Catalog.GetString ("Album:"), out header_album_label);
          
            header.ShowAll ();
            main_vbox.PackStart (header, false, false, 0);
        }
        
        private void AddHeaderRow (Table header, uint row, string title, out Label label)
        {
            Label title_label = new Label ();
            title_label.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (title));
            title_label.Xalign = 0.0f;
            header.Attach (title_label, 1, 2, row, row + 1,
                AttachOptions.Fill, AttachOptions.Expand, 0, 0);
            
            label = new Label ();
            label.Xalign = 0.0f;
            header.Attach (label, 2, 3, row, row + 1, 
                AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Expand, 0, 0);
        }
        
        private void BuildNotebook ()
        {
            notebook = new Notebook ();
            notebook.Show ();
            
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Banshee/Gui/TrackEditor/NotebookPage")) {
                try {
                    ITrackEditorPage page = (ITrackEditorPage)node.CreateInstance ();
                    if ((mode == EditorMode.Edit && (page.PageType == PageType.Edit || page.PageType == PageType.View)) ||
                        (mode == EditorMode.View && (page.PageType == PageType.View || page.PageType == PageType.ViewOnly))) {
                        pages.Add (page);
                        page.Initialize (this);
                        page.Widget.Show ();
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception ("Invalid NotebookPage extension node. Should implement ITrackEditorPage.", e);
                }
            }
            
            pages.Sort (delegate (ITrackEditorPage a, ITrackEditorPage b) { return a.Order.CompareTo (b.Order); });
            foreach (ITrackEditorPage page in pages) {
                Container container = page.Widget as Container;
                if (container == null) {
                    VBox box = new VBox ();
                    box.PackStart (page.Widget, true, true, 0);
                    container = box;
                }
                container.BorderWidth = 12;
                notebook.AppendPage (container, page.TabWidget == null ? new Label (page.Title) : page.TabWidget);
            }
            
            main_vbox.PackStart (notebook, true, true, 0);
        }
        
        private void BuildFooter ()
        {
            if (mode == EditorMode.View) {
                return;
            }
        
            HBox button_box = new HBox ();
            button_box.Spacing = 6;
                       
            if (TrackCount > 1) {
                sync_all_button = new PulsingButton ();
                sync_all_button.FocusInEvent += delegate {
                    ForeachSyncButton (delegate (SyncButton button) {
                        button.StartPulsing ();
                    });
                };
                sync_all_button.FocusOutEvent += delegate {
                    ForeachSyncButton (delegate (SyncButton button) {
                        button.StopPulsing ();
                    });
                };
                sync_all_button.StateChanged += delegate {
                    ForeachSyncButton (delegate (SyncButton button) {
                        if (sync_all_button.State == StateType.Prelight) {
                            button.StartPulsing ();
                        } else {
                            button.StopPulsing ();
                        }
                    });
                };
                sync_all_button.Clicked += delegate {
                    InvokeFieldSync ();
                };
                
                Alignment alignment = new Alignment (0.5f, 0.5f, 0.0f, 0.0f);
                HBox box = new HBox ();
                box.Spacing = 2;
                box.PackStart (new Image (Stock.Copy, IconSize.Button), false, false, 0);
                box.PackStart (new Label (Catalog.GetString ("Sync all field values")), false, false, 0);
                alignment.Add (box);
                sync_all_button.Add (alignment);
                
                TooltipSetter.Set (tooltip_host, sync_all_button, Catalog.GetString (
                    "Apply the values of all common fields set for this track to all of the tracks selected in this editor"));
                    
                button_box.PackStart (sync_all_button, false, false, 0);
                
                foreach (Widget child in ActionArea.Children) {
                    child.SizeAllocated += OnActionAreaChildSizeAllocated;
                }
                
                edit_notif_label = new Label ();
                edit_notif_label.Xalign = 1.0f;
                button_box.PackEnd (edit_notif_label, false, false, 0);
            }
            
            main_vbox.PackStart (button_box, false, false, 0);
            button_box.ShowAll ();
        }
        
        private delegate void SyncButtonAction (SyncButton button);
        
        private void ForeachSyncButton (SyncButtonAction action)
        {
            for (int i = 0; i < notebook.NPages; i++) {
                ForeachSyncButton (notebook.GetNthPage (i) as Container, action);
            }     
        }
        
        private void ForeachSyncButton (Container container, SyncButtonAction action)
        {
            if (container == null) {
                return;
            }
            
            foreach (Widget child in container.Children) {
                SyncButton sync = child as SyncButton;
                if (sync != null) {
                    action (sync);
                } else {
                    Container child_container = child as Container;
                    if (child_container != null) {
                        ForeachSyncButton (child_container, action);
                    }
                }
            }
        }
        
        private void InvokeFieldSync ()
        {
            ForeachSyncButton (delegate (SyncButton button) {
                button.Click ();
            });
        }
        
        private int action_area_children_allocated = 0;
        
        private void OnActionAreaChildSizeAllocated (object o, SizeAllocatedArgs args)
        {
            Widget [] children = ActionArea.Children;
            if (++action_area_children_allocated != children.Length) {
                return;
            }
            
            sync_all_button.WidthRequest = (children[1].Allocation.X + 
                children[1].Allocation.Width) - children[0].Allocation.X - 1;
        }

#endregion
        
#region Track Model/Changes API
        
        private CachedList<DatabaseTrackInfo> db_selection;
        private List<TrackInfo> memory_selection;
        private Dictionary<TrackInfo, EditorTrackInfo> edit_map = new Dictionary<TrackInfo, EditorTrackInfo> ();
        private int current_track_index;
        
        protected void LoadTrackModel (TrackListModel model)
        {
            DatabaseTrackListModel db_model = model as DatabaseTrackListModel;
            if (db_model != null) {
                db_selection = CachedList<DatabaseTrackInfo>.CreateFromModelSelection (db_model);
            } else {
                memory_selection = new List<TrackInfo> ();
                foreach (TrackInfo track in model.SelectedItems) {
                    memory_selection.Add (track);
                }
            }
        }
        
        public void LoadTrackToEditor ()
        {
            TrackInfo current_track = null;
            EditorTrackInfo editor_track = LoadTrack (current_track_index, out current_track);
            if (editor_track == null) {
                return;
            }
            
            // Update the Header
            header_title_label.Text = current_track.DisplayTrackTitle;
            header_artist_label.Text = current_track.DisplayArtistName;
            header_album_label.Text = current_track.DisplayAlbumTitle;
            
            if (edit_notif_label != null) {
                edit_notif_label.Markup = String.Format (Catalog.GetString ("<i>Editing {0} of {1} items</i>"), 
                    CurrentTrackIndex + 1, TrackCount);
            }
            
            ArtworkManager artwork = ServiceManager.Get<ArtworkManager> ();
            Gdk.Pixbuf cover_art = artwork.LookupScale (current_track.ArtworkId, 64);
            header_image.Pixbuf = cover_art;
            if (cover_art == null) {
                header_image.IconName = "media-optical";
                header_image.PixelSize = 64;
                header_image_frame.ShadowType = ShadowType.None;
            } else {
                header_image_frame.ShadowType = ShadowType.In;
            }
            
            foreach (ITrackEditorPage page in pages) {
                page.LoadTrack (editor_track);
            }
            
            // Update Navigation
            if (TrackCount > 0 && nav_backward_button != null && nav_forward_button != null) {
                nav_backward_button.Sensitive = CanGoBackward;
                nav_forward_button.Sensitive = CanGoForward;
            }
        }
        
        public void ForeachNonCurrentTrack (EditorTrackOperationClosure closure)
        {
            for (int i = 0; i < TrackCount; i++) {
                if (i == current_track_index) {
                    continue;
                }
                
                EditorTrackInfo track = LoadTrack (i);
                if (track != null) {
                    closure (track);
                }
            }
        }
        
        public EditorTrackInfo LoadTrack (int index)
        {
            TrackInfo source_track;
            return LoadTrack (index, out source_track);
        }
        
        private EditorTrackInfo LoadTrack (int index, out TrackInfo sourceTrack)
        {
            sourceTrack = GetTrack (index);
            EditorTrackInfo editor_track = null;
            
            if (sourceTrack == null) {
                // Something bad happened here
                return null;
            }
            
            if (!edit_map.TryGetValue (sourceTrack, out editor_track)) {
                editor_track = new EditorTrackInfo (sourceTrack);
                editor_track.EditorIndex = index;
                editor_track.EditorCount = TrackCount;
                edit_map.Add (sourceTrack, editor_track);
            }
            
            return editor_track;
        }
        
        private TrackInfo GetTrack (int index)
        {
            return db_selection != null ? db_selection[index] : memory_selection[index];
        }
        
        protected virtual void OnNavigated ()
        {
            EventHandler handler = Navigated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public void NavigateForward ()
        {
            if (current_track_index < TrackCount - 1) {
                current_track_index++;
                LoadTrackToEditor ();
                OnNavigated ();
            }
        }
        
        public void NavigateBackward ()
        {
            if (current_track_index > 0) {
                current_track_index--;
                LoadTrackToEditor ();
                OnNavigated ();
            }
        }
        
        public int TrackCount {
            get { return db_selection != null ? db_selection.Count : memory_selection.Count; }
        }
        
        public int CurrentTrackIndex {
            get { return current_track_index; }
        }
        
        public bool CanGoBackward {
            get { return current_track_index > 0; }
        }
        
        public bool CanGoForward {
            get { return current_track_index >= 0 && current_track_index < TrackCount - 1; }
        }

#endregion

#region Saving
        
       /* private void OnSaveButtonClicked (object o, EventArgs args)
        {
            List<int> primary_sources = new List<int> ();
            
            // TODO wrap in db transaction
            try {
                DatabaseTrackInfo.NotifySaved = false;

                primary_sources.Clear ();
                foreach (EditorTrack track in TrackSet) {
                    SaveTrack (track);

                    if (track.Track is DatabaseTrackInfo) {
                        int id = (track.Track as DatabaseTrackInfo).PrimarySourceId;
                        if (!primary_sources.Contains (id)) {
                            primary_sources.Add (id);
                        }
                    }
                }
                
                EventHandler handler = Saved;
                if (handler != null) {
                    handler (this, new EventArgs ());
                }

                // Finally, notify the affected primary sources
                foreach (int id in primary_sources) {
                    PrimarySource.GetById (id).NotifyTracksChanged ();
                }
            } finally {
                DatabaseTrackInfo.NotifySaved = true;
            }
            
            Window.Destroy ();
        }
        
        private void SaveTrack (EditorTrack track)
        {
            track.Save ();
            
            track.Track.Save ();
                
            if (LibrarySchema.WriteMetadata.Get ()) {
                SaveToFile (track);
            }

            if (LibrarySchema.MoveOnInfoSave.Get ()) {
                MoveSavedFile (track);
            }

            if (track.Track == ServiceManager.PlayerEngine.CurrentTrack) {
                ServiceManager.PlayerEngine.TrackInfoUpdated ();
            }
        }
        
        private void SaveToFile (EditorTrack track)
        {
            Banshee.Kernel.Scheduler.Schedule (new SaveTrackMetadataJob (track.Track), Banshee.Kernel.JobPriority.Highest);
        }

        private void MoveSavedFile (EditorTrack track)
        {
            Banshee.Kernel.Scheduler.Schedule (new MoveOnInfoSaveJob (track.Track), Banshee.Kernel.JobPriority.Highest);
        }*/

#endregion

    }
}
