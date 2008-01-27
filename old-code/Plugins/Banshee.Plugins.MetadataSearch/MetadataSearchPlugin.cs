/***************************************************************************
 *  MetadataSearchPlugin.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Data;
using System.Collections;
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Kernel;
using Banshee.Metadata;
using Banshee.Database;
using Banshee.Configuration;
using Banshee.Widgets;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.MetadataSearch.MetadataSearchPlugin)
        };
    }
}

namespace Banshee.Plugins.MetadataSearch 
{
    public class MetadataSearchPlugin : Banshee.Plugins.Plugin
    {
        protected override string ConfigurationName { get { return "metadata_searcher"; } }
        public override string DisplayName { get { return Catalog.GetString("Metadata Searcher"); } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Automatically search for missing and supplementary " + 
                    "metadata and cover art for songs in your library."
                    );
            }
        }

        public override string [] Authors {
            get { return new string [] { "Aaron Bockover" }; }
        }
        
        private Gdk.Pixbuf user_event_icon = IconThemeUtils.LoadIcon(22, "document-save", Gtk.Stock.Save);
        private int metadata_jobs_scheduled = 0;
        private int library_albums_count = 0;
        private ActiveUserEvent user_event;
        private ActionGroup actions;
        private uint ui_manager_id;
        
        public event EventHandler ScanStartStop;
        
        protected override void PluginInitialize()
        {
            metadata_jobs_scheduled = 0;
            library_albums_count = 0;
            
            CountScheduledJobs();
            
            Scheduler.JobStarted += OnJobStarted;
            Scheduler.JobScheduled += OnJobScheduled;
            Scheduler.JobFinished += OnJobUnscheduled;
            Scheduler.JobUnscheduled += OnJobUnscheduled;
        
            if(Globals.Library.IsLoaded) {
                OnLibraryReloaded(null, EventArgs.Empty);
            } else {
                Globals.Library.Reloaded += OnLibraryReloaded;
            }
        }
        
        protected override void PluginDispose()
        {
            Globals.ActionManager.UI.RemoveUi(ui_manager_id);
            Globals.ActionManager.UI.RemoveActionGroup(actions);
            
            actions = null;
            ui_manager_id = 0;
            
            CancelJobs();
            
            Globals.Library.Reloaded -= OnLibraryReloaded;
            Globals.Library.TrackAdded -= OnLibraryTrackAdded;
            
            Scheduler.JobStarted -= OnJobStarted;
            Scheduler.JobScheduled -= OnJobScheduled;
            Scheduler.JobFinished -= OnJobUnscheduled;
            Scheduler.JobUnscheduled -= OnJobUnscheduled;
            
            if(user_event != null) {
                user_event.Dispose();
                user_event = null;
            }
        }

        protected override void InterfaceInitialize()
        {
            actions = new ActionGroup("Metadata Search");
            
            actions.Add(new ActionEntry [] {
                new ActionEntry("GetCoverArtAction", null,
                    Catalog.GetString("Download Cover Art"), null,
                    Catalog.GetString("Download Cover Art"), RescanLibrary)
            });
            
            actions.GetAction("GetCoverArtAction").Sensitive = !IsScanning;
            
            Globals.ActionManager.UI.InsertActionGroup(actions, 0);
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("MetadataSearchMenu.xml");
        }

        private void CountScheduledJobs()
        {
            metadata_jobs_scheduled = 0;

            foreach(IJob job in Scheduler.ScheduledJobs) {
                if(job is IMetadataLookupJob) {
                    metadata_jobs_scheduled++;
                }
            }
        }

        private void OnLibraryReloaded(object o, EventArgs args)
        {
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
        }

        private void OnLibraryTrackAdded(object o, LibraryTrackAddedArgs args)
        {
            MetadataService.Instance.Lookup(args.Track, JobPriority.Lowest);
            library_albums_count++;
        }
        
        private void OnJobScheduled(IJob job)
        {
            if(job is IMetadataLookupJob) {
                bool previous = IsScanning;
            
                metadata_jobs_scheduled++;
                
                if(IsScanning != previous) {
                    OnScanStartStop();
                }
            }
        }
        
        private void OnJobStarted(IJob job)
        {
            lock(this) {
                if(job is IMetadataLookupJob) {
                    OnUpdateProgress(job as IMetadataLookupJob);
                }
            }
        }
        
        private void OnJobUnscheduled(IJob job)
        {
            if(job is IMetadataLookupJob) {
                bool previous = IsScanning;
            
                metadata_jobs_scheduled--;
                
                OnUpdateProgress(job as IMetadataLookupJob);
                
                if(IsScanning != previous) {
                    OnScanStartStop();
                }
            }
        }
        
        private void OnScanStartStop()
        {
            ThreadAssist.ProxyToMain(OnRaiseScanStartStop);
        }
        
        private void OnUpdateProgress(IMetadataLookupJob job)
        {
            lock(this) {
                try{
                    if(IsScanning && user_event == null) {
                        user_event = new ActiveUserEvent(Catalog.GetString("Download"));
                        user_event.Header = Catalog.GetString("Downloading Cover Art");
                        user_event.Message = Catalog.GetString("Searching");
                        user_event.CancelMessage = Catalog.GetString(
                            "Are you sure you want to stop downloading cover art for the albums in your library? "
                            + "The operation can be resumed at any time from the <i>Tools</i> menu.");
                        user_event.Icon = user_event_icon;
                        user_event.CancelRequested += OnUserEventCancelRequested;
                    } else if(!IsScanning && user_event != null) {
                        user_event.Dispose();
                        user_event = null;
                    } else if(user_event != null) {
                        user_event.Progress = ScanningProgress;
                        user_event.Message = String.Format("{0} - {1}", job.Track.Artist, job.Track.Album);
                        
                        if(job.Track is TrackInfo) {
                            try {
                                TrackInfo track = (TrackInfo)job.Track;
                                if(track.CoverArtFileName != null) {
                                    Gdk.Pixbuf pixbuf = new Gdk.Pixbuf(track.CoverArtFileName);
                                    if(pixbuf != null) {
                                        user_event.Icon = pixbuf.ScaleSimple(22, 22, Gdk.InterpType.Bilinear);
                                    }
                                }
                            } catch {
                            }
                        }
                    }
                } catch {
                }
            }
        }
        
        private void OnUserEventCancelRequested(object o, EventArgs args)
        {
            ThreadAssist.Spawn(CancelJobs);
        }
        
        private void OnRaiseScanStartStop(object o, EventArgs args)
        {
            actions.GetAction("GetCoverArtAction").Sensitive = !IsScanning;
        
            EventHandler handler = ScanStartStop;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        private void CancelJobs()
        {
            library_albums_count = 0;
            metadata_jobs_scheduled = 0;
            Scheduler.Unschedule(typeof(IMetadataLookupJob));
            
            ThreadAssist.ProxyToMain(delegate {
                if(actions != null) {
                    actions.GetAction("GetCoverArtAction").Sensitive = !IsScanning;
                }
            });
        }
        
        private void UpdateAlbumsCount()
        {
            lock(this) {
               try {
                    library_albums_count = Convert.ToInt32(Globals.Library.Db.QuerySingle(
                        @"SELECT COUNT(DISTINCT AlbumTitle) FROM Tracks"));
                } catch {
                }
            }
        }
        
        internal void RescanLibrary(object o, EventArgs args)
        {
            CancelJobs();
            Scheduler.Schedule(new ScanLibraryJob(this), JobPriority.BelowNormal);
        }
        
        public int AlbumsCount {
            get { return library_albums_count; }
        }
        
        public int JobsScheduledCount {
            get { return metadata_jobs_scheduled; }
        }
        
        public bool IsScanning {
            get { return metadata_jobs_scheduled > 3; }
        }
        
        public double ScanningProgress {
            get { return (AlbumsCount - JobsScheduledCount) / (double)AlbumsCount; }
        }
        
        private class ScanLibraryJob : IJob
        {
            private MetadataSearchPlugin plugin;
        
            public ScanLibraryJob(MetadataSearchPlugin plugin)
            {
                this.plugin = plugin;
            }
        
            public void Run()
            {
                plugin.UpdateAlbumsCount();

                IDataReader reader = Globals.Library.Db.Query(@"SELECT TrackID FROM Tracks GROUP BY AlbumTitle");
                
                while(reader.Read()) {
                    MetadataService.Instance.Lookup(Globals.Library.Tracks[Convert.ToInt32(reader["TrackID"])], 
                        JobPriority.Lowest);
                }
                
                reader.Dispose();
            }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.metadata_searcher", "enabled",
            false,
            "Plugin enabled",
            "Metadata searcher plugin enabled"
        );
    }
}
