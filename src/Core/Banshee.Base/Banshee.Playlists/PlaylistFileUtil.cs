using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Configuration;
using Banshee.Sources;
using Banshee.Playlists.Formats;
using Banshee.Widgets;

namespace Banshee.Playlists
{
    public class InvalidPlaylistException : ApplicationException
    {
        public InvalidPlaylistException(string message) : base(message)
        {
        }
    }
    
    public class PlaylistImportCanceledException : ApplicationException
    {
        public PlaylistImportCanceledException(string message) : base(message)
        {
        }
        
        public PlaylistImportCanceledException() : base()
        {
        }
    }
    
    public class PlaylistFileUtil
    {        
        public static readonly SchemaEntry<string> DefaultExportFormat = new SchemaEntry<string>(
            "player_window", "default_export_format",
            String.Empty,
            "Export Format",
            "The default playlist export format"
        );
        
        private PlaylistFileUtil()
        {
        }
        
        public static bool IsSourceExportSupported(Source source)
        {
            bool supported = true;
            
            if(source == null || !(source is IPlaylist)) {
                supported = false;
            }
            
            return supported;
        }
        
        public static PlaylistFile GetDefaultExportFormat()
        {
            PlaylistFile default_format = null;
            try {
                string exportFormat = DefaultExportFormat.Get();
                PlaylistFile[] formats = PlaylistFileUtil.GetAllExportFormats();
                foreach (PlaylistFile format in formats) {
                    if (format.Extension.Equals(exportFormat)) {
                        default_format = format;
                        break;
                    }
                }
            } catch {            
                // Ignore errors, return our default if we encounter an error.                
            } finally {
                if (default_format == null) {                    
                    default_format = new M3u();
                }
            }
            return default_format;
        }
        
        public static void SetDefaultExportFormat(PlaylistFile format)
        {
            try {
                DefaultExportFormat.Set(format.Extension);               
            } catch (Exception) {
                // Ignore errors.                
            }            
        }
        
        public static int GetFormatIndex(PlaylistFile[] formats, PlaylistFile playlist) 
        {
            int default_export_index = -1;
            foreach (PlaylistFile format in formats) {
                default_export_index++;
                if (format.Extension.Equals(playlist.Extension)) {                    
                    break;
                }
            }
            return default_export_index;
        }
        
        public static PlaylistFile[] GetAllExportFormats()
        {
            return new PlaylistFile[] {
                    new M3u(),
                    new Pls()
                };
        }
        
        public static string[] ImportPlaylist(string playlistUri)
        {            
            PlaylistFile[] formats = PlaylistFileUtil.GetAllExportFormats();            
            string[] uris = null;
            
            // If the file has an extenstion, rearrange the format array so that the 
            // appropriate format is tried first.
            if (System.IO.Path.HasExtension(playlistUri)) {
                string extension = System.IO.Path.GetExtension(playlistUri);
                extension = extension.ToLower();
                
                int index = -1;
                foreach (PlaylistFile format in formats) {
                    index++;                    
                    if (extension.Equals("." + format.Extension)) {                        
                        break;
                    } 
                }
                                
                if (index != -1 && index != 0 && index < formats.Length) {
                    // Move to first position in array.
                    PlaylistFile preferredFormat = formats[index];
                    formats[index] = formats[0];
                    formats[0] = preferredFormat;
                }
            }
            
            
            foreach (PlaylistFile format in formats) {
                try {
                    uris = format.Import(playlistUri);
                    break;
                } catch (InvalidPlaylistException) {                    
                    continue;
                }            
            }
        
            return uris;
        }
    }
    
    public class ImportPlaylistWorker
    {
        private string[] uris;
        private List<string> not_found_uri_list = new List<string>();
        private List<TrackInfo> track_info_list = new List<TrackInfo>();
        
        public ImportPlaylistWorker(string[] uris)
        {
            this.uris = uris;
        }
        
        public void Import()
        {    
            try {
                // If all tracks aren't already in the library, then
                // import them.
                if (!VerifyTracksInLibrary()) {
                    ImportTracksIntoLibrary();
                }                
                CreatePlaylist();
            } catch (PlaylistImportCanceledException) {
                // Do nothing, user canceled import.
            }
        }
        
        private bool VerifyTracksInLibrary()
        {        
            // Verification can take a while when using large playlists.  Use an ActiveUserEvent
            // to show progress.    
            double processed_count = 0;
            double total_count = uris.Length;
            ActiveUserEvent user_event = 
                new ActiveUserEvent(Catalog.GetString("Verifying"));
            user_event.CancelMessage = Catalog.GetString("The playlist import process is " +
                "currently running. Would you like to stop it?");
            user_event.Icon = IconThemeUtils.LoadIcon(22, "system-search", Stock.Find);
            user_event.Message = Catalog.GetString("Verifying playlist tracks exist in library");            
            processed_count = 0;
            
            // Verify tracks are already imported into the library.  If they aren't in the library
            // then we add the tracks to a list so we can import them.
            foreach (string uri in uris) {
                bool found_track = false;
                
                foreach (TrackInfo ti in LibrarySource.Instance.Tracks) {
                    if (ti.Uri.AbsolutePath.Equals(uri)) {
                        found_track = true;
                        break;
                    }
                }
                
                if (!found_track) {                    
                    not_found_uri_list.Add(uri);
                }
                
                processed_count++;
                double new_progress = (double)processed_count / (double)total_count;
                double old_progress = user_event.Progress;
                
                if(new_progress >= 0.0 && new_progress <= 1.0 && 
                        Math.Abs(new_progress - old_progress) > 0.001) {
                    string disp_progress = String.Format(Catalog.GetString("Verifying {0} of {1}"),
                            processed_count, total_count);            
                    user_event.Header = disp_progress;
                    user_event.Message = Catalog.GetString("Verifying ") + uri;
                    user_event.Progress = new_progress;
                }
                
                if(user_event != null && user_event.IsCancelRequested) {
                    DisposeOfUserEvent(user_event);
                    throw new PlaylistImportCanceledException();  
                }
            }
            
            DisposeOfUserEvent(user_event);
            
            return not_found_uri_list.Count == 0;
        }
        
