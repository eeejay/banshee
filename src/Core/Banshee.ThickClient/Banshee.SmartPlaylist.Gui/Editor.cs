using System;
using System.Collections;
using Gtk;
using Glade;

using Mono.Unix;

using Hyena.Query;
using Hyena.Query.Gui;
 
using Banshee.Base;
using Banshee.Query;
using Banshee.ServiceStack;
using Banshee.Widgets;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Gui.Dialogs;
using Banshee.Query.Gui;

namespace Banshee.SmartPlaylist
{
    public class Editor : GladeDialog
    {
        private BansheeQueryBox builder;
        private SmartPlaylistSource playlist = null;

        private static SmartPlaylistSource currently_editing;
        public static SmartPlaylistSource CurrentlyEditing {
            get { return currently_editing; }
        }

        [Widget] private Gtk.Entry name_entry;
        [Widget] private Gtk.VBox builder_box;
        [Widget] private Gtk.Button ok_button;
        [Widget] private Gtk.TreeView adv_tree_view;
        [Widget] private Gtk.Button adv_use_button;
        [Widget] private Gtk.Button adv_add_button;

        public Editor (SmartPlaylistSource playlist) : base ("SmartPlaylistEditorDialog")
        {
            currently_editing = playlist;
            this.playlist = playlist;
            /*Console.WriteLine ("Loading smart playlist into editor: {0}",
                playlist.ConditionTree == null ? "" : playlist.ConditionTree.ToXml (BansheeQuery.FieldSet, true));*/

            Initialize ();

            Dialog.Title = Catalog.GetString ("Edit Smart Playlist");

            name_entry.Text = playlist.Name;

            Condition = playlist.ConditionTree;
            Limit = playlist.Limit;
            LimitValue = playlist.LimitValue;
            Order = playlist.QueryOrder;
            LimitEnabled = playlist.IsLimited;
        }
    
        public Editor () : base ("SmartPlaylistEditorDialog")
        {
            Initialize ();
        }

