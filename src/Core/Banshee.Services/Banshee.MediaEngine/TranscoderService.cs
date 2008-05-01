//
// TranscoderService.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Mono.Addins;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.MediaProfiles;

namespace Banshee.MediaEngine
{
    public class TranscoderService : IService
    {
        public delegate void TrackTranscodedHandler (TrackInfo track, SafeUri uri);
        public delegate void TranscodeCancelledHandler ();

        private struct TranscodeContext
        {
            public TrackInfo Track;
            public SafeUri OutUri;
            public ProfileConfiguration Config;
            public TrackTranscodedHandler Handler;
            public TranscodeCancelledHandler CancelledHandler;

            public TranscodeContext (TrackInfo track, SafeUri out_uri, ProfileConfiguration config, 
                TrackTranscodedHandler handler, TranscodeCancelledHandler cancelledHandler)
            {
                Track = track;
                OutUri = out_uri;
                Config = config;
                Handler = handler;
                CancelledHandler = cancelledHandler;
            }
        }

        private static bool transcoder_extension_queried = false;
        private static TypeExtensionNode transcoder_extension_node = null;
        private static TypeExtensionNode TranscoderExtensionNode {
            get { 
                if (!transcoder_extension_queried) {
                    transcoder_extension_queried = true;
                    foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (
                        "/Banshee/MediaEngine/Transcoder")) {
                        transcoder_extension_node = node;
                        break;
                    }
                }
                return transcoder_extension_node;
            }
        }
                
        public static bool Supported {
            get { return TranscoderExtensionNode != null; }
        }

        private ITranscoder transcoder;
        private BatchUserJob user_job;
        private Queue<TranscodeContext> queue;
        private TranscodeContext current_context;

        public TranscoderService ()
        {
            queue = new Queue <TranscodeContext> ();

            try {
                Banshee.IO.Directory.Delete (cache_dir, true);
            } catch {}

            Banshee.IO.Directory.Create (cache_dir);
        }

        private static string cache_dir = Paths.Combine (Paths.ApplicationCache, "transcoder");
        
        public static SafeUri GetTempUriFor (string extension)
        {
            return new SafeUri (Paths.GetTempFileName (cache_dir, extension));
        }

        private ITranscoder Transcoder {
            get {
                if (transcoder == null) {
                    if (TranscoderExtensionNode != null) {
                        transcoder = (ITranscoder) TranscoderExtensionNode.CreateInstance ();
                        transcoder.TrackFinished += OnTrackFinished;
                        transcoder.Progress += OnProgress;
                        transcoder.Error += OnError;
                    } else {
                        throw new ApplicationException ("No Transcoder extension is installed");
                    }
                }
                return transcoder;
            }
        }

        private BatchUserJob UserJob {
            get {
                if (user_job == null) {
                    user_job = new BatchUserJob (Catalog.GetString("Converting {0} of {1}"), Catalog.GetString("Initializing"), "encode");
                    user_job.CancelMessage = Catalog.GetString ("Files are currently being converted to another format. Would you like to stop this?");
                    user_job.CanCancel = true;
                    user_job.DelayShow = true;
                    user_job.CancelRequested += OnCancelRequested;
                    user_job.Finished += OnFinished;
                    user_job.Register ();
                }
                return user_job;
            }
        }
        
        private void Reset ()
        {
            lock (queue) {
                if (user_job != null) {
                    user_job.CancelRequested -= OnCancelRequested;
                    user_job.Finished -= OnFinished;
                    user_job.Finish ();
                    user_job = null;
                }
                
                if (transcoder != null) {
                    transcoder.Finish ();
                    transcoder = null;
                }

                foreach (TranscodeContext context in queue) {
                    context.CancelledHandler ();
                }

                if (transcoding) {
                    current_context.CancelledHandler ();
                    transcoding = false;
                }

                queue.Clear ();
            }
        }

        public void Enqueue (TrackInfo track, ProfileConfiguration config, 
            TrackTranscodedHandler handler, TranscodeCancelledHandler cancelledHandler)
        {
            Enqueue (track, GetTempUriFor (config.Profile.OutputFileExtension), config, handler, cancelledHandler);
        }

        public void Enqueue (TrackInfo track, SafeUri out_uri, ProfileConfiguration config, 
            TrackTranscodedHandler handler, TranscodeCancelledHandler cancelledHandler)
        {
            bool start = false;
            lock (queue) {
                start = (queue.Count == 0 && !transcoding);
                queue.Enqueue (new TranscodeContext (track, out_uri, config, handler, cancelledHandler));
                UserJob.Total++;
            }

            if (start)
                ProcessQueue ();
        }

        private bool transcoding = false;
        private void ProcessQueue ()
        {
            TranscodeContext context;
            lock (queue) {
                if (queue.Count == 0) {
                    Reset ();
                    return;
                }

                context = queue.Dequeue ();
                transcoding = true;
            }

            current_context = context;
            UserJob.Status = String.Format("{0} - {1}", context.Track.ArtistName, context.Track.TrackTitle);
            Transcoder.TranscodeTrack (context.Track, context.OutUri, context.Config);
        }

#region Transcoder Event Handlers

        private void OnTrackFinished (object o, TranscoderTrackFinishedArgs args)
        {
            transcoding = false;

            if (user_job == null || transcoder == null) {
                return;
            }

            UserJob.Completed++;
            current_context.Handler (args.Track, current_context.OutUri);

            ProcessQueue ();
        }
        
        private void OnProgress (object o, TranscoderProgressArgs args)
        {
            if (user_job == null) {
                return;
            }

            UserJob.DetailedProgress = args.Fraction;
        }
        
        private void OnError (object o, TranscoderErrorArgs args)
        {
            Reset ();
            Hyena.Log.Error (Catalog.GetString ("Cannot Convert File"), args.Message, true);
        }

#endregion
                                
#region User Job Event Handlers        
        
        private void OnCancelRequested (object o, EventArgs args)
        {
            Reset ();
        }
        
        private void OnFinished (object o, EventArgs args)
        {
            Reset ();
        }
        
#endregion

        string IService.ServiceName {
            get { return "TranscoderService"; }
        }
    }
}
