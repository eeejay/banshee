//
// FileSystemQueueSource.cs
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
using Mono.Unix;
using Gtk;

using Hyena;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration;

using Banshee.Gui;

namespace Banshee.FileSystemQueue
{
    public class FileSystemQueueSource : PrimarySource, IDisposable
    {
        private DatabaseImportManager importer;
        private bool visible = false;
        private bool actions_loaded = false;
        
        protected override string TypeUniqueId {
            get { return "file-system-queue"; }
        }
        
        public FileSystemQueueSource () : base (Catalog.GetString ("File System Queue"), 
            Catalog.GetString ("File System Queue"), "file-system-queue", 9)
        {
            Properties.SetString ("Icon.Name", "system-file-manager");
            Properties.Set<bool> ("AutoAddSource", false);
            
            importer = new DatabaseImportManager (this);
            importer.KeepUserJobHidden = true;
            
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed += OnCommandLineArgument;
            
            AfterInitialized ();
            
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            uia_service.GlobalActions.AddImportant (
                new ActionEntry ("ClearFileSystemQueueAction", Stock.Clear,
                    Catalog.GetString ("Clear"), null,
                    Catalog.GetString ("Remove all tracks from the file system queue"),
                    OnClearFileSystemQueue)
            );
            
            uia_service.GlobalActions.Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ClearFileSystemQueueOnQuitAction", null,
                    Catalog.GetString ("Clear on Quit"), null, 
                    Catalog.GetString ("Clear the file system queue when quitting"), 
                    OnClearFileSystemQueueOnQuit, ClearOnQuitSchema.Get ())
            });
            
            uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");
            
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/FileSystemQueueContextMenu");
            
            actions_loaded = true;
            
            UpdateActions ();
            ServiceManager.SourceManager.ActiveSourceChanged += delegate { UpdateActions (); };

            TrackModel.Reloaded += OnTrackModelReloaded;
        }
        
        public void Enqueue (string path)
        {
            importer.QueueSource (path);
        }
        
        public override void Dispose ()
        {
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed -= OnCommandLineArgument;
            if (ClearOnQuitSchema.Get ()) {
                OnClearFileSystemQueue (this, EventArgs.Empty);
            }
            base.Dispose ();
        }
        
        private void OnCommandLineArgument (string argument, object value, bool isFile)
        {
            if (!isFile) {
                return;
            }
            
            Log.DebugFormat ("FileSystemQueueSource::Enqueue => {0}", argument);
            
            try {
                if (Banshee.IO.Directory.Exists (argument) || Banshee.IO.File.Exists (new SafeUri (argument))) {
                    Enqueue (argument);
                }
            } catch {
            }
        }
        
        protected override void OnUpdated ()
        {
            base.OnUpdated ();
            
            if (actions_loaded) {
                UpdateActions ();
            }
        }

        private void OnTrackModelReloaded (object sender, EventArgs args)
        {
            if (Count > 0 && !visible) {
                ServiceManager.SourceManager.AddSource (this);
                visible = true;
            } else if (Count <= 0 && visible) {
                ServiceManager.SourceManager.RemoveSource (this);
                visible = false;
            }
        }
        
        private void OnClearFileSystemQueue (object o, EventArgs args)
        {
            RemoveTrackRange ((DatabaseTrackListModel)TrackModel, new Hyena.Collections.RangeCollection.Range (0, Count));
            Reload ();
        }
        
        private void OnClearFileSystemQueueOnQuit (object o, EventArgs args)
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            ToggleAction action = (ToggleAction)uia_service.GlobalActions["ClearFileSystemQueueOnQuitAction"];
            ClearOnQuitSchema.Set (action.Active);
        }
        
        private void UpdateActions ()
        {   
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            uia_service.GlobalActions.UpdateAction ("ClearFileSystemQueueAction", true, Count > 0);
        }
        
        public static readonly SchemaEntry<bool> ClearOnQuitSchema = new SchemaEntry<bool> (
            "plugins.file_system_queue", "clear_on_quit",
            false,
            "Clear on Quit",
            "Clear the file system queue when quitting"
        );
    }
}
