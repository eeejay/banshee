/***************************************************************************
 *  DaapSource.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Mono.Unix;
using Gtk;

using DAAP;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Widgets;

namespace Banshee.Plugins.Daap
{
    public class DaapSource : ChildSource, IImportable, IImportSource
    {        
        private Service service;
        private Client client;
        private DAAP.Database database;
        private DatabaseProxy database_proxy;
        private bool is_activating;
        private VBox box;
        private Alignment container;
        private HighlightMessageArea message_area;
        
        public DaapSource(Service service) : base(service.Name, 300)
        {
            this.service = service;
            is_activating = false;
            database_proxy = new DatabaseProxy();
            
            box = new VBox();
            box.Spacing = 5;
            
            container = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            
            message_area = new HighlightMessageArea();
            message_area.BorderWidth = 5;
            message_area.LeftPadding = 15;
            message_area.Pixbuf = Icon;
            
            box.PackStart(container, true, true, 0);
            box.PackStart(message_area, false, false, 0);
            box.ShowAll();
        }
        
        public override void Activate()
        {
            InterfaceElements.DetachPlaylistContainer();
            container.Add(InterfaceElements.PlaylistContainer);
            
            if(client != null || is_activating) {
            	return;
            }
            
            is_activating = true;
            
            SetStatusMessage(String.Format(Catalog.GetString("Connecting to {0}"), Name));
            
            ThreadAssist.Spawn(delegate {
                client = new Client(service);
                client.Updated += OnClientUpdated;
                try {
                    if(client.AuthenticationMethod == AuthenticationMethod.None) {
                        client.Login();
                    } else {
                        ThreadAssist.ProxyToMain(PromptLogin);
                    }
                } catch(Exception e) {
                    ThreadAssist.ProxyToMain(delegate {
                        DaapErrorView error_view = new DaapErrorView(this, DaapErrorType.BrokenAuthentication);
                        while(box.Children.Length > 0) {
                            box.Remove(box.Children[0]);
                        }
                        box.PackStart(error_view, true, true, 0);
                        error_view.Show();
                    });
                }
               
                is_activating = false;
            });
        }
        
        private void SetStatusMessage(string message)
        {
            message_area.Message = String.Format("<big>{0}</big>", GLib.Markup.EscapeText(message));
            message_area.Visible = true;
        }
        
        private void ClearStatusMessage()
        {
            message_area.Visible = false;
        }

        private void AddPlaylistSources ()
        {
            foreach (Playlist pl in database.Playlists) {
                AddPlaylistSource (pl);
            }
        }

        private void AddPlaylistSource (DAAP.Playlist pl) 
        {
            DaapPlaylistSource source = new DaapPlaylistSource (database, pl);

            ThreadAssist.ProxyToMain (delegate {
                AddChildSource (source);
            });
        }

        private void AuthenticatedLogin(string username, string password)
        {
            ThreadAssist.Spawn(delegate {
                try {
                    client.Login(username, password);
                } catch(AuthenticationException) {
                    ThreadAssist.ProxyToMain(PromptLogin);
                }
            });
        }

        private void PromptLogin(object o, EventArgs args)
        {
            SetStatusMessage(String.Format(Catalog.GetString("Logging in to {0}"), Name));
            
            DaapLoginDialog dialog = new DaapLoginDialog(client.Name, 
            client.AuthenticationMethod == AuthenticationMethod.UserAndPassword);
            if(dialog.Run() == (int)Gtk.ResponseType.Ok) {
                AuthenticatedLogin(dialog.Username, dialog.Password);
            } else {
                Disconnect(false);
            }

            dialog.Destroy();
        }
        
        protected override void OnDispose()
        {
            Unmap();
        }

        internal bool Disconnect(bool logout)
        {
            if(client != null) {
                if(logout) {
                    client.Logout();
                }
                
                client.Dispose();
                client = null;
                database = null;
            }
            
            if(database != null) {
                database.TrackAdded -= OnDatabaseTrackAdded;
                database.TrackRemoved -= OnDatabaseTrackRemoved;
                DaapCore.ProxyServer.UnregisterDatabase(database);
                database = null;
            }

            ClearChildSources();
            
            ThreadAssist.ProxyToMain(delegate {
                DaapErrorView error_view = new DaapErrorView(this, logout 
                    ? DaapErrorType.UserDisconnect 
                    : DaapErrorType.InvalidAuthentication);
                while(box.Children.Length > 0) {
                    box.Remove(box.Children[0]);
                }
                box.PackStart(error_view, true, true, 0);
                error_view.Show();
            });
            
            return true;
        }
        
        public override bool Unmap()
        {
            return Disconnect (true);
        }
        
        public override string UnmapIcon {
            get { return Gtk.Stock.Disconnect; }
        }

        public override string UnmapLabel {
            get { return Catalog.GetString("Disconnect"); }
        }

        private void OnClientUpdated(object o, EventArgs args)
        {
            if(database == null && client.Databases.Count > 0) {
                database = client.Databases[0];
                database.TrackAdded += OnDatabaseTrackAdded;
                database.TrackRemoved += OnDatabaseTrackRemoved;
                database_proxy.Database = database;
                DaapCore.ProxyServer.RegisterDatabase(database);
                AddPlaylistSources ();
                
                ThreadAssist.ProxyToMain(delegate {
                    ClearStatusMessage();
                    while(box.Children.Length > 0) {
                        box.Remove(box.Children[0]);
                    }
                    
                    box.PackStart(container, true, true, 0);
                    box.PackStart(message_area, false, false, 0);
                });
            }
            
            Name = client.Name;
            
            ThreadAssist.ProxyToMain(delegate {
                OnUpdated();
            });
        }
        
        private void OnDatabaseTrackAdded(object o, TrackArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                OnTrackAdded(new DaapTrackInfo(args.Track, database));
            });
        }
        
        private void OnDatabaseTrackRemoved(object o, TrackArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                OnTrackRemoved(new DaapTrackInfo(args.Track, database, false));
            });
        }
        
        public override IEnumerable<TrackInfo> Tracks {
            get { return database_proxy; }
        }
        
        public override int Count {
            get { return database == null ? -1 : database.TrackCount; }
        }
        
        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, "computer", 
            "network-server", Gtk.Stock.Network);
            
        private static Gdk.Pixbuf locked_icon = IconThemeUtils.LoadIcon(22,
            "system-lock-screen", "computer", "network-server", Gtk.Stock.Network);
            
        public override Gdk.Pixbuf Icon {
            get { 
                if(service != null && service.IsProtected) {
                    return locked_icon;
                } else {
                    return icon;
                }
            }
        }
        
        public override bool? AutoExpand {
            get { return false; }
        }

        public override Gtk.Widget ViewWidget {
            get { return box; }
        }

        public void Import()
        {
            Activate ();

            // omg this is so crappy
            while (is_activating)
                System.Threading.Thread.Sleep (1);
            
            Import(Tracks);
        }
        
        private QueuedOperationManager import_manager;
        
        public void Import(IEnumerable<TrackInfo> tracks, PlaylistSource playlist)
        {
            if(playlist != null && playlist.Count == 0) {
                playlist.Rename(PlaylistUtil.GoodUniqueName(tracks));
                playlist.Commit();
            }
        
            if(import_manager == null) {
                import_manager = new QueuedOperationManager();
                import_manager.HandleActveUserEvent = false;
                import_manager.ActionMessage = Catalog.GetString("Importing");
                import_manager.UserEvent.CancelMessage = String.Format(Catalog.GetString(
                    "You are currently importing from {0}. Would you like to stop it?"), Name);
                import_manager.UserEvent.Icon = Icon;
                import_manager.UserEvent.Header = String.Format(Catalog.GetString("Copying from {0}"), Name);
                import_manager.UserEvent.Message = Catalog.GetString("Scanning...");
                import_manager.OperationRequested += OnImportOperationRequested;
                import_manager.Finished += delegate {
                    import_manager = null;
                };
            }

            foreach(TrackInfo track in tracks) {
                if(playlist == null) {
                    import_manager.Enqueue(track);
                } else {
                    import_manager.Enqueue(new KeyValuePair<TrackInfo, PlaylistSource>(track, playlist));
                }
            }
        }
        
        public void Import(IEnumerable<TrackInfo> tracks)
        {
            Import(tracks, null);
        }
        
        private void OnImportOperationRequested(object o, QueuedOperationArgs args)
        {
            DaapTrackInfo track = null;
            PlaylistSource playlist = null;
            
            if(args.Object is DaapTrackInfo) {
                track = args.Object as DaapTrackInfo;
            } else if(args.Object is KeyValuePair<TrackInfo, PlaylistSource>) {
                KeyValuePair<TrackInfo, PlaylistSource> container = 
                    (KeyValuePair<TrackInfo, PlaylistSource>)args.Object;
                track = container.Key as DaapTrackInfo;
                playlist = container.Value;
            } else {
                throw new ApplicationException ("Unknown object type: " + o.GetType ());
            }
            
            import_manager.UserEvent.Progress = import_manager.ProcessedCount / (double)import_manager.TotalCount;
            import_manager.UserEvent.Message = String.Format("{0} - {1}", track.Artist, track.Title);
            
            string from = track.Uri.LocalPath;
            string to = FileNamePattern.BuildFull(track, track.Track.Format);
            
            try {
                if(File.Exists(to)) {
                    FileInfo to_info = new FileInfo(to);
                    
                    // probably already the same file
                    if(track.Track.Size == to_info.Length) {
                        try {
                            new LibraryTrackInfo(new SafeUri(to, false), track);
                        } catch {
                            // was already in the library
                        }
                        
                        return;
                    }
                }

                long total_bytes;
                using(Stream from_stream = track.GetStream (out total_bytes)) {
                    long bytes_read = 0;
                    
                    using(FileStream to_stream = new FileStream(to, FileMode.Create, FileAccess.ReadWrite)) {
                        byte [] buffer = new byte[8192];
                        int chunk_bytes_read = 0;
                        
                        DateTime last_message_pump = DateTime.MinValue;
                        TimeSpan message_pump_delay = TimeSpan.FromMilliseconds(500);

                        while(bytes_read < total_bytes) {
                            chunk_bytes_read = from_stream.Read(buffer, 0, (int) Math.Min(total_bytes - bytes_read,
                                                                                          buffer.Length));
                            to_stream.Write(buffer, 0, chunk_bytes_read);
                            bytes_read += chunk_bytes_read;
                            
                            if(DateTime.Now - last_message_pump < message_pump_delay) {
                                continue;
                            }
                            
                            double tracks_processed = (double)import_manager.ProcessedCount;
                            double tracks_total = (double)import_manager.TotalCount;
                            
                            import_manager.UserEvent.Progress = (tracks_processed / tracks_total) +
                                ((bytes_read / (double)total_bytes) / tracks_total);
                                
                            if(import_manager.UserEvent.IsCancelRequested) {
                                throw new QueuedOperationManager.OperationCanceledException();
                            }
                            
                            last_message_pump = DateTime.Now;
                        }
                    }
                }
                
                try {
                    LibraryTrackInfo library_track = new LibraryTrackInfo(new SafeUri(to, false), track);
                    if(playlist != null) {
                        playlist.AddTrack(library_track);
                        playlist.Commit();
                    }
                } catch (Exception e) {
                    // song already in library
                }
            } catch(Exception e) {
                try {
                    File.Delete(to);
                } catch {
                }
                
                if(e is QueuedOperationManager.OperationCanceledException) {
                    return;
                }
                
                args.Abort = true;
                
                LogCore.Instance.PushError(String.Format(Catalog.GetString("Cannot import track from {0}"), 
                    Name), e.Message);
                Console.Error.WriteLine(e);
            } 
        }
    }
}