        private void DisposeOfUserEvent(ActiveUserEvent user_event) 
        {
            if(user_event != null) {
                user_event.Dispose();
                user_event = null;
            }
        }
        
        private void ImportTracksIntoLibrary()
        {
            if (not_found_uri_list.Count > 0) {                       
                   // Add the tracks that aren't already in the library.
                   string[] temp_uris = not_found_uri_list.ToArray();
                   Banshee.Library.Import.QueueSource(temp_uris);
                   
                   // Give the ImportManager a second to get started.
                   Thread.Sleep(1000);
                   
                   // Wait for the import to complete.
                   while (Banshee.Library.Import.IsImportInProgress()) {
                       Thread.Sleep(1000);
                   }        
               }
        }
        
        private void CreatePlaylist()
        {
            foreach (string uri in uris) {                  
                foreach (TrackInfo ti in LibrarySource.Instance.Tracks) {
                    if (ti.Uri.AbsolutePath.Equals(uri)) {                                
                        track_info_list.Add(ti);                        
                        break;
                    }
                }
            }
               
               // Create the playlist, add the tracks, then add the playlist to the library.
               if (track_info_list.Count > 0) {                   
                   PlaylistSource playlist = new PlaylistSource();                
                foreach(TrackInfo ti in track_info_list) {
                    playlist.AddTrack(ti);
                }
                
                playlist.Rename(PlaylistUtil.GoodUniqueName(playlist.Tracks));                
                playlist.Commit();                                
                
                LibrarySource.Instance.AddChildSource(playlist);
            }
        }
    }
    
    public class PlaylistExportDialog : Banshee.Gui.Dialogs.FileChooserDialog
    {
        protected ComboBox combobox;
        protected ListStore store;    
        protected PlaylistFile playlist;
        protected string initial_name;
                
        public PlaylistExportDialog(string name, Window parent) : 
            base(Catalog.GetString("Export Playlist"), parent, FileChooserAction.Save)
        {
            initial_name = name;
            playlist = PlaylistFileUtil.GetDefaultExportFormat();             
            CurrentName = playlist.UpdateExtension(initial_name);            
            DefaultResponse = ResponseType.Ok;
            DoOverwriteConfirmation = true;            
            
            AddButton(Stock.Cancel, ResponseType.Cancel);
            AddButton(Catalog.GetString("Export"), ResponseType.Ok);
            
            InitializeExtraWidget();                   
        }
        
        protected void InitializeExtraWidget() 
        {               
            PlaylistFile[] formats = PlaylistFileUtil.GetAllExportFormats();
            int default_export_index = PlaylistFileUtil.GetFormatIndex(formats, playlist);
            
            // Build custom widget used to select the export format.
            store = new ListStore(typeof(string), typeof(PlaylistFile));
            foreach (PlaylistFile format in formats) {
                store.AppendValues(format.Name, format);
            }
                                    
            HBox hBox = new HBox(false, 2);
            
            combobox = new ComboBox(store);
            CellRendererText crt = new CellRendererText();
            combobox.PackStart(crt, true);
            combobox.SetAttributes(crt, "text", 0);
            combobox.Active = default_export_index;
            combobox.Changed += OnComboBoxChange;
            
            hBox.PackStart(new Label(Catalog.GetString("Select Format: ")), false, false, 0);
            hBox.PackStart(combobox, true, true, 0);
            
            combobox.ShowAll();
            hBox.ShowAll();            
            ExtraWidget = hBox; 
        }
        
        protected void OnComboBoxChange(object o, EventArgs args)
        {
            playlist = GetExportFormat();
            
            if (playlist != null) {
                // Store the export format so that we can default to it the
                // next time the user exports.
                PlaylistFileUtil.SetDefaultExportFormat(playlist);
                
                // If the filename has an extension, update it to the extension
                // of the export format.
                string file_name = null;
                
                if (Filename != null) {
                    file_name = System.IO.Path.GetFileName(Filename);
                }
                                
                if (file_name != null) {
                    CurrentName = playlist.UpdateExtension(file_name);
                } else {
                    CurrentName = playlist.UpdateExtension(initial_name);  
                }
            }            
        }
        
        public PlaylistFile GetExportFormat() 
        {
            PlaylistFile selected_playlist = null;
            
            // Get the format that the user selected.
            if (combobox != null && store != null) {
                TreeIter iter;
                if (combobox.GetActiveIter(out iter)) {
                    selected_playlist = store.GetValue(iter, 1) as PlaylistFile;
                }
            }
            
            return selected_playlist;
        }
    }
}
