//
// MassStorageSource.cs
//
// Author:
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
using Mono.Unix;

using Hyena;
using Hyena.Collections;

using Banshee.IO;
using Banshee.Dap;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Hardware;

using Banshee.Playlists.Formats;
using Banshee.Playlist;

namespace Banshee.Dap.MassStorage
{
    public class MassStorageSource : DapSource
    {
        private Banshee.Collection.Gui.ArtworkManager artwork_manager = ServiceManager.Get<Banshee.Collection.Gui.ArtworkManager> ();
        protected IVolume volume;

        public override void DeviceInitialize (IDevice device)
        {
            base.DeviceInitialize (device);
            
            this.volume = device as IVolume;
            if (volume == null)
                throw new InvalidDeviceException ();

            // TODO set up a ui for selecting volumes we want mounted/shown within Banshee
            // (so people don't have to touch .is_audio_player, and so we can give them a harddrive icon
            // instead of pretending they are DAPs).
            if (!HasMediaCapabilities && !HasIsAudioPlayerFile)
                throw new InvalidDeviceException ();

            if (HasIsAudioPlayerFile)
                ParseIsAudioPlayerFile ();

            // Ignore iPods, except ones with .is_audio_player files
            if (MediaCapabilities != null && MediaCapabilities.IsType ("ipod")) {
                if (HasIsAudioPlayerFile) {
                    Log.Information (
                        "Mass Storage Support Loading iPod",
                        "The USB mass storage audio player support is loading an iPod because it has an .is_audio_player file. " +
                        "If you aren't running Rockbox or don't know what you're doing, things might not behave as expected."
                    );
                } else {
                    throw new InvalidDeviceException ();
                }
            }

            Name = volume.Name;
            mount_point = volume.MountPoint;

            Initialize ();

            AddDapProperties ();

            // TODO differentiate between Audio Players and normal Disks, and include the size, eg "2GB Audio Player"?
            //GenericName = Catalog.GetString ("Audio Player");
        }

        private void AddDapProperties ()
        {
            if (AudioFolders.Length > 0 && !String.IsNullOrEmpty (AudioFolders[0])) {
                AddDapProperty (String.Format (
                    Catalog.GetPluralString ("Audio Folder", "Audio Folders", AudioFolders.Length), AudioFolders.Length),
                    System.String.Join ("\n", AudioFolders)
                );
            }

            if (FolderDepth != -1) {
                AddDapProperty (Catalog.GetString ("Required Folder Depth"), FolderDepth.ToString ());
            }

            AddYesNoDapProperty (Catalog.GetString ("Supports Playlists"), PlaylistTypes.Count > 0);

            /*if (AcceptableMimeTypes.Length > 0) {
                AddDapProperty (String.Format (
                    Catalog.GetPluralString ("Audio Format", "Audio Formats", PlaybackFormats.Length), PlaybackFormats.Length),
                    System.String.Join (", ", PlaybackFormats)
                );
            }*/
        }

        private DatabaseImportManager importer;
        // WARNING: This will be called from a thread!
        protected override void LoadFromDevice ()
        {
            importer = new DatabaseImportManager (this);
            importer.KeepUserJobHidden = true;
            importer.Threaded = false; // We are already threaded
            importer.Finished += OnImportFinished;

            foreach (string audio_folder in BaseDirectories) {
                importer.Enqueue (audio_folder);
            }
        }

