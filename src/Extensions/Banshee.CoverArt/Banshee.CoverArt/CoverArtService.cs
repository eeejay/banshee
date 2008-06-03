//
// CoverArtService.cs
//
// Authors:
//   James Willcox <snorp@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using Gtk;
using Mono.Unix;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;
using Banshee.Library;
using Banshee.Metadata;
using Banshee.Networking;
using Banshee.Sources;
using Hyena;

namespace Banshee.CoverArt
{
    public class CoverArtService : IExtensionService
    {
        private InterfaceActionService action_service;
		private ActionGroup actions;
        private bool disposed;
        private uint ui_manager_id;
        
        private CoverArtJob job;
        
        public CoverArtService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            if (!ServiceManager.DbConnection.TableExists ("CoverArtDownloads")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE CoverArtDownloads (
                        AlbumID     INTEGER UNIQUE,
                        Downloaded  BOOLEAN,
                        LastAttempt INTEGER NOT NULL
                    )");
            }

            action_service = ServiceManager.Get<InterfaceActionService> ();
            
            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }
        }
        
        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }
        
        private bool ServiceStartup ()
        {
            if (action_service == null || ServiceManager.SourceManager.MusicLibrary == null) {
                return false;
            }
            
            Initialize ();
            
            return true;
        }
        
        private void Initialize ()
        {            
            actions = new ActionGroup ("CoverArt");
            
            ActionEntry[] action_list = new ActionEntry [] {
                new ActionEntry ("CoverArtAction", null,
                    Catalog.GetString ("_Cover Art"), null,
                    Catalog.GetString ("Manage cover art"), null),
				new ActionEntry ("FetchCoverArtAction", null,
                    Catalog.GetString ("_Download Cover Art"), null,
                    Catalog.GetString ("Download cover art for all tracks"), OnFetchCoverArt)
            };
            
            actions.Add (action_list);
            
            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("CoverArtMenu.xml");
            
            ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksChanged += OnTracksChanged;
        }
        
        public void Dispose ()
        {
            if (disposed) {
                return;
            }
            
            Gtk.Action fetch_action = action_service.GlobalActions["FetchCoverArtAction"];
            if (fetch_action != null) {
                action_service.GlobalActions.Remove (fetch_action);
            }
            
            action_service.RemoveActionGroup ("CoverArt");
            action_service.UIManager.RemoveUi (ui_manager_id);
            
            actions = null;
            action_service = null;
            
            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksChanged -= OnTracksChanged;
            
            disposed = true;
        }
        
        public void FetchCoverArt ()
        {
            bool force = false;
            if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("BANSHEE_FORCE_COVER_ART_FETCH"))) {
                Log.Debug ("Forcing cover art download session");
                force = true;                
            }
            
            FetchCoverArt (force);
        }
        
        public void FetchCoverArt (bool force)
        {
            if (job == null && NetworkDetect.Instance.Connected) {
                DateTime last_scan = DateTime.MinValue;
                
                if (!force) {
                    last_scan = DatabaseConfigurationClient.Client.Get<DateTime> ("last_cover_art_scan",
                                                                                  DateTime.MinValue);
                }
                Log.DebugFormat ("Last cover art scan was '{0}'", last_scan);
                job = new CoverArtJob (last_scan);
                job.Finished += delegate {
                    DatabaseConfigurationClient.Client.Set<DateTime> ("last_cover_art_scan",
                                                                      DateTime.Now);
                    job = null;
                };
                job.Start ();
            }
        }
        
        private void OnFetchCoverArt (object o, EventArgs args)
        {
            FetchCoverArt ();
        }
        
        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            FetchCoverArt ();
        }
        
        private void OnTracksChanged (Source sender, TrackEventArgs args)
        {
            FetchCoverArt ();
        }
    
        string IService.ServiceName {
            get { return "CoverArtService"; }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.cover_art", "enabled",
            true,
            "Plugin enabled",
            "Cover art plugin enabled"
        );
    }
}
