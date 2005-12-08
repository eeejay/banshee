/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  AudioCdDisk.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using MusicBrainz;

namespace Banshee.Base
{
    public class AudioCdDisk
    {
        private string udi;
        private string device_node;
        private string drive_name;
        
        private bool mb_queried = false;
        
        private string album_title;
        
        private AudioCdTrackInfo [] tracks;
        
        public event EventHandler Updated;

        public AudioCdDisk(string udi, string deviceNode, string driveName)
        {
            this.udi = udi;
            device_node = deviceNode;
            drive_name = driveName;
            
            Globals.Network.StateChanged += OnNetworkStateChanged;
            
            LoadDiskInfo();
        }
              
        private void LoadDiskInfo()
        {
            ArrayList track_list = new ArrayList();
            SimpleDisc mb_disc = new SimpleDisc(device_node);
            //mb_disc.Client.Debug = true;
            
            foreach(SimpleTrack mb_track in mb_disc) {
                AudioCdTrackInfo track = new AudioCdTrackInfo(device_node);
                track.Duration = new TimeSpan(mb_track.Length * TimeSpan.TicksPerSecond);
                track.TrackIndex = mb_track.Index;
                track.Artist = Catalog.GetString("Unknown Artist");
                track.Album = Catalog.GetString("Unknown Album");
                track.Title = String.Format(Catalog.GetString("Track {0}"), mb_track.Index);
                
                track_list.Add(track);
            }
            
            album_title = Catalog.GetString("Audio CD");
            tracks = track_list.ToArray(typeof(AudioCdTrackInfo)) as AudioCdTrackInfo [];
            
            ThreadPool.QueueUserWorkItem(QueryMusicBrainz, mb_disc);
        }
        
        private void OnNetworkStateChanged(object o, NetworkStateChangedArgs args)
        {
            if(!mb_queried && args.Connected) {
                ThreadPool.QueueUserWorkItem(QueryMusicBrainz, null);
            }
        }
        
        private void QueryMusicBrainz(object o)
        {
            if(!Globals.Network.Connected) {
                return;
            }
            
            SimpleDisc mb_disc;
            
            if(o == null) {
                mb_disc = new SimpleDisc(device_node);
            } else {
                mb_disc = o as SimpleDisc;
            }
            
            try {
                mb_disc.QueryCDMetadata();
            } catch(Exception) {
                return;
            }
            
            int min = tracks.Length < mb_disc.Tracks.Length 
                ? tracks.Length : mb_disc.Tracks.Length;
            
            if(mb_disc.AlbumName != null) {
                album_title = mb_disc.AlbumName;
            } 
            
            for(int i = 0; i < min; i++) {
                tracks[i].Duration = new TimeSpan(mb_disc[i].Length * TimeSpan.TicksPerSecond);
                tracks[i].TrackIndex = mb_disc[i].Index;
                
                if(mb_disc[i].Artist != null) {
                    tracks[i].Artist = mb_disc[i].Artist;
                }
                
                if(mb_disc.AlbumName != null) {
                    tracks[i].Album = mb_disc.AlbumName;
                }
                
                if(mb_disc[i].Title != null) {
                    tracks[i].Title = mb_disc[i].Title;
                }
                
                tracks[i].Asin = mb_disc.AmazonAsin;
            }
            
            string asin = mb_disc.AmazonAsin;
            mb_disc.Dispose();
            
            mb_queried = true;
            
            Gtk.Application.Invoke(delegate {
                if(Updated != null) {
                    Updated(this, new EventArgs());
                }
            });
            
            string path = Paths.GetCoverArtPath(asin);
            if(System.IO.File.Exists(path)) {
                return;
            }
            
            if(AmazonCoverFetcher.Fetch(asin, Paths.CoverArtDirectory)) {
                Gtk.Application.Invoke(delegate {
                    if(Updated != null) {
                        Updated(this, new EventArgs());
                    }
                });
            }
        }
        
        [DllImport("libc")]
        private static extern int ioctl(int device, EjectOperation request); 

        private enum EjectOperation {
            Open = 0x5309,
            Close = 0x5319
        }

        public bool Eject()
        {
            return Eject(true);
        }

        public bool Eject(bool open)
        {
            try {
                Hal.Device device = new Hal.Device(HalCore.Context, udi);
                if(device.GetPropertyBool("volume.is_mounted")) {
                    if(!Utilities.UnmountVolume(device_node)) {
                        return false;
                    }
                }
            
                using(UnixStream stream = (new UnixFileInfo(device_node)).Open( 
                    Mono.Unix.Native.OpenFlags.O_RDONLY | 
                    Mono.Unix.Native.OpenFlags.O_NONBLOCK)) {
                    return ioctl(stream.Handle, open
                        ? EjectOperation.Open
                        : EjectOperation.Close) == 0;
                }
            } catch {
                return false;
            }
        }
        
        public string Udi { 
            get { 
                return udi;
            }
        }
        
        public string DeviceNode { 
            get { 
                return device_node;
            }
        }
        
        public string DriveName { 
            get { 
                return drive_name;
            }
        }
        
        public string Title { 
            get { 
                return album_title;
            }
        }
        
        public int TrackCount { 
            get { 
                return tracks.Length; 
            }
        }
        
        public bool Valid { 
            get { 
                return true;
            }
        }
        
        public AudioCdTrackInfo [] Tracks { 
            get { 
                return tracks; 
            }
        }
    }
}