        private void OnImportFinished (object o, EventArgs args)
        {
            importer.Finished -= OnImportFinished;

            if (!CanSyncPlaylists) {
                return;
            }

            Hyena.Data.Sqlite.HyenaSqliteCommand insert_cmd = new Hyena.Data.Sqlite.HyenaSqliteCommand (
                "INSERT INTO CorePlaylistEntries (PlaylistID, TrackID) VALUES (?, ?)");
            int [] psources = new int [] {DbId};
            foreach (string playlist_path in PlaylistFiles) {
                IPlaylistFormat loaded_playlist = PlaylistFileUtil.Load (playlist_path, new Uri (BaseDirectory));
                if (loaded_playlist == null)
                    continue;

                PlaylistSource playlist = new PlaylistSource (System.IO.Path.GetFileNameWithoutExtension (playlist_path), this);
                playlist.Save ();
                //Hyena.Data.Sqlite.HyenaSqliteCommand.LogAll = true;
                foreach (Dictionary<string, object> element in loaded_playlist.Elements) {
                    string track_path = (element["uri"] as Uri).LocalPath;
                    int track_id = DatabaseTrackInfo.GetTrackIdForUri (track_path, Paths.MakePathRelative (track_path, BaseDirectory), psources);
                    if (track_id == 0) {
                        Log.DebugFormat ("Failed to find track {0} in DAP library to load it into playlist {1}", track_path, playlist_path);
                    } else {
                        ServiceManager.DbConnection.Execute (insert_cmd, playlist.DbId, track_id);
                    }
                }
                //Hyena.Data.Sqlite.HyenaSqliteCommand.LogAll = false;
                playlist.UpdateCounts ();
                AddChildSource (playlist);
            }
        }

        public override void CopyTrackTo (DatabaseTrackInfo track, SafeUri uri, BatchUserJob job)
        {
            if (track.PrimarySourceId == DbId) {
                Banshee.IO.File.Copy (track.Uri, uri, false);
            }
        }

        public override void Import ()
        {
            LibraryImportManager importer = new LibraryImportManager (true);
            foreach (string audio_folder in BaseDirectories) {
                importer.Enqueue (audio_folder);
            }
        }

        public IVolume Volume {
            get { return volume; }
        }

        private string mount_point;
        public override string BaseDirectory {
            get { return mount_point; }
        }

        private bool? has_is_audio_player_file = null;
        private bool HasIsAudioPlayerFile {
            get {
                if (has_is_audio_player_file == null)
                    has_is_audio_player_file = File.Exists (new SafeUri (IsAudioPlayerPath));
                return has_is_audio_player_file.Value;
            }
        }

        protected override IDeviceMediaCapabilities MediaCapabilities {
            get {
                return (volume.Parent == null)
                    ? base.MediaCapabilities
                    : volume.Parent.MediaCapabilities ?? base.MediaCapabilities;
            }
        }

        protected string IsAudioPlayerPath {
            get { return System.IO.Path.Combine (volume.MountPoint, ".is_audio_player"); }
        }

        #region Properties and Methods for Supporting Syncing of Playlists

        private string playlists_path;
        private string PlaylistsPath {
            get {
                if (playlists_path == null) {
                    if (MediaCapabilities == null || MediaCapabilities.PlaylistPath == null) {
                        playlists_path = WritePath;
                    } else {
                        playlists_path = System.IO.Path.Combine (WritePath, MediaCapabilities.PlaylistPath);
                        playlists_path = playlists_path.Replace ("%File", String.Empty);
                    }

                    if (!Directory.Exists (playlists_path)) {
                        Directory.Create (playlists_path);
                    }
                }
                return playlists_path;
            }
        }

        private string [] playlist_formats;
        private string [] PlaylistFormats {
            get {
                if (playlist_formats == null && MediaCapabilities != null) {
                    playlist_formats = MediaCapabilities.PlaylistFormats;
                }
                return playlist_formats;
            }
            set { playlist_formats = value; }
        }

        private List<PlaylistFormatDescription> playlist_types;
        private IList<PlaylistFormatDescription> PlaylistTypes  {
            get {
                if (playlist_types == null) {
                    playlist_types = new List<PlaylistFormatDescription> ();
                    if (PlaylistFormats != null) {
                        foreach (PlaylistFormatDescription desc in Banshee.Playlist.PlaylistFileUtil.ExportFormats) {
                            foreach (string mimetype in desc.MimeTypes) {
                                if (Array.IndexOf (PlaylistFormats, mimetype) != -1) {
                                    playlist_types.Add (desc);
                                    break;
                                }
                            }
                        }
                    }
                }
                
                SupportsPlaylists &= CanSyncPlaylists;
                return playlist_types;
            }
        }

