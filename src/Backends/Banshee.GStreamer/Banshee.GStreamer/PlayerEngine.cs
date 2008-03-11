// 
// PlayerEngine.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Collections;
using System.Runtime.InteropServices;
using Mono.Unix;
using Hyena;
using Hyena.Data;

using Banshee.Base;
using Banshee.Streaming;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.GStreamer
{
    internal delegate void GstPlaybackEosCallback (IntPtr engine);
    internal delegate void GstPlaybackErrorCallback (IntPtr engine, uint domain, int code, IntPtr error, IntPtr debug);
    internal delegate void GstPlaybackStateChangedCallback (IntPtr engine, int old_state, int new_state, int pending_state);
    internal delegate void GstPlaybackIterateCallback (IntPtr engine);
    internal delegate void GstPlaybackBufferingCallback (IntPtr engine, int buffering_progress);

    internal delegate void GstTaggerTagFoundCallback (string tagName, ref GLib.Value value, IntPtr userData);        
    
    public class PlayerEngine : Banshee.MediaEngine.PlayerEngine, Banshee.MediaEngine.IEqualizer
    {
        private uint GST_CORE_ERROR = 0;
        private uint GST_LIBRARY_ERROR = 0;
        private uint GST_RESOURCE_ERROR = 0;
        private uint GST_STREAM_ERROR = 0; 
    
        private HandleRef handle;
        
        private GstPlaybackEosCallback eos_callback;
        private GstPlaybackErrorCallback error_callback;
        private GstPlaybackStateChangedCallback state_changed_callback;
        private GstPlaybackIterateCallback iterate_callback;
        private GstPlaybackBufferingCallback buffering_callback;
        private GstTaggerTagFoundCallback tag_found_callback;
        
        private bool buffering_finished;
        private short pending_volume = -1;
        
        public PlayerEngine ()
        {
            if (ServiceManager.IsInitialized) {
                Initialize ();
            } else {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }
        
        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Service) {
                ServiceManager.ServiceStarted -= OnServiceStarted;
                Initialize ();
            }
        }
        
        private void Initialize ()
        {
            IntPtr ptr = gst_playback_new ();
            
            if (ptr == IntPtr.Zero) {
                throw new ApplicationException (Catalog.GetString ("Could not initialize GStreamer library"));
            }
            
            handle = new HandleRef (this, ptr);
            
            gst_playback_get_error_quarks (out GST_CORE_ERROR, out GST_LIBRARY_ERROR, 
                out GST_RESOURCE_ERROR, out GST_STREAM_ERROR);
            
            eos_callback = new GstPlaybackEosCallback (OnEos);
            error_callback = new GstPlaybackErrorCallback (OnError);
            iterate_callback = new GstPlaybackIterateCallback (OnIterate);
            buffering_callback = new GstPlaybackBufferingCallback (OnBuffering);
            tag_found_callback = new GstTaggerTagFoundCallback (OnTagFound);
            
            gst_playback_set_eos_callback (handle, eos_callback);
            gst_playback_set_iterate_callback (handle, iterate_callback);
            gst_playback_set_error_callback (handle, error_callback);
            gst_playback_set_state_changed_callback (handle, state_changed_callback);
            gst_playback_set_buffering_callback (handle, buffering_callback);
            gst_playback_set_tag_found_callback (handle, tag_found_callback);
            
            OnStateChanged (PlayerEngineState.Ready);
            
            if (pending_volume >= 0) {
                Volume = (ushort)pending_volume;
            }
        }
        
        public override void Dispose ()
        {
            base.Dispose ();
            gst_playback_free (handle);
        }
        
        public override void Close ()
        {
            gst_playback_stop (handle);
            base.Close ();
        }
        
        protected override void OpenUri (SafeUri uri)
        {
            // The GStreamer engine can use the XID of the main window if it ever
            // needs to bring up the plugin installer so it can be transient to
            // the main window.

            IPropertyStoreExpose service = ServiceManager.Get<IService> ("GtkElementsService") as IPropertyStoreExpose;
            if (service != null) {
                gst_playback_set_gdk_window (handle, service.PropertyStore.Get<IntPtr> ("PrimaryWindow.RawHandle"));
            }
                
            IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup (uri.AbsoluteUri);
            gst_playback_open (handle, uri_ptr);
            GLib.Marshaller.Free (uri_ptr);
        }
        
        public override void Play ()
        {
            gst_playback_play (handle);
            OnStateChanged (PlayerEngineState.Playing);
        }
        
        public override void Pause ()
        {
            gst_playback_pause (handle);
            OnStateChanged (PlayerEngineState.Paused);
        }

        public override IntPtr [] GetBaseElements ()
        {
            IntPtr [] elements = new IntPtr[3];
            
            if (gst_playback_get_pipeline_elements (handle, out elements[0], out elements[1], out elements[2])) {
                return elements;
            }
            
            return null;
        }

        private void OnEos (IntPtr engine)
        {
            Close ();
            OnEventChanged (PlayerEngineEvent.EndOfStream);
        }
        
        private void OnIterate (IntPtr engine)
        {
            OnEventChanged (PlayerEngineEvent.Iterate);
        }
        
        private void OnError (IntPtr engine, uint domain, int code, IntPtr error, IntPtr debug)
        {
            Close ();
            
            string error_message = error == IntPtr.Zero
                ? Catalog.GetString ("Unknown Error")
                : GLib.Marshaller.Utf8PtrToString (error);

            if (domain == GST_RESOURCE_ERROR) {
                GstResourceError domain_code = (GstResourceError) code;
                if (CurrentTrack != null) {
                    switch (domain_code) {
                        case GstResourceError.NotFound:
                            CurrentTrack.PlaybackError = StreamPlaybackError.ResourceNotFound;
                            break;
                        default:
                            break;
                    }        
                }
                
                Log.Error (String.Format ("GStreamer resource error: {0}", domain_code), false);
            } else if (domain == GST_STREAM_ERROR) {
                GstStreamError domain_code = (GstStreamError) code;
                if (CurrentTrack != null) {
                    switch (domain_code) {
                        case GstStreamError.CodecNotFound:
                            CurrentTrack.PlaybackError = StreamPlaybackError.CodecNotFound;
                            break;
                        default:
                            break;
                    }
                }
                
                Log.Error (String.Format("GStreamer stream error: {0}", domain_code), false);
            } else if (domain == GST_CORE_ERROR) {
                GstCoreError domain_code = (GstCoreError) code;
                if (CurrentTrack != null) {
                    switch (domain_code) {
                        case GstCoreError.MissingPlugin:
                            CurrentTrack.PlaybackError = StreamPlaybackError.CodecNotFound;
                            break;
                        default:
                            break;
                    }
                }
                
                if (domain_code != GstCoreError.MissingPlugin) {
                    Log.Error (String.Format("GStreamer core error: {0}", (GstCoreError) code), false);
                }
            } else if (domain == GST_LIBRARY_ERROR) {
                Log.Error (String.Format("GStreamer library error: {0}", (GstLibraryError) code), false);
            }
            
            OnEventChanged (PlayerEngineEvent.Error, error_message);
        }
        
        private void OnBuffering (IntPtr engine, int progress)
        {
            if (buffering_finished && progress >= 100) {
                return;
            }
            
            buffering_finished = progress >= 100;
            OnEventChanged (PlayerEngineEvent.Buffering, Catalog.GetString ("Buffering"), (double) progress / 100.0);
        }
        
        private void OnTagFound (string tagName, ref GLib.Value value, IntPtr userData)
        {
            OnTagFound(ProcessNativeTagResult (tagName, ref value));
        }
            
        private static StreamTag ProcessNativeTagResult (string tagName, ref GLib.Value valueRaw)
        {
            if (tagName == String.Empty || tagName == null) {
                return StreamTag.Zero;
            }
        
            object value = null;
            
            try {
                value = valueRaw.Val;
            } catch {
                return StreamTag.Zero;
            }
            
            if (value == null) {
                return StreamTag.Zero;
            }
            
            StreamTag item;
            item.Name = tagName;
            item.Value = value;
            
            return item;
        }
        
        public override ushort Volume {
            get { return (ushort)gst_playback_get_volume (handle); }
            set { 
                if ((IntPtr)handle == IntPtr.Zero) {
                    pending_volume = (short)value;
                    return;
                }
                
                gst_playback_set_volume (handle, (int)value);
                OnEventChanged (PlayerEngineEvent.Volume);
            }
        }
        
        public override uint Position {
            get { return (uint)gst_playback_get_position(handle); }
            set { 
                gst_playback_set_position(handle, (ulong)value);
                OnEventChanged (PlayerEngineEvent.Seek);
            }
        }
        
        public override bool CanSeek {
            get { return gst_playback_can_seek (handle); }
        }
        
        public override uint Length {
            get { return (uint)gst_playback_get_duration (handle); }
        }
        
        public override string Id {
            get { return "gstreamer"; }
        }
        
        public override string Name {
            get { return "GStreamer 0.10"; }
        }
        
        public override bool SupportsEqualizer {
            get { return gst_equalizer_is_supported (handle); }
        }
    
        public double AmplifierLevel {
            set { gst_equalizer_set_preamp_level (handle, value); }
        }
        
        public int [] BandRange {
            get {
                int min = -1;
                int max = -1;
                
                gst_equalizer_get_bandrange (handle, out min, out max);
                
                return new int [] { min, max };
            }
        }
        
        public uint [] EqualizerFrequencies {
            get {
                double [] freq = new double[10];
                gst_equalizer_get_frequencies (handle, out freq);
                
                uint [] ret = new uint[freq.Length];
                for (int i = 0; i < freq.Length; i++) {
                    ret[i] = (uint)freq[i];
                }
                
                return ret;
            }
        }
        
        public void SetEqualizerGain (uint band, double gain)
        {
            gst_equalizer_set_gain (handle, band, gain);
        }
        
        private static string [] source_capabilities = { "file", "http", "cdda" };
        public override IEnumerable SourceCapabilities {
            get { return source_capabilities; }
        }
                
        private static string [] decoder_capabilities = { "ogg", "wma", "asf", "flac" };
        public override IEnumerable ExplicitDecoderCapabilities {
            get { return decoder_capabilities; }
        }
        
        [DllImport ("libbanshee")]
        private static extern IntPtr gst_playback_new ();
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_free (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_eos_callback (HandleRef engine, GstPlaybackEosCallback cb);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_error_callback (HandleRef engine, GstPlaybackErrorCallback cb);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_state_changed_callback (HandleRef engine, 
            GstPlaybackStateChangedCallback cb);
            
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_iterate_callback (HandleRef engine,
            GstPlaybackIterateCallback cb);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_buffering_callback (HandleRef engine,
            GstPlaybackBufferingCallback cb);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_tag_found_callback (HandleRef engine,
            GstTaggerTagFoundCallback cb);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_open (HandleRef engine, IntPtr uri);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_stop (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_pause (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_play (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_volume (HandleRef engine, int volume);
        
        [DllImport("libbanshee")]
        private static extern int gst_playback_get_volume (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern bool gst_playback_can_seek (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_position (HandleRef engine, ulong time_ms);
        
        [DllImport ("libbanshee")]
        private static extern ulong gst_playback_get_position (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern ulong gst_playback_get_duration (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern bool gst_playback_get_pipeline_elements (HandleRef engine, out IntPtr playbin,
            out IntPtr audiobin, out IntPtr audiotee);
            
        [DllImport ("libbanshee")]
        private static extern void gst_playback_set_gdk_window (HandleRef engine, IntPtr window);
                                                                   
        [DllImport ("libbanshee")]
        private static extern void gst_playback_get_error_quarks (out uint core, out uint library, 
            out uint resource, out uint stream);
        
        [DllImport ("libbanshee")]
        private static extern bool gst_equalizer_is_supported (HandleRef engine);
        
        [DllImport ("libbanshee")]
        private static extern void gst_equalizer_set_preamp_level (HandleRef engine, double level);
        
        [DllImport ("libbanshee")]
        private static extern void gst_equalizer_set_gain (HandleRef engine, uint bandnum, double gain);
        
        [DllImport ("libbanshee")]
        private static extern void gst_equalizer_get_bandrange (HandleRef engine, out int min, out int max);
        
        [DllImport ("libbanshee")]
        private static extern void gst_equalizer_get_frequencies (HandleRef engine,
            [MarshalAs (UnmanagedType.LPArray)] out double [] freq);
    }
}
