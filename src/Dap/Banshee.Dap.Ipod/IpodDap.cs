/***************************************************************************
 *  IpodDap.cs
 *
 *  Copyright (C) 2005-2007 Novell, Inc.
 *  Written by Aaron Bockvoer <abockover@novell.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using Mono.Unix;
using Gtk;
using Hal;
using IPod;
using IPod.Hal;

using Banshee.Base;
using Banshee.Dap;
using Banshee.Metadata;
using Banshee.Sources;
using Banshee.Widgets;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Dap.Ipod.IpodDap)
        };
    }
}

namespace Banshee.Dap.Ipod
{
    [DapProperties(DapType = DapType.NonGeneric, PipelineName="Ipod")]
    [SupportedCodec(CodecType.Mp3)]
    [SupportedCodec(CodecType.Mp4)]
    public sealed class IpodDap : DapDevice, IPlaylistCapable
    {
        private IPod.Device device;
        private Hal.Device hal_device;
        private bool database_supported;
        private UnsupportedDatabaseView db_unsupported_container;
        private bool metadata_provider_initialized = false;
        private List<SafeUri> added_tracks = new List<SafeUri>();
    
        public override InitializeResult Initialize(Hal.Device halDevice)
        {
            if (!metadata_provider_initialized) {
                MetadataService.Instance.AddProvider (0, new IpodMetadataProvider ());
                metadata_provider_initialized = true;
            }
            
            hal_device = halDevice;
            
            if(!hal_device.PropertyExists("block.device") || 
                !hal_device.PropertyExists("block.is_volume") ||
                !hal_device.GetPropertyBoolean("block.is_volume") ||
                hal_device.Parent["portable_audio_player.type"] != "ipod") {
                return InitializeResult.Invalid;
            } else if(!hal_device.GetPropertyBoolean("volume.is_mounted")) {
                return WaitForVolumeMount(hal_device);
            }
            
            if(LoadIpod() == InitializeResult.Invalid) {
                return InitializeResult.Invalid;
            }

            base.Initialize(halDevice);
            
            InstallProperty("Generation", device.Generation.ToString());
            InstallProperty("Model", device.ModelClass);
           // InstallProperty("Model Number", device.ModelNumber);
            InstallProperty("Serial Number", device.SerialNumber);
            InstallProperty("Firmware Version", device.FirmwareVersion);
            InstallProperty("Database Version", device.TrackDatabase.Version.ToString());
            
            if(device.ProductionYear > 0) {
                // Translators "Week 25 of 2006"
                InstallProperty(Catalog.GetString("Manufactured During"), 
                    String.Format(Catalog.GetString("Week {0} of {1}"), device.ProductionWeek, 
                        device.ProductionYear.ToString("0000")));
            }
            
            if(device.ShouldAskIfUnknown) {
                GLib.Timeout.Add(5000, AskAboutUnknown);
            }

            ReloadDatabase(false);
            
            CanCancelSave = false;
            return InitializeResult.Valid;
        }
        
        private InitializeResult LoadIpod()
        {
            try {
					//FIXME: Major Hack! Need to figure out selecting a specific ipod
					IPod.DeviceManager devman = IPod.Hal.HalDeviceManager.Create();
					foreach( IPod.Device dev in devman.Devices){
						device = dev;
					}
					//device = new IPod.Hal.HalDevice(
                //device = new IPod.Hal.HalDevice(hal_device["block.device"]);
                if(File.Exists(Path.Combine(device.ControlPath, Path.Combine("iTunes", "iTunesDB")))) { 
                    device.LoadTrackDatabase();
                } else {
                    throw new DatabaseReadException("iTunesDB does not exist");
                }
                database_supported = true;
            } catch(DatabaseReadException) {
                device.LoadTrackDatabase(true);
                database_supported = false;
            } catch {
                return InitializeResult.Invalid;
            }
            
            return InitializeResult.Valid;
        }
        
        private bool AskAboutUnknown()
        {
            HigMessageDialog dialog = new HigMessageDialog(null, Gtk.DialogFlags.Modal,
                Gtk.MessageType.Warning, Gtk.ButtonsType.None,
                Catalog.GetString("Your iPod could not be identified"),
                Catalog.GetString("Please consider submitting information about your iPod " +
                    "to the Banshee Project so your iPod may be more fully identified in the future.\n"));
        
            CheckButton do_not_ask = new CheckButton(Catalog.GetString("Do not ask me again"));
            do_not_ask.Show();
            dialog.LabelVBox.PackStart(do_not_ask, false, false, 0);
            
            dialog.AddButton("gtk-cancel", Gtk.ResponseType.Cancel, false);
            dialog.AddButton(Catalog.GetString("Go to Web Site"), Gtk.ResponseType.Ok, false);
            
            try {
                if(dialog.Run() == (int)ResponseType.Ok) {
                    do_not_ask.Active = true;
                    Banshee.Web.Browser.Open(device.UnknownIpodUrl);
                }
            } finally {
                dialog.Destroy();
            }
            
            if(do_not_ask.Active) {
                device.DoNotAskIfUnknown();
            }
            
            return false;
        }
        
        public DapPlaylistSource AddPlaylist(Source playlist){        	
       		IPodPlaylistSource ips = new IPodPlaylistSource(this, playlist.Name);       		
       		       		
       		LogCore.Instance.PushDebug("In IPodDap.AddPlaylist" , "");
            
            foreach(TrackInfo ti in playlist.Tracks) {
                LogCore.Instance.PushDebug("Adding track " + ti.ToString() , " to new playlist " + ips.Name);
                IpodDapTrackInfo idti = new IpodDapTrackInfo(ti, device.TrackDatabase);
                ips.AddTrack(idti);
                AddTrack(idti);                
            }
            
        	return (DapPlaylistSource) ips;
        }
        
        public override void AddTrack(TrackInfo track)
        {
            AddTrack(track, false);
        }

        private void AddTrack(TrackInfo track, bool loading)
        {
            if (track == null || (IsReadOnly && !loading))
                return;

            IpodDapTrackInfo new_track = null;

            if(track is IpodDapTrackInfo)
                new_track = track as IpodDapTrackInfo;
            else
                new_track = new IpodDapTrackInfo(track, device.TrackDatabase);
            
            if (new_track != null && !added_tracks.Contains(new_track.Uri)) {                
                tracks.Add(new_track);
                OnTrackAdded(new_track);
                added_tracks.Add(new_track.Uri);                
            }
        }
        
        public override void RemoveTrack(TrackInfo track)
        {
            if (SourceManager.ActiveSource == Source) {
                if(!(track is IpodDapTrackInfo)) {
                    LogCore.Instance.PushDebug("In IPodDap.TrackRemoved" , "track is NOT IpodDapTrackInfo");
                    return;
                }
                
                IpodDapTrackInfo ipod_track = (IpodDapTrackInfo) track;
                device.TrackDatabase.RemoveTrack(ipod_track.Track);
                
                // User is removing from the master list.  Remove the track
                // from any child sources.
                RemoveTrackFromPlaylists(ipod_track);
                base.RemoveTrack(ipod_track);   
            } else {
                // The user is removing a track from a child source.  Only remove
                // from the Source if it is no longer in use by any other playlists.
                RemoveTrackIfNotInPlaylists(track, null);
            }
        }
        
        public void RemoveTrackIfNotInPlaylists(TrackInfo track, Source source_to_ignore)
        {
            // Loop through the other Banshee playlists.  If the 
            // track isn't used by any other playlists, then remove
            // it from the Source playlist.  When the user removes 
            // from a child source, this is necessary to keep
            // the Source playlist in sync with the child playlists.
            bool in_use = false;
            foreach (Source c in Source.Children) {
                if (source_to_ignore != null && source_to_ignore.Name == c.Name) {
                    continue;
                }
            
                foreach (TrackInfo ti in c.Tracks) {
                    if (TrackCompare(track, ti)) {
                        in_use = true;
                        break;
                    }
                }
                
                if (in_use) {
                    break;
                }
            }
            
            if (!in_use) {
                if(!(track is IpodDapTrackInfo)) {
                    LogCore.Instance.PushDebug("In IPodDap.TrackRemoved" , "track is NOT IpodDapTrackInfo");
                    return;
                }
                
                IpodDapTrackInfo ipod_track = (IpodDapTrackInfo) track;
                device.TrackDatabase.RemoveTrack(ipod_track.Track);
                
                // Find a reference to the equivalent track in the source list.
                // When removing a track from a playlist, we won't have a reference
                // to the track in the Source list.  We will only have an equivalent
                // track info object.
                foreach (TrackInfo ti in Tracks) {
                    if (TrackCompare(track, ti)) {                
                        base.RemoveTrack(ti);                  
                        break;
                    }                    
                }
            }            
        }
        
        public void RemoveTrackFromPlaylists(TrackInfo track)
        {
            // Loop through the other Banshee playlists.  Remove the track if
            // it appears in any other playlists.            
            foreach (Source c in Source.Children) {
                foreach (TrackInfo ti in c.Tracks) {
                    if (TrackCompare(track, ti)) {
                        c.RemoveTrack(ti); 
                        break;                        
                    }
                }
            }           
        }
        
        private void ReloadDatabase(bool refresh)
        {
            LogCore.Instance.PushDebug("Reloading iPod Database", "");
            bool previous_database_supported = database_supported;
            
            ClearTracks(false);
            added_tracks.Clear();
            
            if(refresh) {
                device.TrackDatabase.Reload();
            }            
            
            if(database_supported ||  !device.IsShuffle) {
            
                LogCore.Instance.PushDebug("Clearing child sources.", "");
                // Clear the playlists in Banshee.  This is to clear the lists
                // when reloading the database after a sync.
                Source.ClearChildSources();                
                
                // Add the ipod playlists to Banshee.
                foreach(Playlist p in device.TrackDatabase.Playlists) {
                    LogCore.Instance.PushDebug("Adding iPod playlist '" + p.Name + "' to Banshee", "");
                    IPodPlaylistSource ps = new IPodPlaylistSource(this, p.Name);
           			foreach (Track t in p.Tracks) {
           			    IpodDapTrackInfo idti = new IpodDapTrackInfo(t);           			    
           			    ps.AddTrack(idti);
           			    AddTrack(idti, true);           			    
           			}       
                    Source.SourceDrop(ps);
                }
                
                // Add tracks that aren't in a playlist to the master DAP
                // playlist.  AddTrack prevents duplicates.
                foreach(Track t in device.TrackDatabase.Tracks) {
                    IpodDapTrackInfo idti = new IpodDapTrackInfo(t);  
                    AddTrack(idti, true);                    
                }
                
            } else if (database_supported || (device.IsShuffle)) {
            
                // Shuffles don't support playlists ???  I think this is true.
                foreach(Track track in device.TrackDatabase.Tracks) {                    
                    IpodDapTrackInfo ti = new IpodDapTrackInfo(track);
                    AddTrack(ti, true);
                }
                
            } else {
                BuildDatabaseUnsupportedWidget();
            }
            
            if(previous_database_supported != database_supported) {
                OnPropertiesChanged();
            }
        }
        
        public override void Eject()
        {
            try {
                device.Eject();
                base.Eject();
            } catch(Exception e) {
                LogCore.Instance.PushError(Catalog.GetString("Could not eject iPod"),
                    e.Message);
            }
        }
        
        public override void Synchronize()
        {
            UpdateSaveProgress(
                Catalog.GetString("Synchronizing iPod"), 
                Catalog.GetString("Pre-processing tracks"),
                0.0);
            
            // Create a hashtable of iPod tracks.  When we add go to add
            // tracks to the iPod playlists, we need iPod tracks.  
            Hashtable ipod_tracks_hash = new Hashtable();
            
            foreach(IpodDapTrackInfo track in Tracks) {
                Track ipod_track = null;
                
                if(track.Track == null) {
                    ipod_track = CommitTrackToDevice(track);
                } else {
                    ipod_track = track.Track;
                    track.Track.Uri = new Uri(track.Uri.AbsoluteUri);                    
                }
                
                ipod_tracks_hash.Add(track.Uri, ipod_track);
            }
            
            LogCore.Instance.PushDebug("Starting Synchronize", "Finished Tracks, starting playlists");
            
            // Loop through the playlists in Banshee that the user has configured for the iPod.
            // Add playlists on the iPod if they don't exist. We remove all tracks and then add them back
            // to ensure the right tracks exist in the playlist in the proper order.
            foreach (Source c in Source.Children) {
                LogCore.Instance.PushDebug("Checking ChildSource " + c.Name + "", "");
                Playlist p = device.TrackDatabase.LookupPlaylist(c.Name);
                if (p == null) {
                    LogCore.Instance.PushDebug("Adding ipod playlist " + c.Name , "It wasn't on the iPod.");
                    try {
                        p = device.TrackDatabase.CreatePlaylist(c.Name);
                    } catch (InvalidCastException ice) {
                        LogCore.Instance.PushDebug("Caught InvalidCastException when creating " + c.Name , ice.ToString());
                        continue;
                    }
                }
                
                if (p == null) {
                    LogCore.Instance.PushDebug("Creating playlist on ipod failed for " + c.Name , "");
                    continue;
                } else {
                    LogCore.Instance.PushDebug("Playlist is not null.", "");
                }
                
                // Remove all tracks in the playlist.
                int num_tracks = p.Tracks.Count;
                for (int i = 0; i < num_tracks; i++) {
                    p.RemoveTrack(0);
                    LogCore.Instance.PushDebug("Removed first row.", p.Tracks.Count + " remaining in playlist.");
                }                
                
                // Add them back.
                foreach (TrackInfo ti in c.Tracks) {
                    LogCore.Instance.PushDebug("Adding track " + ti.ToString() , " to playlist " + p.Name);
                    
                    
                    if (ti is IpodDapTrackInfo) {
                        LogCore.Instance.PushDebug("ti is IpodDapTrackInfo", "");
                        IpodDapTrackInfo idti = ti as IpodDapTrackInfo;
                        
                        object ipod_track_obj = ipod_tracks_hash[idti.Uri];
                        if (ipod_track_obj == null) {
                            LogCore.Instance.PushDebug("Couldn't find ipod track in hash!", "");
                            continue;
                        }
                        
                        if (ipod_track_obj is Track) {
                            LogCore.Instance.PushDebug("Adding Ipod track", "");                        
                            p.AddTrack(ipod_track_obj as Track);
                        } else {
                            LogCore.Instance.PushDebug("ipod_track_object not an instance of Track", "continuing to next track");
                            continue;
                        }                       
                        
                    } else {
                        LogCore.Instance.PushDebug("ti is not IpodDapTrackInfo", "");
                        continue;
                    }    
                    
                    /*
                    IpodDapTrackInfo idti = null;
                    if (ti is IpodDapTrackInfo) {
                        LogCore.Instance.PushDebug("ti is IpodDapTrackInfo", "");
                        idti = ti as IpodDapTrackInfo;
                    } else {
                        LogCore.Instance.PushDebug("ti is not IpodDapTrackInfo", "");
                        idti = new IpodDapTrackInfo(ti, device.TrackDatabase);
                    }                    
                    
                    if (idti == null) {
                        LogCore.Instance.PushDebug("idti is null", "");
                        continue;
                    }
                    
                    // TODO: the track may already be on the ipod.  I don't want to copy it again if it is, so 
                    // I need to figure out how to determine which tracks have already been added to the ipod 
                    // and get a ref to the Track for this case.
                    Track track = idti.Track;
                    if(track == null) {
                        LogCore.Instance.PushDebug("track is null", "Adding track to ipod");
                        track = CommitTrackToDevice(idti);
                        
                        if (track == null) {
                            LogCore.Instance.PushDebug("track is still null after committing!", "");
                            continue;
                        }
                    } 
                    
                    LogCore.Instance.PushDebug("Adding Ipod track", "");
                    p.AddTrack(track);
                    */
                }
            }
            
            LogCore.Instance.PushDebug("Finished adding playlists, checking for removed playlists. ", "");
            
            // Remove playlists that have been deleted.
            foreach (Playlist p in device.TrackDatabase.Playlists) {
                LogCore.Instance.PushDebug("Checking ipod playlist " + p.Name , "");
                bool found_playlist = false;
                foreach (Source c in Source.Children) {
                    if (c.Name == p.Name) {
                        found_playlist = true;
                        break;
                    }
                }
                
                if (!found_playlist) {
                    LogCore.Instance.PushDebug("Removing playlist " + p.Name + "; It is missing in Banshee", "");
                    device.TrackDatabase.RemovePlaylist(p);
                } else {
                    LogCore.Instance.PushDebug("Found playlist " + p.Name + " in Banshee", "");
                }
            }
                        
            LogCore.Instance.PushDebug("Finished modifying playlists", "Continuing regular synchronization process");
            
            device.TrackDatabase.SaveProgressChanged += delegate(object o, TrackSaveProgressArgs args)
            {
                double progress = args.CurrentTrack == null ? 0.0 : args.TotalProgress;
                string message = args.CurrentTrack == null 
                    ? Catalog.GetString("Flushing to Disk (may take time)")
                    : args.CurrentTrack.Artist + " - " + args.CurrentTrack.Title;
                    
                UpdateSaveProgress(Catalog.GetString("Synchronizing iPod"), message, progress);
            };

            try {
                device.TrackDatabase.Save();
            } catch(Exception e) {
                Console.Error.WriteLine (e);
                LogCore.Instance.PushError(Catalog.GetString("Failed to synchronize iPod"), e.Message);
            } finally {
                ReloadDatabase(true);
                FinishSave();
            }
        }
        
        private Track CommitTrackToDevice(IpodDapTrackInfo ti)
        {
            Track track = device.TrackDatabase.CreateTrack();
                        
            try {
                track.Uri = new Uri(ti.Uri.AbsoluteUri);
            } catch {
                device.TrackDatabase.RemoveTrack (track);
                return null;
            }
        
            if(ti.Album != null) {
                track.Album = ti.Album;
            }
            
            if(ti.Artist != null) {
                track.Artist = ti.Artist;
            }
            
            if(ti.Title != null) {
                track.Title = ti.Title;
            }
            
            if(ti.Genre != null) {
                track.Genre = ti.Genre;
            }
            
            track.Duration = ti.Duration;
            track.TrackNumber = (int)ti.TrackNumber;
            track.TotalTracks = (int)ti.TrackCount;
            track.Year = (int)ti.Year;
            track.LastPlayed = ti.LastPlayed;
            
            switch(ti.Rating) {
                case 1: track.Rating = TrackRating.Zero; break;
                case 2: track.Rating = TrackRating.Two; break;
                case 3: track.Rating = TrackRating.Three; break;
                case 4: track.Rating = TrackRating.Four; break;
                case 5: track.Rating = TrackRating.Five; break;
                default: track.Rating = TrackRating.Zero; break;
            }
            
            if(track.Artist == null) {
                track.Artist = String.Empty;
            }
            
            if(track.Album == null) {
                track.Album = String.Empty;
            }
            
            if(track.Title == null) {
                track.Title = String.Empty;
            }
            
            if(track.Genre == null) {
                track.Genre = String.Empty;
            }

            if (ti.CoverArtFileName != null && File.Exists (ti.CoverArtFileName)) {
                try {
                    Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (ti.CoverArtFileName);

                    if (pixbuf != null) {
                        SetCoverArt (track, ArtworkUsage.Cover, pixbuf);
                        pixbuf.Dispose ();
                    }
                } catch (Exception e) {
                    Console.Error.WriteLine ("Failed to set cover art from {0}: {1}", ti.CoverArtFileName, e);
                }
            }
            
            // tale: I don't think this will break things.  I added it
            // because I need the track to add to playlists.
            return track;
        }

        private void SetCoverArt (Track track, ArtworkUsage usage, Gdk.Pixbuf pixbuf)
        {
            foreach (ArtworkFormat format in device.LookupArtworkFormats (usage)) {
                if (!track.HasCoverArt (format)) {
                    track.SetCoverArt (format, ArtworkHelpers.ToBytes (format, pixbuf));
                }
            }
        }
        
        public override Gdk.Pixbuf GetIcon(int size)
        {
            string prefix = "multimedia-player-";
            string id = null;
				//FIXME: Need to reinstate ipod-icon logic
//            switch(device.ModelClass) {
//                case DeviceModel.Color: id = "ipod-standard-color"; break;
//                case DeviceModel.ColorU2: id = "ipod-U2-color"; break;
//                case DeviceModel.Regular: id = "ipod-standard-monochrome"; break;
//                case DeviceModel.RegularU2: id = "ipod-U2-monochrome"; break;
//                case DeviceModel.Mini: id = "ipod-mini-silver"; break;
//                case DeviceModel.MiniBlue: id = "ipod-mini-blue"; break;
//                case DeviceModel.MiniPink: id = "ipod-mini-pink"; break;
//                case DeviceModel.MiniGreen: id = "ipod-mini-green"; break;
//                case DeviceModel.MiniGold: id = "ipod-mini-gold"; break;
//                case DeviceModel.Shuffle: id = "ipod-shuffle"; break;
//                case DeviceModel.NanoWhite: id = "ipod-nano-white"; break;
//                case DeviceModel.NanoBlack: id = "ipod-nano-black"; break;
//                case DeviceModel.VideoWhite: id = "ipod-video-white"; break;
//                case DeviceModel.VideoBlack: id = "ipod-video-black"; break;
//                default:
//                    if(device.IsShuffle) {
//                        id = "ipod-shuffle";
//                    } else if(device.Model >= DeviceModel.NanoSilver &&
//                        device.Model <= DeviceModel.NanoProductRed) {
//                        id = "ipod-nano-white";
//                    } else {
//                        id = "ipod-standard-monochrome";
//                    }
//                    break;
//            }
            if(device.IsShuffle) {
                        id = "ipod-shuffle";
                    } else {
                        id = "ipod-standard-monochrome";
                    }
            Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(prefix + id, size);
            
            if(icon != null) {
                return icon;
            }
            
            return base.GetIcon(size);
        }
        
        public override void SetName(string name)
        {
            device.Name = name;
            device.Save();
        }
        
        public override void SetOwner(string owner)
        {
            //device. = owner;
            //device.Save();
        }
        
        private void BuildDatabaseUnsupportedWidget()
        {
            db_unsupported_container = new UnsupportedDatabaseView(this);
            db_unsupported_container.Show();
            db_unsupported_container.Refresh += delegate(object o, EventArgs args) {
                LoadIpod();
                ReloadDatabase(false);
                OnReactivate();
            };
        }
        
        public override string Name {
            get {
                if(device.Name != null && device.Name != String.Empty) {
                    return device.Name;
                } else if(hal_device.PropertyExists("volume.label")) {
                    return hal_device["volume.label"];
                } else if(hal_device.PropertyExists("info.product")) {
                    return hal_device["info.product"];
                }
                
                return "iPod";
            }
        }
        
        public override string Owner {
            get {
                return device.Name;
            }
        }
        
        public override ulong StorageCapacity {
            get {
                return device.VolumeSize;
            }
        }
        
        public override ulong StorageUsed {
            get {
                return device.VolumeUsed;
            }
        }
        
        public override bool IsReadOnly {
            get {
                return !device.CanWrite;
            }
        }
        
        public override bool IsPlaybackSupported {
            get {
                return true;
            }
        }
        
        public override string GenericName {
            get {
                return "iPod";
            }
        }
        
        public override Gtk.Widget ViewWidget {
            get {
                return !database_supported ? db_unsupported_container : null;
            }
        }
        
        internal IPod.Device Device {
            get {
                return device;
            }
        }
    }
}
