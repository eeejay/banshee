//
// PerformanceTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Data;
using System.Threading;
using NUnit.Framework;

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Query;
using Hyena.CommandLine;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Query;
using Banshee.ServiceStack;

namespace Banshee.Tests
{
    [TestFixture]
    public class PerformanceTests
    {
        const int NUM = 50;
        private string select_single_command;

#region Reload tests

        private void ReloadMusicLibrary (string filter)
        {
            ReloadMusicLibrary (filter, 1.0);
        }

        private void ReloadMusicLibrary (string filter, double runFactor)
        {
            music_library.FilterQuery = filter;
            for (int i = 0; i < NUM*runFactor; i++) {
                music_library.Reload ();
            }
        }

        [Test]
        [Category ("Performance")]
        public void Reload ()
        {
            ReloadMusicLibrary ("", .25);
        }

        [Test]
        [Category ("Performance")]
        public void ReloadD ()
        {
            ReloadMusicLibrary ("d", .25);
        }

        [Test]
        [Category ("Performance")]
        public void ReloadDave ()
        {
            ReloadMusicLibrary ("dave");
        }

        [Test]
        [Category ("Performance")]
        public void ReloadByDave ()
        {
            ReloadMusicLibrary ("by:dave");
        }

        [Test]
        [Category ("Performance")]
        public void ReloadFavorites ()
        {
            ReloadMusicLibrary ("rating>3");
        }

        [Test]
        [Category ("Performance")]
        public void ReloadGenreRock ()
        {
            ReloadMusicLibrary ("genre:rock");
        }

#endregion

#region Access tests

        [Test]
        [Category ("Performance")]
        public void ScrollLinear ()
        {
            music_library.FilterQuery = "";
            int count = Math.Min (2500, music_library.FilteredCount);
            bool any_null = false;

            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < count; j++) {
                    any_null |= music_library.TrackModel [j] == null;
                }
                music_library.DatabaseTrackModel.InvalidateCache (false);
            }

            Assert.IsFalse (any_null);
        }

        [Test]
        [Category ("Performance")]
        public void FetchTrack ()
        {
            var track = music_library.TrackModel[0] as DatabaseTrackInfo;
            int track_id = track.TrackId;
            var cmd = new HyenaSqliteCommand (select_single_command, track_id);
            var db = ServiceManager.DbConnection;

            for (int i = 0; i < NUM * 30; i++) {
                using (IDataReader reader = db.Query (cmd)) {
                    if (reader.Read ()) {
                    }
                }
            }
        }

        [Test]
        [Category ("Too")]
        public void LoadTrack ()
        {
            var track = music_library.TrackModel[0] as DatabaseTrackInfo;
            var provider = DatabaseTrackInfo.Provider;
            int track_id = track.TrackId;
            var cmd = new HyenaSqliteCommand (select_single_command, track_id);
            var db = ServiceManager.DbConnection;
            IDataReader reader = db.Query (cmd);
            Assert.IsTrue (reader.Read ());

            for (int i = 0; i < NUM*1000; i++) {
                provider.Load (reader, track);
            }

            reader.Dispose ();
        }

        private void Select<T> (T val)
        {
            T res = default(T);
            var cmd = new HyenaSqliteCommand ("SELECT ?", val);
            Log.DebugFormat ("Select<T> cmd is {0}", cmd.Text);
            var db = ServiceManager.DbConnection;

            for (int i = 0; i < NUM * 250; i++) {
                res = db.Query<T> (cmd);
            }
            Assert.AreEqual (val, res);
        }

        [Test]
        [Category ("Performance")]
        public void SelectStrings ()
        {
            Select ("Foo bar");
        }

        [Test]
        [Category ("Performance")]
        public void SelectInt ()
        {
            Select ((int)42);
        }

        [Test]
        [Category ("Performance")]
        public void SelectLong ()
        {
            Select ((long)42);
        }

        [Test]
        [Category ("Performance")]
        public void SelectDateTime ()
        {
            Select (new DateTime (1999, 2, 3));
        }

        [Test]
        [Category ("Performance")]
        public void SelectBool ()
        {
            Select (true);
        }

