//
// AudioCdRipper.cs
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
using System.Collections.Generic;

using Mono.Unix;
using Mono.Addins;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.MediaEngine;

namespace Banshee.AudioCd
{
    public class AudioCdRipper : IDisposable
    {
        private static bool ripper_extension_queried = false;
        private static TypeExtensionNode ripper_extension_node = null;
        
        public static bool Supported {
            get { 
                if (ripper_extension_queried) {
                    return ripper_extension_node != null;
                }
                
                ripper_extension_queried = true;
                
                foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (
                    "/Banshee/MediaEngine/AudioCdRipper")) {
                    ripper_extension_node = node;
                    break;
                }
                
                return ripper_extension_node != null;
            }
        }

        public event EventHandler Finished;
        
        // State that does real work
        private IAudioCdRipper ripper;
        private AudioCdSource source;
        private UserJob user_job;
        
        // State to process the rip operation
        private Queue<AudioCdTrackInfo> queue = new Queue<AudioCdTrackInfo> ();
        
        private TimeSpan ripped_duration;
        private TimeSpan total_duration;
        private int track_index;
        
        // State to compute/display the rip speed (i.e. 24x)
        private TimeSpan last_speed_poll_duration;
        private DateTime last_speed_poll_time;
        private double last_speed_poll_factor;
        private string status;
        
        public AudioCdRipper (AudioCdSource source)
        {
            if (ripper_extension_node != null) {
                ripper = (IAudioCdRipper)ripper_extension_node.CreateInstance ();
                ripper.TrackFinished += OnTrackFinished;
                ripper.Progress += OnProgress;
                ripper.Error += OnError;
            } else {
                throw new ApplicationException ("No AudioCdRipper extension is installed");
            }
            
            this.source = source;
        }
        
        public void Start ()
        {   
            ResetState ();

            foreach (AudioCdTrackInfo track in source.DiscModel) {
                if (track.RipEnabled) {
                    total_duration += track.Duration;
                    queue.Enqueue (track);
                }
            }
            
            if (queue.Count == 0) {
                return;
            }

            source.LockAllTracks ();
                                                
            user_job = new UserJob (Catalog.GetString ("Importing Audio CD"), 
                Catalog.GetString ("Initializing Drive"), "media-import-audio-cd");
            user_job.CancelMessage = String.Format (Catalog.GetString (
                "<i>{0}</i> is still being imported into the music library. Would you like to stop it?"
                ), GLib.Markup.EscapeText (source.DiscModel.Title));
            user_job.CanCancel = true;
            user_job.CancelRequested += OnCancelRequested;
            user_job.Finished += OnFinished;
            user_job.Register ();
            
            if (source != null && source.DiscModel != null) {
                if (!source.DiscModel.LockDoor ()) {
                    Hyena.Log.Warning ("Could not lock CD-ROM door", false);
                }
            }
            
            ripper.Begin (source.DiscModel.Volume.DeviceNode, AudioCdService.ErrorCorrection.Get ());
            
            RipNextTrack ();
        }
        
        public void Dispose ()
        {
            ResetState ();
            
            if (source != null && source.DiscModel != null) {
                source.DiscModel.UnlockDoor ();
            }
                            
            if (ripper != null) {
                ripper.Finish ();
                ripper = null;
            }
            
            if (user_job != null) {
                user_job.Finish ();
                user_job = null;
            }
        }
        
        private void ResetState ()
        {
            track_index = 0;
            ripped_duration = TimeSpan.Zero;
            total_duration = TimeSpan.Zero;
            last_speed_poll_duration = TimeSpan.Zero;
            last_speed_poll_time = DateTime.MinValue;
            last_speed_poll_factor = 0;
            status = null;
            queue.Clear ();
        }
        
        private void RipNextTrack ()
        {
            if (queue.Count == 0) {
                OnFinished ();
                Dispose ();
                return;
            }
            
            AudioCdTrackInfo track = queue.Dequeue ();

            user_job.Title = String.Format (Catalog.GetString ("Importing {0} of {1}"), 
                ++track_index, source.TrackModel.Count);
            status = String.Format("{0} - {1}", track.ArtistName, track.TrackTitle);
            user_job.Status = status;
            
            SafeUri uri = new SafeUri (FileNamePattern.BuildFull (track, null));
            bool tagging_supported;
            ripper.RipTrack (track.IndexOnDisc, track, uri, out tagging_supported);
        }

#region Ripper Event Handlers

        private void OnTrackFinished (object o, AudioCdRipperTrackFinishedArgs args)
        {
            if (user_job == null || ripper == null) {
                return;
            }
        
            AudioCdTrackInfo track = (AudioCdTrackInfo)args.Track;
        
            ripped_duration += track.Duration;
            track.PrimarySource = ServiceManager.SourceManager.MusicLibrary;
            track.Uri = args.Uri;
            
            if (track.Artist != null) {
                track.Artist = DatabaseArtistInfo.UpdateOrCreate (track.Artist);
                track.ArtistId = track.Artist.DbId;
            }
            
            if (track.Album != null && track.AlbumArtist != null) {
                DatabaseArtistInfo.UpdateOrCreate (track.AlbumArtist);
                track.Album = DatabaseAlbumInfo.UpdateOrCreate (track.AlbumArtist, track.Album);
                track.AlbumId = track.Album.DbId;
            }

            track.FileSize = Banshee.IO.File.GetSize (track.Uri);
            
            track.Save ();
            
            source.UnlockTrack (track);
            RipNextTrack ();
        }
        
        private void OnProgress (object o, AudioCdRipperProgressArgs args)
        {
            if (user_job == null) {
                return;
            }
        
            TimeSpan total_ripped_duration = ripped_duration + args.EncodedTime;
            user_job.Progress = total_ripped_duration.TotalMilliseconds / total_duration.TotalMilliseconds;
            
            TimeSpan poll_diff = DateTime.Now - last_speed_poll_time;
            double factor = 0;
            
            if (poll_diff.TotalMilliseconds >= 1000) {
                factor = ((total_ripped_duration - last_speed_poll_duration).TotalMilliseconds 
                    * (poll_diff.TotalMilliseconds / 1000.0)) / 1000.0;
                
                last_speed_poll_duration = total_ripped_duration;
                last_speed_poll_time = DateTime.Now;
                last_speed_poll_factor = factor > 1 ? factor : 0;
            }
            
            user_job.Status = last_speed_poll_factor > 1 ? String.Format ("{0} ({1:0.0}x)", 
                status, last_speed_poll_factor) : status;
        }
        
        private void OnError (object o, AudioCdRipperErrorArgs args)
        {
            Dispose ();
            Hyena.Log.Error (Catalog.GetString ("Cannot Import CD"), args.Message, true);
        }

#endregion

        private void OnFinished ()
        {
            EventHandler handler = Finished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
                                
#region User Job Event Handlers        
        
        private void OnCancelRequested (object o, EventArgs args)
        {
            Dispose ();
        }
        
        private void OnFinished (object o, EventArgs args)
        {
            if (user_job != null) {
                user_job.CancelRequested -= OnCancelRequested;
                user_job.Finished -= OnFinished;
                user_job = null;
            }
            
            source.UnlockAllTracks ();
        }
        
#endregion

    }
}
