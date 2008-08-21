//
// DapLibrarySync.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections.Generic;

using Hyena;
using Hyena.Query;

using Banshee.Configuration;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Playlist;
using Banshee.SmartPlaylist;
using Banshee.Query;

namespace Banshee.Dap
{ 
    public sealed class DapLibrarySync
    {
        private DapSync sync;
        private LibrarySource library;
        private string conf_ns;
        private SchemaEntry<bool> enabled, sync_entire_library;
        private SchemaEntry<string[]> playlist_ids;
        private SmartPlaylistSource sync_src, to_add;
        
        #region Public Properties

        public bool Enabled {
            get { return sync.Enabled && enabled.Get (); }
        }
        
        public bool SyncEntireLibrary {
            get { return sync_entire_library.Get (); }
        }
        
        #endregion
        
        private string [] SyncPlaylistIds {
            get { return playlist_ids.Get (); }
        }
        
        List<AbstractPlaylistSource> sync_playlists;
        private IList<AbstractPlaylistSource> SyncPlaylists {
            get {
                if (sync_playlists == null) {
                    sync_playlists = new List<AbstractPlaylistSource> ();
                    foreach (string id in SyncPlaylistIds) {
                        foreach (Source src in library.Children) {
                            if (src.UniqueId == id) {
                                sync_playlists.Add (src as AbstractPlaylistSource);
                                break;
                            }
                        }
                    }
                }
                return sync_playlists;
            }
        }

        internal string SmartPlaylistId {
            get { return sync_src.DbId.ToString (); }
        }
        
        internal DapLibrarySync (DapSync sync, LibrarySource library)
        {
            this.sync = sync;
            this.library = library;
            conf_ns = String.Format ("{0}.{1}", sync.ConfigurationNamespace, library.ConfigurationId);
            
            enabled = sync.Dap.CreateSchema<bool> (conf_ns, "enabled", true,
                "Whether sync is enabled for this device and library source.", "");
            
            sync_entire_library = sync.Dap.CreateSchema<bool> (conf_ns, "sync_entire_library", true,
                "Whether to sync the entire library and all playlists.", "");
            
            playlist_ids = sync.Dap.CreateSchema<string[]> (conf_ns, "playlist_ids", new string [0],
                "If sync_entire_library is false, this contains a list of playlist ids specifically to sync", "");

            // This smart playlist is the list of items we want on the device - nothing more, nothing less
            sync_src = new SmartPlaylistSource ("sync_list", library.DbId);
            sync_src.IsTemporary = true;
            sync_src.Save ();

            // This is the same as the previous list with the items that are already on the device removed
            to_add = new SmartPlaylistSource ("to_add", library.DbId);
            to_add.IsTemporary = true;
            to_add.Save ();
            to_add.ConditionTree = UserQueryParser.Parse (String.Format ("smartplaylistid:{0}", sync_src.DbId),
                Banshee.Query.BansheeQuery.FieldSet);
            to_add.DatabaseTrackModel.AddCondition (String.Format (
                "MetadataHash NOT IN (SELECT MetadataHash FROM CoreTracks WHERE PrimarySourceId = {0})", sync.Dap.DbId
            ));
        }
        
        internal void CalculateSync ()
        {
            if (SyncEntireLibrary) {
                to_add.ConditionTree = null;
            } else if (SyncPlaylistIds.Length > 0) {
                QueryListNode playlists_node = new QueryListNode (Keyword.Or);
                foreach (AbstractPlaylistSource src in SyncPlaylists) {
                    if (src is PlaylistSource) {
                        playlists_node.AddChild (UserQueryParser.Parse (String.Format ("playlistid:{0}", src.DbId), BansheeQuery.FieldSet));
                    } else if (src is SmartPlaylistSource) {
                        playlists_node.AddChild (UserQueryParser.Parse (String.Format ("smartplaylistid:{0}", src.DbId), BansheeQuery.FieldSet));
                    }
                }
                sync_src.ConditionTree = playlists_node;
            }
            sync_src.RefreshAndReload ();
            to_add.RefreshAndReload ();
        }

        public override string ToString ()
        {
            return String.Format ("Sync calculated for {1}: to add: {0} items", to_add.Count, library.Name);
        }
        
        internal void Sync ()
        {
            CalculateSync ();
            
            DoSyncPlaylists ();
        }
        
        private void DoSyncPlaylists ()
        {
            // Remove all playlists
            foreach (Source child in sync.Dap.Children) {
                if (child is AbstractPlaylistSource && !(child is MediaGroupSource)) {
                    (child as IUnmapableSource).Unmap ();
                }
            }
            
            if (!SyncEntireLibrary && SyncPlaylistIds.Length == 0) {
                return;
            }

            foreach (AbstractPlaylistSource src in SyncPlaylists) {
                SyncPlaylist (src);
            }
        }
        
        private void SyncPlaylist (AbstractPlaylistSource from)
        {
            //PlaylistSource to = new PlaylistSource (from.Name, sync.Dap.DbId);
            //to.Save ();
            
            // copy playlist/track entries based on metadatahash..
        }
    }
}