        private IEnumerable<string> PlaylistFiles {
            get {
                foreach (string file_name in Directory.GetFiles (PlaylistsPath)) {
                    foreach (PlaylistFormatDescription desc in playlist_types) {
                        if (file_name.EndsWith (desc.FileExtension)) {
                            yield return file_name;
                            break;
                        }
                    }
                }
            }
        }

        private bool CanSyncPlaylists {
            get {
                return PlaylistsPath != null && playlist_types.Count > 0;
            }
        }

        public override void SyncPlaylists ()
        {
            if (!CanSyncPlaylists) {
                return;
            }

            foreach (string file_name in PlaylistFiles) {
                Banshee.IO.File.Delete (new SafeUri (file_name));
            }

            // Add playlists from Banshee to the device
            PlaylistFormatBase playlist_format = null;
            List<Source> children = new List<Source> (Children);
            foreach (Source child in children) {
                PlaylistSource from = child as PlaylistSource;
                if (from != null) {
                    from.Reload ();
                    if (playlist_format == null) {
                         playlist_format = Activator.CreateInstance (PlaylistTypes[0].Type) as PlaylistFormatBase;
                    }

                    SafeUri playlist_path = new SafeUri (System.IO.Path.Combine (
                        PlaylistsPath, String.Format ("{0}.{1}", from.Name, PlaylistTypes[0].FileExtension)));

                    System.IO.Stream stream = Banshee.IO.File.OpenWrite (playlist_path, true);
                    playlist_format.BaseUri = new Uri (BaseDirectory);
                    playlist_format.Save (stream, from);
                    stream.Close ();
                }
            }
        }

        #endregion
        
        public override long BytesUsed {
            get { return BytesCapacity - volume.Available; }
        }
        
        public override long BytesCapacity {
            get { return (long) volume.Capacity; }
        }

        private bool had_write_error = false;
        public override bool IsReadOnly {
            get { return volume.IsReadOnly || had_write_error; }
        }

        private string write_path = null;
        protected string WritePath {
            get {
                if (write_path == null) {
                    write_path = BaseDirectory;
                    // According to the HAL spec, the first folder listed in the audio_folders property
                    // is the folder to write files to.
                    if (AudioFolders.Length > 0) {
                        write_path = Banshee.Base.Paths.Combine (write_path, AudioFolders[0]);
                    }
                }
                return write_path;
            }

            set { write_path = value; }
        }

        private string [] audio_folders;
        protected string [] AudioFolders {
            get {
                if (audio_folders == null) {
                    audio_folders = HasMediaCapabilities ? MediaCapabilities.AudioFolders : new string [] {};
                }
                return audio_folders;
            }
            set { audio_folders = value; }
        }

        protected IEnumerable<string> BaseDirectories {
            get {
                if (AudioFolders.Length == 0) {
                    yield return BaseDirectory;
                } else {
                    foreach (string audio_folder in AudioFolders) {
                        yield return Paths.Combine (BaseDirectory, audio_folder);
                    }
                }
            }
        }

        private int folder_depth = -1;
        protected int FolderDepth {
            get {
                if (folder_depth == -1) {
                    folder_depth = HasMediaCapabilities ? MediaCapabilities.FolderDepth : 0;
                }
                return folder_depth;
            }
            set { folder_depth = value; }
        }

        private int cover_art_size = -1;
        protected int CoverArtSize {
            get {
                if (cover_art_size == -1) {
                    cover_art_size = HasMediaCapabilities ? MediaCapabilities.CoverArtSize : 0;
                }
                return cover_art_size;
            }
            set { cover_art_size = value; }
        }

        private string cover_art_file_name = null;
        protected string CoverArtFileName {
            get {
                if (cover_art_file_name == null) {
                    cover_art_file_name = HasMediaCapabilities ? MediaCapabilities.CoverArtFileName : null;
                }
                return cover_art_file_name;
            }
            set { cover_art_file_name = value; }
        }

