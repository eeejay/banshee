using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Mono.Unix;

using Banshee.Base;
using Banshee.Configuration;
using Banshee.Database;
using Banshee.Sources;
using Banshee.Library;
using Banshee.Playlists.Formats;
using Banshee.Collection;

namespace Banshee.Playlist
{
    public class PlaylistImportCanceledException : ApplicationException
    {
        public PlaylistImportCanceledException (string message) : base (message)
        {
        }
        
        public PlaylistImportCanceledException () : base ()
        {
        }
    }
    
    public static class PlaylistFileUtil
    {        
        public static readonly SchemaEntry<string> DefaultExportFormat = new SchemaEntry<string> (
            "player_window", "default_export_format",
            String.Empty,
            "Export Format",
            "The default playlist export format"
        );
        
        private static PlaylistFormatDescription [] export_formats = new PlaylistFormatDescription [] {
            M3uPlaylistFormat.FormatDescription,
            PlsPlaylistFormat.FormatDescription
        };
        
        public static PlaylistFormatDescription [] ExportFormats {
            get { return export_formats; }
        }
        
        public static bool IsSourceExportSupported (Source source)
        {
            bool supported = true;
            
            if (source == null || !(source is AbstractPlaylistSource)) {
                supported = false;
            }
            
            return supported;
        }
        
        public static PlaylistFormatDescription GetDefaultExportFormat ()
        {
            PlaylistFormatDescription default_format = null;
            try {
                string exportFormat = DefaultExportFormat.Get ();
                PlaylistFormatDescription [] formats = PlaylistFileUtil.ExportFormats;
                foreach (PlaylistFormatDescription format in formats) {
                    if (format.FileExtension.Equals (exportFormat)) {
                        default_format = format;
                        break;
                    }
                }
            } catch {            
                // Ignore errors, return our default if we encounter an error.                
            } finally {
                if (default_format == null) {                    
                    default_format = M3uPlaylistFormat.FormatDescription;
                }
            }
            return default_format;
        }
        
        public static void SetDefaultExportFormat (PlaylistFormatDescription format)
        {
            try {
                DefaultExportFormat.Set (format.FileExtension);        
            } catch (Exception) {
                // Ignore errors.                
            }            
        }
        
        public static int GetFormatIndex (PlaylistFormatDescription [] formats, PlaylistFormatDescription playlist) 
        {
            int default_export_index = -1;
            foreach (PlaylistFormatDescription format in formats) {
                default_export_index++;
                if (format.FileExtension.Equals (playlist.FileExtension)) {                    
                    break;
                }
            }
            return default_export_index;
        }
        
        public static string [] ImportPlaylist (string playlistUri)
        {            
            PlaylistFormatDescription [] formats = PlaylistFileUtil.ExportFormats;            
            
            // If the file has an extenstion, rearrange the format array so that the 
            // appropriate format is tried first.
            if (System.IO.Path.HasExtension (playlistUri)) {
                string extension = System.IO.Path.GetExtension (playlistUri);
                extension = extension.ToLower ();
                
                int index = -1;
                foreach (PlaylistFormatDescription format in formats) {
                    index++;                    
                    if (extension.Equals ("." + format.FileExtension)) {                        
                        break;
                    } 
                }
                                
                if (index != -1 && index != 0 && index < formats.Length) {
                    // Move to first position in array.
                    PlaylistFormatDescription preferredFormat = formats[index];
                    formats[index] = formats[0];
                    formats[0] = preferredFormat;
                }
            }
            
            List<string> uris = new List<string> ();
                
            foreach (PlaylistFormatDescription format in formats) {
                try {
                    IPlaylistFormat playlist = (IPlaylistFormat)Activator.CreateInstance (format.Type);
                    playlist.Load (Banshee.IO.IOProxy.File.OpenRead (new SafeUri (playlistUri)), true);
                    foreach (Dictionary<string, object> element in playlist.Elements) {
                        uris.Add (((Uri)element["uri"]).AbsoluteUri);
                    }
                    break;
                } catch (InvalidPlaylistException) {                    
                    continue;
                }      
            }
        
            return uris.ToArray ();
        }
    }
    
    public class ImportPlaylistWorker
    {
        private string [] uris;
        private string name;
        
        public ImportPlaylistWorker (string name, string [] uris)
        {
            this.name = name;
            this.uris = uris;
        }
        
        public void Import ()
        {    
            try {
                Banshee.ServiceStack.ServiceManager.Get<LibraryImportManager> ("LibraryImportManager").ImportFinished += delegate {
                    CreatePlaylist ();
                };

                Banshee.ServiceStack.ServiceManager.Get<LibraryImportManager> ("LibraryImportManager").QueueSource (uris);
            } catch (PlaylistImportCanceledException) {
                // Do nothing, user canceled import.
            }
        }
        
        private void CreatePlaylist ()
        {
            PlaylistSource playlist = new PlaylistSource (name);
            playlist.Save ();

            BansheeDbCommand command = new BansheeDbCommand (
                @"INSERT INTO CorePlaylistEntries (PlaylistID, TrackID)
                    VALUES (?, (SELECT TrackID FROM CoreTracks WHERE Uri = ?))", 2
            );

            foreach (string uri in uris) {
                command.ApplyValues (playlist.DbId, uri);
            }
        }
    }
}
