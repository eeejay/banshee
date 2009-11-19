//
// EmusicImport.cs
//
// Author:
//   Eitan Isaacson <eitan@monotonous.org>
//
// Copyright (C) 2009 Eitan Isaacson
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
using System.IO;
using System.Xml;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Playlist;
using Banshee.Emusic;

using Migo.DownloadCore;
using Migo.TaskCore;

namespace Banshee.Emusic
{
    public sealed class EmusicImport : IImportSource
    {

        private DownloadManager download_manager;
        private DownloadManagerInterface download_manager_iface;
        private LibraryImportManager import_manager;
        private Dictionary<string,HttpFileDownloadTask> tasks;
        private readonly string tmp_download_path = Paths.Combine (Paths.ExtensionCacheRoot, "emusic", "partial-downloads");
        

        public EmusicImport ()
        {
            tasks = new Dictionary<string, HttpFileDownloadTask> ();
            
            import_manager = ServiceManager.Get<LibraryImportManager> ();
            import_manager.ImportResult += HandleImportResult;
        }

        public void Import ()
        {
            
            var chooser = Banshee.Gui.Dialogs.FileChooserDialog.CreateForImport (Catalog.GetString ("Import eMusic Downloads to Library"), true);
            Gtk.FileFilter ff = new Gtk.FileFilter();
            ff.Name = Catalog.GetString ("eMusic Files");
            ff.AddPattern("*.emx");
            chooser.AddFilter (ff);

            if (chooser.Run () == (int)Gtk.ResponseType.Ok)
                DoImport (chooser.Uris);
            
            chooser.Destroy ();
        }

        private void DoImport (string[] uris)
        {
            download_manager = new DownloadManager (2, tmp_download_path);
            download_manager_iface = new DownloadManagerInterface (download_manager);
            download_manager_iface.Initialize ();
            
            foreach (string uri in uris)
            {
                using (var xml_reader = new XmlTextReader (uri))
                {
                    Console.WriteLine ("File: {0}", uri);
                    
                    while (xml_reader.Read())
                    {
                        if (xml_reader.NodeType == XmlNodeType.Element &&
                            xml_reader.Name == "TRACKURL")
                        {
                            xml_reader.Read();
                            Console.WriteLine("URL: {0}", xml_reader.Value);
                            HttpFileDownloadTask task = download_manager.CreateDownloadTask (xml_reader.Value);
                            if (File.Exists (task.LocalPath))
                                File.Delete (task.LocalPath); // FIXME: We go into a download loop if we don't.
                            task.Completed += OnDownloadCompleted;
                            download_manager.QueueDownload (task);
                            tasks.Add (task.LocalPath, task);
                        }
                    }
                }
            }
        }

        private void OnDownloadCompleted (object sender, TaskCompletedEventArgs args)
        {
            HttpFileDownloadTask task = sender as HttpFileDownloadTask;
            Console.WriteLine ("RESULT: {0}", task.Status.ToString());
            
            if (task.Status != TaskStatus.Succeeded)
            {
                task.Completed -= OnDownloadCompleted;

                if (File.Exists (task.LocalPath))
                    File.Delete (task.LocalPath);

                if (Directory.Exists (Path.GetDirectoryName (task.LocalPath)))
                    Directory.Delete (Path.GetDirectoryName (task.LocalPath));
                
                tasks.Remove (task.LocalPath);
                
              
            } else {
                import_manager.Enqueue (task.LocalPath);
            }
        }

        void HandleImportResult(object o, DatabaseImportResultArgs args)
        {
            if (tasks.ContainsKey (args.Path))
            {
                HttpFileDownloadTask task = tasks[args.Path];
                task.Completed -= OnDownloadCompleted;
                File.Delete (args.Path);
                Directory.Delete (Path.GetDirectoryName (args.Path));
                tasks.Remove (args.Path);
            }

            if (download_manager.Tasks.Count == 0)
                PostImport ();
        }

        private void PostImport ()
        {
            download_manager.Dispose ();
            download_manager_iface.Dispose ();
            download_manager = null;
        }
   
        public bool CanImport {
            get { return true;}
        }

        public string Name {
            get { return Catalog.GetString ("eMusic Album"); }
        }

        public string ImportLabel {
            get { return Catalog.GetString ("C_hoose Files"); }
        }

        public string [] IconNames {
            get { return new string [] { "gtk-open" }; }
        }

        public int SortOrder {
            get { return 50; }
        }
    }
}