        private string cover_art_file_type = null;
        protected string CoverArtFileType {
            get {
                if (cover_art_file_type == null) {
                    cover_art_file_type = HasMediaCapabilities ? MediaCapabilities.CoverArtFileType : null;
            }
                return cover_art_file_type;
            }
            set { cover_art_file_type = value; }
        }

        protected override void AddTrackToDevice (DatabaseTrackInfo track, SafeUri fromUri)
        {
            if (track.PrimarySourceId == DbId)
                return;

            SafeUri new_uri = new SafeUri (GetTrackPath (track, System.IO.Path.GetExtension (fromUri.LocalPath)));
            // If it already is on the device but it's out of date, remove it
            //if (File.Exists(new_uri) && File.GetLastWriteTime(track.Uri.LocalPath) > File.GetLastWriteTime(new_uri))
                //RemoveTrack(new MassStorageTrackInfo(new SafeUri(new_uri)));

            if (!File.Exists (new_uri)) {
                Directory.Create (System.IO.Path.GetDirectoryName (new_uri.LocalPath));
                File.Copy (fromUri, new_uri, false);

                DatabaseTrackInfo copied_track = new DatabaseTrackInfo (track);
                copied_track.PrimarySource = this;
                copied_track.Uri = new_uri;
                copied_track.Save (false);
            }

            if (CoverArtSize > -1 && !String.IsNullOrEmpty (CoverArtFileType) && 
                    !String.IsNullOrEmpty (CoverArtFileName) && FolderDepth > 0) {
                SafeUri cover_uri = new SafeUri (System.IO.Path.Combine (System.IO.Path.GetDirectoryName (new_uri.LocalPath),
                                                                         CoverArtFileName));
                string coverart_id;
                if (track.HasAttribute (TrackMediaAttributes.Podcast)) {
                    coverart_id = String.Format ("podcast-{0}", Banshee.Base.CoverArtSpec.EscapePart (track.AlbumTitle));
                } else {
                    coverart_id = track.ArtworkId;
                }                
                
                if (!File.Exists (cover_uri) && CoverArtSpec.CoverExists (coverart_id)) {       
                    Gdk.Pixbuf pic = null;
                    
                    if (CoverArtSize == 0) {
                        if (CoverArtFileType == "jpg" || CoverArtFileType == "jpeg") {                        
                            SafeUri local_cover_uri = new SafeUri (Banshee.Base.CoverArtSpec.GetPath (coverart_id));
                            Banshee.IO.File.Copy (local_cover_uri, cover_uri, false);
                        } else {
                            pic = artwork_manager.Lookup (coverart_id);
                        }
                    } else {
                        pic = artwork_manager.LookupScale (coverart_id, CoverArtSize);
                    }

                    if (pic != null) {
                        try {                        
                            byte [] bytes = pic.SaveToBuffer (CoverArtFileType);
                            System.IO.Stream cover_art_file = File.OpenWrite (cover_uri, true);
                            cover_art_file.Write (bytes, 0, bytes.Length);
                            cover_art_file.Close ();
                        } catch (GLib.GException){
                            Log.DebugFormat ("Could convert cover art to {0}, unsupported filetype?", CoverArtFileType);
                        } finally {
                            pic.Dispose ();
                        }
                    }
                }
            }
        }

        protected override void DeleteTrack (DatabaseTrackInfo track)
        {
            try {
                Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (track.Uri);
            } catch (System.IO.FileNotFoundException) {
            } catch (System.IO.DirectoryNotFoundException) {
            }
        }

        protected override void Eject ()
        {
            base.Eject ();
            if (volume.CanUnmount) {
                volume.Unmount ();
            }

            if (volume.CanEject) {
                volume.Eject ();
            }
        }

