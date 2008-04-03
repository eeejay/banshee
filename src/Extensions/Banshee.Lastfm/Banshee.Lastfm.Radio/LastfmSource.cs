//
// LastfmSource.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;

using Lastfm;
using Hyena.Data;
using SortType = Hyena.Data.SortType;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.Sources;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Networking;

using Banshee.Sources.Gui;

using Browser = Lastfm.Browser;

namespace Banshee.Lastfm.Radio
{   
    public class LastfmSource : Source, IDisposable
    {
        private const string lastfm = "Last.fm";

        private RadioConnection connection;
        public RadioConnection Connection {
            get { return connection; }
        }

        private Account account;
        public Account Account {
            get { return account; }
        }

        private LastfmActions actions;
        public LastfmActions Actions {
            get { return actions; }
        }
        
        protected override string TypeUniqueId {
            get { return lastfm; }
        }

        public LastfmSource () : base (lastfm, lastfm, 150)
        {
            account = LastfmCore.Account;

            // We don't automatically connect to Last.fm, but load the last Last.fm
            // username we used so we can load the user's stations.
            if (account.UserName != null) {
                account.UserName = LastUserSchema.Get ();
                account.CryptedPassword = LastPassSchema.Get ();
            }

            if (LastfmCore.UserAgent == null) {
                LastfmCore.UserAgent = Banshee.Web.Browser.UserAgent;
            }
            
            Browser.Open = Banshee.Web.Browser.Open;
            
            connection = LastfmCore.Radio;
            connection.UpdateNetworkState (NetworkDetect.Instance.Connected);
            NetworkDetect.Instance.StateChanged += delegate (object o, NetworkStateChangedArgs args) {
                connection.UpdateNetworkState (args.Connected);
            };

            Initialize ();

            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/LastfmSourcePopup");
            Properties.SetString ("Icon.Name", "lastfm-audioscrobbler");
            Properties.SetString ("SourcePropertiesActionLabel", Catalog.GetString ("Edit Last.fm Settings"));

            // FIXME this is temporary until we split the GUI part from the non-GUI part
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new LastfmSourceContents ());
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);

            actions = new LastfmActions (this);

