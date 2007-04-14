using System;
using System.Data;
using System.Collections.Generic;
using Gtk;

using Mono.Gettext;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Configuration;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.Bookmarks.BookmarksPlugin)
        };
    }
}

namespace Banshee.Plugins.Bookmarks
{
    public class BookmarksPlugin : Banshee.Plugins.Plugin
    {
        private BookmarkUI ui;
        
        protected override string ConfigurationName { 
            get { return "bookmarks"; } 
        }
        
        public override string DisplayName { 
            get { return Catalog.GetString("Bookmarks"); }
        }
        
        public override string Description {
            get { return Catalog.GetString( "Bookmark your position in tracks."); }
        }
        
        public override string [] Authors {
            get { return new string [] { "Gabriel Burt" }; }
        }
 
        protected override void PluginInitialize()
        {
            Bookmark.Initialize();
        }
        
        protected override void InterfaceInitialize()
        {
            ui = BookmarkUI.Instance;
        }

        protected override void PluginDispose()
        {
            if (ui != null)
                ui.Dispose();
        }

        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.bookmarks", "enabled", false, "Plugin enabled", "Bookmarks plugin enabled"
        );
    }

    public class BookmarkUI
    {
        private Menu parent_menu;
        private Menu bookmark_menu;
        private Menu remove_menu;

        private ImageMenuItem bookmark_item;
        private ImageMenuItem new_item;
        private ImageMenuItem remove_item;
        private SeparatorMenuItem separator;

        private List<Bookmark> bookmarks = new List<Bookmark>();
        private Dictionary<Bookmark, MenuItem> select_items = new Dictionary<Bookmark, MenuItem>();
        private Dictionary<Bookmark, MenuItem> remove_items = new Dictionary<Bookmark, MenuItem>();
        private Dictionary<MenuItem, Bookmark> bookmark_map = new Dictionary<MenuItem, Bookmark>();

        private static BookmarkUI instance = null;
        public static BookmarkUI Instance {
            get {
                if (instance == null)
                    instance = new BookmarkUI();
                return instance;
            }
        }

        public static bool Instantiated {
            get { return instance != null; }
        }

        private BookmarkUI()
        {
            parent_menu = (Globals.ActionManager.GetWidget("/MainMenu/ToolsMenu") as MenuItem).Submenu as Menu;

            bookmark_item = new ImageMenuItem(Catalog.GetString("_Bookmarks"));
            bookmark_item.Image = new Image ("bookmark", IconSize.Menu);
            bookmark_item.AccelPath = "<control>D";
            bookmark_item.Submenu = bookmark_menu = new Menu();

            new_item = new ImageMenuItem(Catalog.GetString("_New Bookmark"));
            new_item.Image = new Image(Stock.Add, IconSize.Menu);
            new_item.Activated += HandleNewBookmark;
            bookmark_item.Selected += HandleMenuShown;

            bookmark_menu.Append(new_item);

            remove_item = new ImageMenuItem(Catalog.GetString("_Remove Bookmark"));
            remove_item.Sensitive = false;
            remove_item.Image = new Image(Stock.Remove, IconSize.Menu);

            remove_item.Submenu = remove_menu = new Menu();
            bookmark_menu.Append(remove_item);
            parent_menu.Append(bookmark_item);

            // Wait until the library is loaded to load existing bookmarks
            if (Globals.Library.IsLoaded) {
                HandleLibraryReloaded (null, null);
            } else {
                Globals.Library.Reloaded += HandleLibraryReloaded;
            }
        }

        private void HandleMenuShown(object sender, EventArgs args)
        {
            new_item.Sensitive = (PlayerEngineCore.CurrentTrack != null);
        }

        private void HandleNewBookmark(object sender, EventArgs args)
        {
            TrackInfo track = PlayerEngineCore.CurrentTrack;
            if (track != null) {
                AddBookmark(new Bookmark(track.TrackId, PlayerEngineCore.Position));
            }
        }

        private void HandleLibraryReloaded (object sender, EventArgs args)
        {
            Globals.Library.Reloaded -= HandleLibraryReloaded;

            separator = new SeparatorMenuItem();

            foreach (Bookmark bookmark in Bookmark.LoadAll()) {
                AddBookmark(bookmark);
            }

            bookmark_item.ShowAll();
        }
        
        public void AddBookmark(Bookmark bookmark)
        {
            if (select_items.ContainsKey(bookmark))
                return;

            bookmarks.Add(bookmark);
            if (bookmarks.Count == 1) {
                bookmark_menu.Append(separator);
                remove_item.Sensitive = true;
            }

            // Add menu item to jump to this bookmark
            ImageMenuItem select_item = new ImageMenuItem(bookmark.Name.Replace("_", "__"));
            select_item.Image = new Image(Stock.JumpTo, IconSize.Menu);
            select_item.Activated += delegate {
                bookmark.JumpTo();
            };
            bookmark_menu.Append(select_item);
            select_items[bookmark] = select_item;

            // Add menu item to remove this bookmark
            ImageMenuItem rem = new ImageMenuItem(bookmark.Name.Replace("_", "__"));
            rem.Image = new Image(Stock.Remove, IconSize.Menu);
            rem.Activated += delegate {
                bookmark.Remove();
            };
            remove_menu.Append(rem);
            remove_items[bookmark] = rem;
            bookmark_map[rem] = bookmark;

            bookmark_menu.ShowAll();
        }

        public void RemoveBookmark(Bookmark bookmark)
        {
            if (!remove_items.ContainsKey(bookmark))
                return;

            bookmark_menu.Remove(select_items[bookmark]);
            remove_menu.Remove(remove_items[bookmark]);
            bookmarks.Remove(bookmark);
            select_items.Remove(bookmark);
            bookmark_map.Remove(remove_items[bookmark]);
            remove_items.Remove(bookmark);

            if (bookmarks.Count == 0) {
                bookmark_menu.Remove(separator);
                remove_item.Sensitive = false;
           }
        }

        public void Dispose()
        {
            if (parent_menu != null && bookmark_item != null) {
                parent_menu.Remove(bookmark_item);
            }
            instance = null;
        }
    }

    public class Bookmark
    {
        private int id;
        private int track_id;
        private uint position;
        private DateTime created_at;

        // Translators: This is used to generate bookmark names. {0} is track title, {1} is minutes
        // (possibly more than two digits) and {2} is seconds (between 00 and 60).
        private readonly string bookmark_format = Catalog.GetString("{0} ({1}:{2:00})");

        private string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        public DateTime CreatedAt {
            get { return created_at; }
        }

        public TrackInfo Track {
            get { return Globals.Library.Tracks.ContainsKey(track_id) ? Globals.Library.GetTrack(track_id) : null; }
        }

        private Bookmark(int id, int track_id, uint position, DateTime created_at)
        {
            this.id = id;
            this.track_id = track_id;
            this.position = position;
            this.created_at = created_at;
            Name = String.Format(bookmark_format, Track.DisplayTitle, position/60, position%60);
        }

        public Bookmark(int track_id, uint position)
        {
            this.track_id = track_id;
            this.position = position;
            this.created_at = DateTime.Now;
            Name = String.Format(bookmark_format, Track.DisplayTitle, position/60, position%60);

            DbCommand command = new DbCommand(@"
                INSERT INTO Bookmarks
                    (TrackID, Position, CreatedAt)
                    VALUES (:track_id, :position, :created_at)",
                "track_id", track_id,
                "position", position, 
                "created_at", DateTimeUtil.FromDateTime(created_at)
            );

            this.id = Globals.Library.Db.Execute(command);
        }

        public void JumpTo()
        {
            TrackInfo track = Track;
            if (track != null) {
                if (PlayerEngineCore.CurrentTrack != track) {
                    PlayerEngineCore.OpenPlay(track); 
                }

                if (PlayerEngineCore.CanSeek) {
                    PlayerEngineCore.Position = position;
                }
            } else {
                Remove();
            }
        }

        public void Remove()
        {
            try {
                Globals.Library.Db.Execute(String.Format(
                    "DELETE FROM Bookmarks WHERE BookmarkID = {0}", id
                ));

                if (BookmarkUI.Instantiated)
                    BookmarkUI.Instance.RemoveBookmark(this);
            } catch {}
        }

        public static List<Bookmark> LoadAll()
        {
            List<Bookmark> bookmarks = new List<Bookmark>();

            IDataReader reader = Globals.Library.Db.Query(
                "SELECT BookmarkID, TrackID, Position, CreatedAt FROM Bookmarks"
            );

            while (reader.Read()) {
                try {
                    bookmarks.Add(new Bookmark(
                        (int) reader[0], (int) reader[1], (uint)(int) reader[2],
                        DateTimeUtil.ToDateTime(Convert.ToInt64(reader[3]))
                    ));
                } catch {
                    Globals.Library.Db.Execute(String.Format(
                        "DELETE FROM Bookmarks WHERE BookmarkID = {0}", (int)reader[1]
                    ));
                }
            }
            reader.Dispose();

            return bookmarks;
        }

        public static void Initialize()
        {
            if (!Globals.Library.Db.TableExists("Bookmarks")) {
                Globals.Library.Db.Execute(@"
                    CREATE TABLE Bookmarks (
                        BookmarkID          INTEGER PRIMARY KEY,
                        TrackID             INTEGER NOT NULL,
                        Position            INTEGER NOT NULL,
                        CreatedAt           INTEGER NOT NULL
                    )
                ");
            }
        }
    }
}