        private void Initialize ()
        {
            Dialog.Title = Catalog.GetString ("New Smart Playlist");

            builder = new BansheeQueryBox ();

            builder.Show ();
            builder.Spacing = 4;

            builder_box.PackStart (builder, true, true, 0);

            name_entry.Changed += HandleNameChanged;

            // Model is Name, Condition, OrderBy, LimitNumber, LimitCriterion
            ListStore list_model = new ListStore (typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

            // FIXME this is broken
            /*list_model.AppendValues (
                Catalog.GetString ("Neglected Favorites"),
                "Rating>3 played<=\"2 weeks ago\"",
                null, "0", null);*/

            // TODO this one is broken, not supported by the condition GUI
            /*list_model.AppendValues (
                Catalog.GetString ("Unrated"),
                " (Rating = NULL) ",
                null, "0", 0);*/

            list_model.AppendValues (
                Catalog.GetString ("700 MB of Favorites"),
                "rating>3",
                "PlayCount-DESC",
                "700",
                "MB"
            );

            list_model.AppendValues (
                Catalog.GetString ("80 Minutes of Favorites"),
                "rating>3",
                "PlayCount-DESC",
                "80",
                "minutes"
            );

            list_model.AppendValues (
                Catalog.GetString ("Unheard"),
                "playcount=0",
                null,
                "0",
                null
            );

            list_model.AppendValues (
                Catalog.GetString ("Unheard Podcasts"),
                "playcount=0 location:podcast",
                null,
                "0",
                null
            );

            adv_tree_view.Selection.Mode = SelectionMode.Multiple;
            adv_tree_view.Model = list_model;
            adv_tree_view.AppendColumn ("title", new CellRendererText (), "text", 0);
            adv_tree_view.Selection.Changed += HandleAdvSelectionChanged;

            UpdateAdvButtons (0);

            adv_add_button.Clicked += HandleAdvAdd;
            adv_use_button.Clicked += HandleAdvUse;

            Update ();
            
            name_entry.GrabFocus ();
        }

        /*public void SetQueryFromSearch ()
        {
            Banshee.Widgets.SearchEntry search_entry = InterfaceElements.SearchEntry;

            TrackFilterType filter_type = (TrackFilterType)search_entry.ActiveFilterID;
            string query = search_entry.Query;

            string condition = String.Empty;
            ArrayList condition_candidates = new ArrayList ();

            QueryFilter FilterContains = QueryFilter.Contains;
            QueryFilter FilterIs       = QueryFilter.Is;

            condition_candidates.Add (FilterContains.Operator.FormatValues (true, "Artist", query, null) );
            condition_candidates.Add (FilterContains.Operator.FormatValues (true, "Title", query, null) );
            condition_candidates.Add (FilterContains.Operator.FormatValues (true, "AlbumTitle", query, null) );
            condition_candidates.Add (FilterContains.Operator.FormatValues (true, "Genre", query, null) );

            // only search for years if the query is a number
            try {
                int.Parse (query);
                condition_candidates.Add (FilterIs.Operator.FormatValues (false, "Year", query, null) );
            }
            catch {
                //Console.WriteLine ("{0} is not a valid year", query);
                condition_candidates.Add (String.Empty);
            }

            if((filter_type & TrackFilterType.ArtistName) == TrackFilterType.ArtistName) {
                condition = " (" + condition_candidates[0].ToString() + ") ";
            } else if((filter_type & TrackFilterType.SongName) == TrackFilterType.SongName) {
                condition = " (" + condition_candidates[1].ToString() + ") ";
            } else if((filter_type & TrackFilterType.AlbumTitle) == TrackFilterType.AlbumTitle) {
                condition = " (" + condition_candidates[2].ToString() + ") ";
            } else if((filter_type & TrackFilterType.Genre) == TrackFilterType.Genre) {
                condition = " (" + condition_candidates[3].ToString() + ") ";
            } else if((filter_type & TrackFilterType.Year) == TrackFilterType.Year) {
                condition = " (" + condition_candidates[4].ToString() + ") ";
            } else {
                // Searching for all possible conditions
                for(int i = 0; i < condition_candidates.Count; i++) {
                    string c = condition_candidates[i].ToString();
                    if (c.Length > 0) {
                        if (i > 0)
                            condition += "OR";
                        
                        condition += " (" + c  + ") ";
                    }
                }
            }

            //Condition = condition;

            Dialog.Title = Catalog.GetString ("Create Smart Playlist from Search");
            name_entry.Text = search_entry.GetLabelForFilterID(search_entry.ActiveFilterID) + ": " + query;
        }*/

        public void RunDialog ()
        {
            Run ();
            Dialog.Destroy ();
        }

        public override ResponseType Run ()
        {
            Dialog.ShowAll ();

            ResponseType response = (ResponseType)Dialog.Run ();

            //int w = -1, h = -1;
            //dialog.GetSize (out w, out h);
            //Console.WriteLine ("w = {0}, h = {1}", w, h);

            QueryNode node = builder.QueryNode;
            if (node == null) {
                //Console.WriteLine ("Editor query is null");
            } else {
                //Console.WriteLine ("Editor query is: {0}", node.ToXml (BansheeQuery.FieldSet, true));
            }

            if (response == ResponseType.Ok) {
                string name = PlaylistName;
                QueryNode condition_tree = Condition;
                QueryLimit limit = Limit;
                QueryOrder order = Order;
                IntegerQueryValue limit_value = LimitValue;

                ThreadAssist.Spawn (delegate {
                    //Console.WriteLine ("Name = {0}, Cond = {1}, OrderAndLimit = {2}", name, condition, order_by, limit_number);
                    if (playlist == null) {
                        playlist = new SmartPlaylistSource (name);

                        playlist.ConditionTree = condition_tree;
                        playlist.QueryOrder = order;
                        playlist.Limit = limit;
                        playlist.LimitValue = limit_value;

                        playlist.Save ();
                        ServiceManager.SourceManager.DefaultSource.AddChildSource (playlist);
                        playlist.RefreshAndReload ();
                        //SmartPlaylistCore.Instance.StartTimer (playlist);
                    } else {
                        playlist.ConditionTree = condition_tree;
                        playlist.QueryOrder = order;
                        playlist.LimitValue = limit_value;
                        playlist.Limit = limit;

                        playlist.Rename (name);
                        playlist.Save ();
                        playlist.RefreshAndReload ();

                        /*if (playlist.TimeDependent)
                            SmartPlaylistCore.Instance.StartTimer (playlist);
                        else
                            SmartPlaylistCore.Instance.StopTimer ();*/

                        //playlist.ListenToPlaylists ();
                        //SmartPlaylistCore.Instance.SortPlaylists ();
                    }
                });
            }

            currently_editing = null;
            return response;
        }

        private void HandleAdvSelectionChanged (object sender, EventArgs args)
        {
            TreeSelection selection = sender as TreeSelection;
            UpdateAdvButtons (selection.CountSelectedRows ());
        }

        private void UpdateAdvButtons (int num)
        {
            adv_use_button.Sensitive = (num == 1);
            adv_add_button.Sensitive = (num > 0);
        }

        private void HandleAdvAdd (object sender, EventArgs args)
        {
            TreePath [] paths = adv_tree_view.Selection.GetSelectedRows ();

            foreach (TreePath path in paths) {
                TreeIter iter;
                if (adv_tree_view.Model.GetIter (out iter, path)) {
                    string name            = adv_tree_view.Model.GetValue (iter, 0) as string;
                    UserQueryParser parser = new UserQueryParser (adv_tree_view.Model.GetValue (iter, 1) as string);
                    QueryNode condition    = parser.BuildTree (BansheeQuery.FieldSet);
                    QueryOrder order       = BansheeQuery.FindOrder (adv_tree_view.Model.GetValue (iter, 2) as string);
                    IntegerQueryValue val  = new IntegerQueryValue ();
                    val.ParseUserQuery (adv_tree_view.Model.GetValue (iter, 3) as string);
                    QueryLimit limit       = BansheeQuery.FindLimit (adv_tree_view.Model.GetValue (iter, 4) as string);

                    SmartPlaylistSource pl = new SmartPlaylistSource (name, condition, order, limit, val);
                    pl.Save ();
                    //Banshee.Sources.LibrarySource.Instance.AddChildSource (pl);
                    ServiceManager.SourceManager.DefaultSource.AddChildSource (pl);
                    pl.RefreshAndReload ();
                    //SmartPlaylistCore.Instance.StartTimer (pl);
                }
            }

            currently_editing = null;
            Dialog.Destroy ();
        }

        private void HandleAdvUse (object sender, EventArgs args)
        {
            TreePath [] paths = adv_tree_view.Selection.GetSelectedRows ();

            if (paths != null && paths.Length != 1)
                return;

            TreeIter iter;
            
            if (adv_tree_view.Model.GetIter (out iter, paths[0])) {
                PlaylistName     = adv_tree_view.Model.GetValue (iter, 0) as string;
                UserQueryParser parser = new UserQueryParser (adv_tree_view.Model.GetValue (iter, 1) as string);
                Condition        = parser.BuildTree (BansheeQuery.FieldSet);
                Order            = BansheeQuery.FindOrder (adv_tree_view.Model.GetValue (iter, 2) as string);
                QueryLimit limit = BansheeQuery.FindLimit (adv_tree_view.Model.GetValue (iter, 4) as string);
                Limit            = limit;
                LimitEnabled     = limit != null;

                IntegerQueryValue val = new IntegerQueryValue ();
                val.ParseUserQuery (adv_tree_view.Model.GetValue (iter, 3) as string);
                LimitValue = val;
            }
        }

        private void HandleNameChanged (object sender, EventArgs args)
        {
            Update ();
        }

        private void Update ()
        {
            if (String.IsNullOrEmpty (name_entry.Text)) {
                ok_button.Sensitive = false;
                //already_in_use_label.Markup = "";
            } else {
                ok_button.Sensitive = true;
                /*object res = Globals.Library.Db.QuerySingle(new DbCommand(
                    "SELECT Name FROM SmartPlaylists WHERE lower(Name) = lower(:name)",
                    "name", name_entry.Text
                ));

                if (res != null && (playlist == null || String.Compare (playlist.Name, name_entry.Text, true) != 0)) {
                    ok_button.Sensitive = false;
                    //already_in_use_label.Markup = "<small>" + Catalog.GetString ("This name is already in use") + "</small>";
                } else {
                    ok_button.Sensitive = true;
                    //already_in_use_label.Markup = "";
                }
                */
            }
        }

        private string PlaylistName {
            get {
                return name_entry.Text;
            }

            set {
                name_entry.Text = value;
            }
        }

        private QueryNode Condition {
            get { return builder.QueryNode; }
            set { builder.QueryNode = value; }
        }

        private QueryOrder Order {
            get { return builder.LimitBox.Order; }
            set { builder.LimitBox.Order = value; }
        }

        private IntegerQueryValue LimitValue {
            get { return builder.LimitBox.LimitValue; }
            set { builder.LimitBox.LimitValue = value; }
        }

        private QueryLimit Limit {
            get { return builder.LimitBox.Limit; }
            set { builder.LimitBox.Limit = value; }
        }

        private bool LimitEnabled {
            get { return builder.LimitBox.Enabled; }
            set { builder.LimitBox.Enabled = value; }
        }
    }
}