        private string GetTrackPath (TrackInfo track, string ext)
        {
            string file_path = WritePath;

            /*string artist = FileNamePattern.Escape (track.ArtistName);
            string album = FileNamePattern.Escape (track.AlbumTitle);
            string number_title = FileNamePattern.Escape (track.TrackNumberTitle);

            // If the folder_depth property exists, we have to put the files in a hiearchy of
            // the exact given depth (not including the mount point/audio_folder).
            if (FolderDepth != -1) {
                int depth = FolderDepth;

                if (depth == 0) {
                    // Artist - Album - 01 - Title
                    file_path = System.IO.Path.Combine (file_path, String.Format ("{0} - {1} - {2}", artist, album, number_title));
                } else if (depth == 1) {
                    // Artist - Album/01 - Title
                    file_path = System.IO.Path.Combine (file_path, String.Format ("{0} - {1}", artist, album));
                    file_path = System.IO.Path.Combine (file_path, number_title);
                } else if (depth == 2) {
                    // Artist/Album/01 - Title
                    file_path = System.IO.Path.Combine (file_path, artist);
                    file_path = System.IO.Path.Combine (file_path, album);
                    file_path = System.IO.Path.Combine (file_path, number_title);
                } else {
                    // If the *required* depth is more than 2..go nuts!
                    for (int i = 0; i < depth - 2; i++) {
                        file_path = System.IO.Path.Combine (file_path, artist.Substring (0, Math.Min (i, artist.Length)).Trim ());
                    }

                    // Finally add on the Artist/Album/01 - Track
                    file_path = System.IO.Path.Combine (file_path, artist);
                    file_path = System.IO.Path.Combine (file_path, album);
                    file_path = System.IO.Path.Combine (file_path, number_title);
                }
            } else {
                file_path = System.IO.Path.Combine (file_path, FileNamePattern.CreateFromTrackInfo (track));
            }
            */

            file_path = System.IO.Path.Combine (file_path, FileNamePattern.CreateFromTrackInfo (track));
            file_path += ext;

            return file_path;
        }

        private void ParseIsAudioPlayerFile ()
        {
            // Allow the HAL values to be overridden by corresponding key=value pairs in .is_audio_player
            System.IO.StreamReader reader = null;
            try {
                reader = new System.IO.StreamReader (IsAudioPlayerPath);

                string line;
                while ((line = reader.ReadLine ()) != null) {
                    string [] pieces = line.Split ('=');
                    if (line.StartsWith ("#") || pieces == null || pieces.Length != 2)
                        continue;

                    string key = pieces[0];
                    string val = pieces[1];

                    switch (key) {
                    case "audio_folders":
                        AudioFolders = val.Split (',');
                        break;

                    case "output_formats":
                        AcceptableMimeTypes = val.Split (',');
                        for (int i = 0; i < AcceptableMimeTypes.Length; i++) {
                            AcceptableMimeTypes[i] = AcceptableMimeTypes[i].Trim ();
                        }
                        break;

                    case "folder_depth":
                        FolderDepth = Int32.Parse (val);
                        break;
    
                    case "playlist_format":
                        PlaylistFormats = val.Split (',');
                        for (int i = 0; i < PlaylistFormats.Length; i++) {
                            PlaylistFormats[i] = PlaylistFormats[i].Trim ();
                        }
                        break;

                    case "playlist_path":
                        playlists_path = System.IO.Path.Combine (WritePath, val);
                        playlists_path = playlists_path.Replace ("%File", String.Empty);
                        break;

                    case "cover_art_file_type":
                        CoverArtFileType = val.ToLower ();
                        break;

                    case "cover_art_file_name":
                        CoverArtFileName = val;
                        break;

                    case "cover_art_size":
                        CoverArtSize = Int32.Parse (val);
                        break;

                    case "input_formats":
                    default:
                        Log.DebugFormat ("Unsupported .is_audio_player key: {0}", key);
                        break;
                    }
                }
            } catch (Exception e) {
                Log.Exception ("Error parsing .is_audio_player file", e);
            } finally {
                if (reader != null)
                    reader.Close ();
            }
        }
    }
}
