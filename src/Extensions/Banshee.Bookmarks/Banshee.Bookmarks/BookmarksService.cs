using System;
using System.Data;
using System.Collections.Generic;
using Gtk;
using Mono.Unix;

using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Gui;
using Banshee.ServiceStack;
using Hyena;

namespace Banshee.Bookmarks
{
    public class BookmarksService : IExtensionService, IDisposable
    {
        private BookmarkUI ui;
        
        public BookmarksService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            Bookmark.Initialize();
            ui = BookmarkUI.Instance;
        }
        
        public void Dispose ()
        {
            if (ui != null)
                ui.Dispose();
        }

        string IService.ServiceName {
            get { return "BookmarksService"; }
        }
    }

    public class BookmarkUI
    {
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
        
        private InterfaceActionService action_service;
        private ActionGroup actions;
        private uint ui_manager_id;
        
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
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            
            actions = new ActionGroup("Bookmarks");

            actions.Add(new ActionEntry [] {
                new ActionEntry("BookmarksAction", null,
                                  Catalog.GetString("_Bookmarks"), null,
                                  null, null),
                new ActionEntry("BookmarksAddAction", Stock.Add,
                                  Catalog.GetString("_Add Bookmark"), "<control>D",
                                  Catalog.GetString("Bookmark the Position in the Current Track"),
                                  HandleNewBookmark)
            });

            action_service.UIManager.InsertActionGroup(actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource("BookmarksMenu.xml");
            bookmark_item = action_service.UIManager.GetWidget("/MainMenu/ToolsMenu/Bookmarks") as ImageMenuItem;
            new_item = action_service.UIManager.GetWidget("/MainMenu/ToolsMenu/Bookmarks/Add") as ImageMenuItem;

            bookmark_menu = bookmark_item.Submenu as Menu;
            bookmark_item.Selected += HandleMenuShown;

            remove_item = new ImageMenuItem(Catalog.GetString("_Remove Bookmark"));
            remove_item.Sensitive = false;
            remove_item.Image = new Image(Stock.Remove, IconSize.Menu);

            remove_item.Submenu = remove_menu = new Menu();
            bookmark_menu.Append(remove_item);

            LoadBookmarks ();
        }

        private void HandleMenuShown(object sender, EventArgs args)
        {
            new_item.Sensitive = (ServiceManager.PlayerEngine.CurrentTrack != null);
        }

        private void HandleNewBookmark(object sender, EventArgs args)
        {
            DatabaseTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (track != null) {
                try {
                    Bookmark bookmark = new Bookmark(track.TrackId, ServiceManager.PlayerEngine.Position);
                    AddBookmark(bookmark);
                } catch (Exception e) {
                    Log.Warning("Unable to Add New Bookmark", e.ToString(), false);
                }
            }
        }

        private void LoadBookmarks ()
        {
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
                Console.WriteLine ("item delegate, main thread? {0}", Banshee.Base.ThreadAssist.InMainThread);
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
            action_service.UIManager.RemoveUi(ui_manager_id);
            action_service.UIManager.RemoveActionGroup(actions);
            actions = null;

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
            get { return DatabaseTrackInfo.Provider.FetchSingle(track_id); }
        }

        private Bookmark(int id, int track_id, uint position, DateTime created_at)
        {
            this.id = id;
            this.track_id = track_id;
            this.position = position;
            this.created_at = created_at;
            uint position_seconds = position/1000;
            Name = String.Format(bookmark_format, Track.DisplayTrackTitle, position_seconds/60, position_seconds%60);
        }

        public Bookmark(int track_id, uint position)
        {
            Console.WriteLine ("Bookmark, main thread? {0}", Banshee.Base.ThreadAssist.InMainThread);
            this.track_id = track_id;
            this.position = position;
            this.created_at = DateTime.Now;
            uint position_seconds = position/1000;
            Name = String.Format(bookmark_format, Track.DisplayTrackTitle, position_seconds/60, position_seconds%60);

            this.id = ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                    INSERT INTO Bookmarks
                    (TrackID, Position, CreatedAt)
                    VALUES (?, ?, ?)",
                    track_id, position, DateTimeUtil.FromDateTime(created_at) ));
        }

        public void JumpTo()
        {
            DatabaseTrackInfo track = Track as DatabaseTrackInfo;
            DatabaseTrackInfo current_track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (track != null) {
                if (current_track == null || current_track.TrackId != track.TrackId) {
                    ServiceManager.PlayerEngine.Open (track); 
                }

                if (ServiceManager.PlayerEngine.CanSeek) {
                    ServiceManager.PlayerEngine.Position = position;
                }
                
                if (ServiceManager.PlayerEngine.CurrentState != Banshee.MediaEngine.PlayerEngineState.Playing) {
                    ServiceManager.PlayerEngine.Play ();
                }
            } else {
                Remove();
            }
        }

        public void Remove()
        {
            try {
                ServiceManager.DbConnection.Execute(String.Format(
                    "DELETE FROM Bookmarks WHERE BookmarkID = {0}", id
                ));

                if (BookmarkUI.Instantiated)
                    BookmarkUI.Instance.RemoveBookmark(this);
            } catch (Exception e) {
                Log.Warning("Error Removing Bookmark", e.ToString(), false);
            }
        }

        public static List<Bookmark> LoadAll()
        {
            List<Bookmark> bookmarks = new List<Bookmark>();

            IDataReader reader = ServiceManager.DbConnection.Query(
                "SELECT BookmarkID, TrackID, Position, CreatedAt FROM Bookmarks"
            );

            while (reader.Read()) {
                try {
                    bookmarks.Add(new Bookmark(
                        (int) reader[0], (int) reader[1], (uint)(int) reader[2],
                        DateTimeUtil.ToDateTime(Convert.ToInt64(reader[3]))
                    ));
                } catch (Exception e) {
                    ServiceManager.DbConnection.Execute(String.Format(
                        "DELETE FROM Bookmarks WHERE BookmarkID = {0}", (int)reader[0]
                    ));

                    Log.Warning("Error Loading Bookmark", e.ToString(), false);
                }
            }
            reader.Dispose();

            return bookmarks;
        }

        public static void Initialize()
        {
            if (!ServiceManager.DbConnection.TableExists("Bookmarks")) {
                ServiceManager.DbConnection.Execute(@"
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
