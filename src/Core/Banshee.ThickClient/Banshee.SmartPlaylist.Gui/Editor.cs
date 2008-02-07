using System;
using System.Collections;
using Gtk;
using Glade;

using Mono.Unix;

using Hyena.Query;
using Hyena.Query.Gui;
 
using Banshee.Base;
using Banshee.Query;
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
        //private TracksQueryModel model;

        private SmartPlaylistSource playlist = null;

        [Widget] private Gtk.Entry name_entry;
        [Widget] private Gtk.VBox builder_box;
        [Widget] private Gtk.Button ok_button;
        [Widget] private Gtk.TreeView adv_tree_view;
        [Widget] private Gtk.Button adv_use_button;
        [Widget] private Gtk.Button adv_add_button;

        public Editor (SmartPlaylistSource playlist) : base("SmartPlaylistEditorDialog")
        {
            this.playlist = playlist;

            Initialize();

            Dialog.Title = Catalog.GetString ("Edit Smart Playlist");

            name_entry.Text = playlist.Name;
            //Condition = playlist.Condition;
            //OrderBy = playlist.OrderBy;
            //LimitNumber = playlist.LimitNumber;
            //LimitCriterion = playlist.LimitCriterion;
        }
    
        public Editor () : base("SmartPlaylistEditorDialog")
        {
            Initialize();
        }

        private void Initialize()
        {
            Dialog.Title = Catalog.GetString ("New Smart Playlist");

            // Add the QueryBuilder widget
            //model = new TracksQueryModel(this.playlist);
            builder = new BansheeQueryBox ();
            builder.Show();
            builder.Spacing = 4;

            builder_box.PackStart(builder, true, true, 0);

            name_entry.Changed += HandleNameChanged;

            // Model is Name, Condition, OrderBy, LimitNumber, LimitCriterion
            ListStore list_model = new ListStore (typeof(string), typeof(string), typeof(string), 
                typeof(string), typeof(int));

            list_model.AppendValues (
                Catalog.GetString ("Neglected Favorites"),
                " (Rating > 3) AND ((strftime(\"%s\", current_timestamp) - LastPlayedStamp + 3600) > 2592000) ",
                null, "0", 0);

            // TODO this one is broken, not supported by the condition GUI
            /*list_model.AppendValues (
                Catalog.GetString ("Unrated"),
                " (Rating = NULL) ",
                null, "0", 0);*/

            list_model.AppendValues (
                Catalog.GetString ("700 MB of Favorites"),
                " (Rating > 3) ",
                "NumberOfPlays DESC",
                "700",
                3);

            list_model.AppendValues (
                Catalog.GetString ("80 Minutes of Favorites"),
                " (Rating > 3) ",
                "NumberOfPlays DESC",
                "80",
                1);

            list_model.AppendValues (
                Catalog.GetString ("Unheard"),
                " (NumberOfPlays = 0) ",
                null,
                "0",
                0);

            list_model.AppendValues (
                Catalog.GetString ("Unheard Podcasts"),
                " (NumberOfPlays = 0) AND (lower(Uri) LIKE '%podcast%') ",
                null,
                "0",
                0);

            adv_tree_view.Selection.Mode = SelectionMode.Multiple;
            adv_tree_view.Model = list_model;
            adv_tree_view.AppendColumn ("title", new CellRendererText (), "text", 0);
            adv_tree_view.Selection.Changed += HandleAdvSelectionChanged;

            UpdateAdvButtons (0);

            adv_add_button.Clicked += HandleAdvAdd;
            adv_use_button.Clicked += HandleAdvUse;

            Gdk.Geometry limits = new Gdk.Geometry();
            limits.MinWidth = Dialog.SizeRequest().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            Dialog.SetGeometryHints(Dialog, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);

            Update();
            
            name_entry.GrabFocus();
        }

        /*public void SetQueryFromSearch()
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
                int.Parse(query);
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

        public void RunDialog()
        {
            Run();
            Dialog.Destroy();
        }

        public override ResponseType Run()
        {
            Dialog.ShowAll();

            ResponseType response = (ResponseType)Dialog.Run ();

            //int w = -1, h = -1;
            //dialog.GetSize (out w, out h);
            //Console.WriteLine ("w = {0}, h = {1}", w, h);

            QueryNode node = builder.BuildQuery ();
            if (node == null) {
                Console.WriteLine ("Editor query is null");
            } else {
                Console.WriteLine ("Editor query is: {0}", node.ToXml (BansheeQuery.FieldSet, true));
            }

            if (response == ResponseType.Ok) {
                string name = PlaylistName;
                //string condition = Condition;
                //string order_by = OrderBy;
                //string limit_number = LimitNumber;
                //int limit_criterion = LimitCriterion;

                ThreadAssist.Spawn (delegate {
                    //Console.WriteLine ("Name = {0}, Cond = {1}, OrderAndLimit = {2}", name, condition, order_by, limit_number);
                    if (playlist == null) {
                    /*
                        Timer t = new Timer ("Create/Add new Playlist");
                        //playlist = new SmartPlaylistSource(name, condition, order_by, limit_number, limit_criterion);
                        //Banshee.Sources.LibrarySource.Instance.AddChildSource(playlist);
                        SmartPlaylistCore.Instance.StartTimer(playlist);

                        // Add this source to the source manager, otherwise it will be ignored until we restart
                        SourceManager.AddSource (playlist, false);
                        t.Stop();
                        */
                    } else {
                        /*playlist.Rename(name);
                        playlist.Condition = condition;
                        playlist.OrderBy = order_by;
                        playlist.LimitNumber = limit_number;
                        playlist.LimitCriterion = limit_criterion;
                        playlist.Commit();

                        playlist.QueueRefresh();

                        if (playlist.TimeDependent)
                            SmartPlaylistCore.Instance.StartTimer(playlist);
                        else
                            SmartPlaylistCore.Instance.StopTimer();

                        playlist.ListenToPlaylists();
                        SmartPlaylistCore.Instance.SortPlaylists();*/
                    }
                });
            }

            return response;
        }

        private void HandleAdvSelectionChanged (object sender, EventArgs args)
        {
            TreeSelection selection = sender as TreeSelection;
            UpdateAdvButtons (selection.CountSelectedRows());
        }

        private void UpdateAdvButtons (int num)
        {
            adv_use_button.Sensitive = (num == 1);
            adv_add_button.Sensitive = (num > 0);
        }

        private void HandleAdvAdd (object sender, EventArgs args)
        {
            TreePath [] paths = adv_tree_view.Selection.GetSelectedRows ();

            /*foreach (TreePath path in paths) {
                TreeIter iter;
                if (adv_tree_view.Model.GetIter (out iter, path)) {
                    string name            = adv_tree_view.Model.GetValue (iter, 0) as string;
                    string condition       = adv_tree_view.Model.GetValue (iter, 1) as string;
                    string orderBy         = adv_tree_view.Model.GetValue (iter, 2) as string;
                    string limitNumber     = adv_tree_view.Model.GetValue (iter, 3) as string;
                    int limitCriterion  = (int) adv_tree_view.Model.GetValue (iter, 4);

                    SmartPlaylistSource pl = new SmartPlaylistSource(name, condition, orderBy, limitNumber, limitCriterion);
                    Banshee.Sources.LibrarySource.Instance.AddChildSource (pl);
                    SmartPlaylistCore.Instance.StartTimer (pl);
                }
            }*/

            Dialog.Destroy();
        }

        private void HandleAdvUse (object sender, EventArgs args)
        {
            TreePath [] paths = adv_tree_view.Selection.GetSelectedRows ();

            if (paths != null && paths.Length != 1)
                return;

            TreeIter iter;
            /*
            if (adv_tree_view.Model.GetIter (out iter, paths[0])) {
                PlaylistName    = adv_tree_view.Model.GetValue (iter, 0) as string;
                Condition       = adv_tree_view.Model.GetValue (iter, 1) as string;
                OrderBy         = adv_tree_view.Model.GetValue (iter, 2) as string;
                LimitNumber     = adv_tree_view.Model.GetValue (iter, 3) as string;
                LimitCriterion  = (int) adv_tree_view.Model.GetValue (iter, 4);
            }*/
        }

        private void HandleNameChanged(object sender, EventArgs args)
        {
            Update ();
        }

        private void Update()
        {
            if (name_entry.Text == "") {
                ok_button.Sensitive = false;
                //already_in_use_label.Markup = "";
            } else {
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

        /*private string Condition {
            get {
                return builder.MatchesEnabled
                    ? builder.MatchQuery
                    : null;
            }

            set {
                builder.MatchesEnabled = (value != null);
                builder.MatchQuery = value;
            }
        }

        private string OrderBy {
            get {
                return (builder.Limit && builder.LimitNumber != "0")
                    ? builder.OrderBy
                    : null;
            }

            set {
                builder.Limit = (value != null);
                builder.OrderBy = value;
            }
        }

        private string LimitNumber {
            get {
                return (builder.Limit)
                    ? builder.LimitNumber
                    : "0";
            }
            
            set {
                if (value != null && value != "" && value != "0") {
                    builder.Limit = true;
                    builder.LimitNumber = value;
                }
            }
        }

        private string LimitCriterion {
            get {
                return (string) builder.LimitCriterion;
            }
            
            set {
                builder.LimitCriterion = Convert.ToInt32 (value);
            }
        }*/
    }
}