#endregion

#region Sort tests

        private void SortMusicLibrary (QueryField field)
        {
            SortableColumn column = new SortableColumn (field);
            music_library.FilterQuery = "";

            for (int i = 0; i < NUM/2; i++) {
                music_library.DatabaseTrackModel.Sort (column);
                music_library.Reload ();
            }
        }

        [Test]
        [Category ("Performance")]
        public void SortTrack ()
        {
            SortMusicLibrary (BansheeQuery.TrackNumberField);
        }

        [Test]
        [Category ("Performance")]
        public void SortArtist ()
        {
            SortMusicLibrary (BansheeQuery.ArtistField);
        }

        [Test]
        [Category ("Performance")]
        public void SortRating ()
        {
            SortMusicLibrary (BansheeQuery.RatingField);
        }

        [Test]
        [Category ("Performance")]
        public void SortLastPlayed ()
        {
            SortMusicLibrary (BansheeQuery.LastPlayedField);
        }

#endregion

#region Helpers

        private Thread main_thread;
        private HeadlessClient client;
        private Banshee.Library.MusicLibrarySource music_library;

        [TestFixtureSetUp]
        public void Setup ()
        {
            ApplicationContext.CommandLine = new CommandLineParser (new string [] { 
                "--uninstalled",
                Environment.GetEnvironmentVariable ("BANSHEE_DEV_OPTIONS") }, 0);
            Log.Debugging = false;

            Application.TimeoutHandler = RunTimeout;
            Application.IdleHandler = RunIdle;
            Application.IdleTimeoutRemoveHandler = IdleTimeoutRemove;
            Application.Initialize ();

            client = new HeadlessClient ();
            main_thread = new Thread (RunBanshee);
            main_thread.Start ();
            while (!client.IsStarted) {}
        }

        private void RunBanshee ()
        {
            Gtk.Application.Init ();
            ThreadAssist.InitializeMainThread ();
            Application.PushClient (client);
            Application.Run ();

            music_library = ServiceManager.SourceManager.MusicLibrary;

            var provider = DatabaseTrackInfo.Provider;
            select_single_command = String.Format (
                    "SELECT {0} FROM {1} WHERE {2}{3}{4} = ?",
                    provider.Select, provider.From, provider.Where,
                    (String.IsNullOrEmpty (provider.Where) ? String.Empty : " AND "),
                    provider.PrimaryKey
            );

            client.Start ();
        }

        [TestFixtureTearDown]
        [Category ("Performance")]
        public void Teardown ()
        {
            ThreadAssist.ProxyToMain (Application.Shutdown);
            main_thread.Join ();
            main_thread = null;
        }

        protected uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            return GLib.Timeout.Add (milliseconds, delegate { return handler (); });
        }

        protected uint RunIdle (IdleHandler handler)
        {
            return GLib.Idle.Add (delegate { return handler (); });
        }

        protected bool IdleTimeoutRemove (uint id)
        {
            return GLib.Source.Remove (id);
        }

#endregion

        public static void Main (string [] args)
        {
            var tests = new PerformanceTests ();
            tests.Setup ();

            using (new Hyena.Timer ("Performance.exe Tests")) {
                tests.ScrollLinear ();
            }

            tests.Teardown ();
        }

    }

    public class SortableColumn : ISortableColumn
    {
        QueryField field;
        SortType sort_type;

        public SortableColumn (QueryField field)
        {
            this.field = field;
        }

        public string SortKey { get { return field.Name; } }

        public SortType SortType {
            get { return sort_type; }
            set { sort_type = value; }
        }

        public Hyena.Query.QueryField Field { get { return field; } }

        public string Id { get { return field.Name; } }
    }

    public class HeadlessClient : Client
    {
        public override string ClientId {
            get { return "Headless"; }
        }

        public HeadlessClient ()
        {
        }

        public void Start ()
        {
            OnStarted ();
        }
    }
}