            ServiceManager.SourceManager.AddSource (this);
        }

        public void Initialize ()
        {
            Connection.StateChanged += HandleConnectionStateChanged;
            
            /*if (Account.UserName != null && Account.CryptedPassword != null) {
                Connection.Connect ();
            }*/
            
            UpdateUI ();
        }

        public void Dispose ()
        {
            Connection.StateChanged -= HandleConnectionStateChanged;
            //ClearChildSources ();
        }

        /*public override void ClearChildSources ()
        {
            lock (Children) {
                foreach (StationSource child in Children) {
                    //if (SourceManager.ContainsSource (child))
                    //    SourceManager.RemoveSource (child);
                    child.Dispose ();
                }
            }

            base.ClearChildSources ();
        }*/

        /*public override void AddChildSource (ChildSource source)
        {
            base.AddChildSource (source);
            SortChildSources ();
            source.Updated += HandleChildUpdated;
        }

        public override void RemoveChildSource (ChildSource source)
        {
            base.RemoveChildSource (source);
            source.Updated -= HandleChildUpdated;
        }*/


        // Order by the playCount of a station, then by inverted name
        public class PlayCountComparer : IComparer<Source>
        {
            public int Compare (Source sa, Source sb)
            {
                StationSource a = sa as StationSource;
                StationSource b = sb as StationSource;
                int c = a.PlayCount.CompareTo (b.PlayCount);
                return c == 0 ? -(a.Name.CompareTo (b.Name)) : c; 
            }
        }

        // Order by the type of station, then by the station name
        public class TypeComparer : IComparer<Source>
        {
            public int Compare (Source sa, Source sb)
            {
                StationSource a = sa as StationSource;
                StationSource b = sb as StationSource;
                int c = a.Type.Name.CompareTo (b.Type.Name);
                return c == 0 ? (a.Name.CompareTo (b.Name)) : c; 
            }
        }

        public static IComparer<Source> [] ChildComparers = new IComparer<Source> [] {
            new NameComparer (), new PlayCountComparer (), new TypeComparer ()
        };
        public static SortType [] child_orders = new SortType [] {
            SortType.Ascending, SortType.Descending, SortType.Ascending
        };

        public IComparer<Source> ChildComparer {
            get {
                if (child_comparer == null) {
                    int i = (int) StationSortSchema.Get ();
                    ChildComparer = ChildComparers [i];
                }
                return child_comparer;
            }
            set {
                child_comparer = value;
                int i = Array.IndexOf (ChildComparers, child_comparer);
                child_order = child_orders[i];
                StationSortSchema.Set (i);
            }
        }

        private bool sorting = false;
        private IComparer<Source> child_comparer;
        private SortType child_order;
        public override void SortChildSources (IComparer<Source> comparer, bool asc)
        {
            lock (this) {
                if (sorting)
                    return;
                sorting = true;
            }

            base.SortChildSources (comparer, asc);
            ChildComparer = comparer;
            sorting = false;
        }

        public void SortChildSources  ()
        {
            SortChildSources (ChildComparer, child_order == SortType.Ascending);
        }

        private string last_username;
        public void SetUserName (string username)
        {
            if (username != last_username) {
                last_username = username;
                LastfmSource.LastUserSchema.Set (last_username);
                ClearChildSources ();
                sorting = true;
                foreach (StationSource child in StationSource.LoadAll (this, Account.UserName)) {
                    if (Connection.Subscriber ||
                            (!child.Type.SubscribersOnly &&
                                !(child.Type == StationType.Personal && child.Arg != null && child.Arg.Trim().ToLower() == last_username.Trim().ToLower())))
                    {
                        AddChildSource (child);
                    }
                }
                sorting = false;
                SortChildSources ();
            }
        }

        public override void Activate ()
        {
            //InterfaceElements.ActionButtonBox.PackStart (add_button, false, false, 0);
            if (Connection.State == ConnectionState.Disconnected) {
                Connection.Connect ();
            }
        }

        public override bool? AutoExpand {
            get { return ExpandedSchema.Get (); }
        }

        public override bool Expanded {
            get { return ExpandedSchema.Get (); }
            set { ExpandedSchema.Set (value); }
        }

        public override bool CanActivate {
            get { return true; }
        }

        public override bool HasProperties {
            get { return true; }
        }

        private void HandleConnectionStateChanged (object sender, ConnectionStateChangedArgs args)
        {
            UpdateUI ();
        }

        /*private void HandleChildUpdated (object sender, EventArgs args)
        {
            SortChildSources ();
        }*/

        private void UpdateUI ()
        {
            bool have_user = Account.UserName != null;
            bool have_pass = Account.CryptedPassword != null;
            
            if (have_pass) {
                LastPassSchema.Set (Account.CryptedPassword);
            }
            
            if (have_user) {
                SetUserName (Account.UserName);
            } else {
                ClearChildSources ();
            }

            Name = (Connection.State == ConnectionState.Connected ) ? lastfm : Catalog.GetString ("Last.fm (Disconnected)");

            if (Connection.Connected) {
                HideStatus ();
            } else {
                SetStatus (RadioConnection.MessageFor (Connection.State), Connection.State != ConnectionState.Connecting, Connection.State);
            }

            OnUpdated ();
        }

        protected override void SetStatus (string message, bool error)
        {
            base.SetStatus (message, error);
            SetStatus (status_message, this, error, ConnectionState.Connected);
        }

        private void SetStatus (string message, bool error, ConnectionState state)
        {
            base.SetStatus (message, error);
            SetStatus (status_message, this, error, state);
        }

        internal static void SetStatus (SourceMessage status_message, LastfmSource lastfm, bool error, ConnectionState state)
        {
            status_message.FreezeNotify ();
            if (error && (state == ConnectionState.NoAccount || state == ConnectionState.InvalidAccount)) {
                status_message.AddAction (new MessageAction (Catalog.GetString ("Account Settings"),
                    delegate { lastfm.Actions.ShowLoginDialog (); }));
                status_message.AddAction (new MessageAction (Catalog.GetString ("Join Last.fm"),
                    delegate { lastfm.Account.SignUp (); }));
            }
            status_message.ThawNotify ();
        }

        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.lastfm", "enabled", false, "Extension enabled", "Last.fm extension enabled"
        );

        public static readonly SchemaEntry<int> StationSortSchema = new SchemaEntry<int> (
            "plugins.lastfm", "station_sort", 0, "Station sort criteria", "Last.fm station sort criteria. 0 = name, 1 = play count, 2 = type"
        );

        public static readonly SchemaEntry<string> LastUserSchema = new SchemaEntry<string> (
            "plugins.lastfm", "username", "", "Last.fm user", "Last.fm username"
        );

        public static readonly SchemaEntry<string> LastPassSchema = new SchemaEntry<string> (
            "plugins.lastfm", "password_hash", "", "Last.fm password", "Last.fm password (hashed)"
        );

        public static readonly SchemaEntry<bool> ExpandedSchema = new SchemaEntry<bool> (
            "plugins.lastfm", "expanded", false, "Last.fm expanded", "Last.fm expanded"
        );
    }
}
