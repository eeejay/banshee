/***************************************************************************
 *  GstPlayerEngine.cs
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
using System.Collections;
using System.Runtime.InteropServices;
using Mono.Unix;

using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.Gstreamer;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.MediaEngine.Gstreamer.GstreamerPlayerEngine)
        };
    }
}

namespace Banshee.MediaEngine.Gstreamer
{
    internal delegate void GstPlaybackEosCallback(IntPtr engine);
    internal delegate void GstPlaybackErrorCallback(IntPtr engine, uint domain, int code, IntPtr error, IntPtr debug);
    internal delegate void GstPlaybackStateChangedCallback(IntPtr engine, int old_state, int new_state, int pending_state);
    internal delegate void GstPlaybackIterateCallback(IntPtr engine);
    internal delegate void GstPlaybackBufferingCallback(IntPtr engine, int buffering_progress);

    internal enum GstCoreError {
        Failed = 1,
        TooLazy,
        NotImplemented,
        StateChange,
        Pad,
        Thread,
        Negotiation,
        Event,
        Seek,
        Caps,
        Tag,
        MissingPlugin,
        Clock,
        NumErrors
    }
    
    internal enum GstLibraryError {
        Failed = 1,
        Init,
        Shutdown,
        Settings,
        Encode,
        NumErrors
    }
    
    internal enum GstResourceError {
        Failed = 1,
        TooLazy,
        NotFound,
        Busy,
        OpenRead,
        OpenWrite,
        OpenReadWrite,
        Close,
        Read,
        Write,
        Seek,
        Sync,
        Settings,
        NoSpaceLeft,
        NumErrors
    }
    
    internal enum GstStreamError {
        Failed = 1,
        TooLazy,
        NotImplemented,
        TypeNotFound,
        WrongType,
        CodecNotFound,
        Decode,
        Encode,
        Demux,
        Mux,
        Format,
        NumErrors
    }
    
    public class GstreamerPlayerEngine : PlayerEngine
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
        
        public GstreamerPlayerEngine()
        {
            IntPtr ptr = gst_playback_new();
            
            if(ptr == IntPtr.Zero) {
                throw new ApplicationException(Catalog.GetString("Could not initialize GStreamer library"));
            }
            
            handle = new HandleRef(this, ptr);
            
            gst_playback_get_error_quarks(out GST_CORE_ERROR, out GST_LIBRARY_ERROR, 
                out GST_RESOURCE_ERROR, out GST_STREAM_ERROR);
            
            eos_callback = new GstPlaybackEosCallback(OnEos);
            error_callback = new GstPlaybackErrorCallback(OnError);
            state_changed_callback = new GstPlaybackStateChangedCallback(OnStateChanged);
            iterate_callback = new GstPlaybackIterateCallback(OnIterate);
            buffering_callback = new GstPlaybackBufferingCallback(OnBuffering);
            tag_found_callback = new GstTaggerTagFoundCallback(OnTagFound);
            
            gst_playback_set_eos_callback(handle, eos_callback);
            gst_playback_set_iterate_callback(handle, iterate_callback);
            gst_playback_set_error_callback(handle, error_callback);
            gst_playback_set_state_changed_callback(handle, state_changed_callback);
            gst_playback_set_buffering_callback(handle, buffering_callback);
            gst_playback_set_tag_found_callback(handle, tag_found_callback);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            gst_playback_free(handle);
        }
        
        public override void Close()
        {
            gst_playback_stop(handle);
            base.Close();
        }
        
        protected override void OpenUri(SafeUri uri)
        {
            IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup(uri.AbsoluteUri);
            gst_playback_open(handle, uri_ptr);
            GLib.Marshaller.Free(uri_ptr);
        }
        
        public override void Play()
        {
            gst_playback_play(handle);
            OnStateChanged(PlayerEngineState.Playing);
        }
        
        public override void Pause()
        {
            gst_playback_pause(handle);
            OnStateChanged(PlayerEngineState.Paused);
        }

        public override IntPtr [] GetBaseElements()
        {
            IntPtr [] elements = new IntPtr[3];
            
            if(gst_playback_get_pipeline_elements(handle, out elements[0], out elements[1], out elements[2])) {
                return elements;
            }
            
            return null;
        }

        private void OnEos(IntPtr engine)
        {
            Close();
            OnEventChanged(PlayerEngineEvent.EndOfStream);
        }
        
        private void OnIterate(IntPtr engine)
        {
            OnEventChanged(PlayerEngineEvent.Iterate);
        }
        
        private void OnError(IntPtr engine, uint domain, int code, IntPtr error, IntPtr debug)
        {
            Close();
            
            string error_message = error == IntPtr.Zero
                ? Catalog.GetString("Unknown Error")
                : GLib.Marshaller.Utf8PtrToString(error);

            if(domain == GST_RESOURCE_ERROR) {
                GstResourceError domain_code = (GstResourceError)code;
                switch(domain_code) {
                    case GstResourceError.NotFound:
                        CurrentTrack.PlaybackError = TrackPlaybackError.ResourceNotFound;
                        break;
                    default:
                        break;
                }        
                
                Console.WriteLine("GStreamer resource error: {0}", domain_code);
            } else if(domain == GST_STREAM_ERROR) {
                GstStreamError domain_code = (GstStreamError)code;
                switch(domain_code) {
                    case GstStreamError.CodecNotFound:
                        CurrentTrack.PlaybackError = TrackPlaybackError.CodecNotFound;
                        break;
                    default:
                        break;
                }
                
                Console.WriteLine("GStreamer stream error: {0}", domain_code);
            } else if(domain == GST_CORE_ERROR) {
                Console.WriteLine("GStreamer core error: {0}", (GstCoreError)code);
            } else if(domain == GST_LIBRARY_ERROR) {
                Console.WriteLine("GStreamer library error: {0}", (GstLibraryError)code);
            }
            
            OnEventChanged(PlayerEngineEvent.Error, error_message);
        }
        
        private void OnStateChanged(IntPtr engine, int new_state, int old_state, int pending_state)
        {
        }
        
        private void OnBuffering(IntPtr engine, int progress)
        {
            if(buffering_finished && progress >= 100) {
                return;
            }
            
            buffering_finished = progress >= 100;
            OnEventChanged(PlayerEngineEvent.Buffering, Catalog.GetString("Buffering"), (double)progress / 100.0);
        }
        
        private void OnTagFound(string tagName, ref GLib.Value value, IntPtr userData)
        {
            OnTagFound(GstTagger.ProcessNativeTagResult(tagName, ref value));
        }
        
        public override ushort Volume {
            get { return (ushort)gst_playback_get_volume(handle); }
            set { 
                gst_playback_set_volume(handle, (int)value);
                OnEventChanged(PlayerEngineEvent.Volume);
            }
        }
        
        public override uint Position {
            get { return (uint)gst_playback_get_position(handle) / 1000; }
            set { 
                gst_playback_set_position(handle, (ulong)value * 1000);
                OnEventChanged(PlayerEngineEvent.Seek);
            }
        }
        
        public override bool CanSeek {
            get { return gst_playback_can_seek(handle); }
        }
        
        public override uint Length {
            get { return (uint)gst_playback_get_duration(handle) / 1000; }
        }
        
        public override string Id {
            get { return "gstreamer"; }
        }
        
        public override string Name {
            get { return "GStreamer 0.10"; }
        }
        
        private static string [] source_capabilities = { "file", "http", "cdda" };
        public override IEnumerable SourceCapabilities {
            get { return source_capabilities; }
        }
                
        private static string [] decoder_capabilities = { "ogg", "wma", "asf", "flac" };
        public override IEnumerable ExplicitDecoderCapabilities {
            get { return decoder_capabilities; }
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr gst_playback_new();
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_free(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_eos_callback(HandleRef engine, GstPlaybackEosCallback cb);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_error_callback(HandleRef engine, GstPlaybackErrorCallback cb);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_state_changed_callback(HandleRef engine, 
            GstPlaybackStateChangedCallback cb);
            
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_iterate_callback(HandleRef engine, GstPlaybackIterateCallback cb);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_buffering_callback(HandleRef engine, GstPlaybackBufferingCallback cb);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_tag_found_callback(HandleRef engine, GstTaggerTagFoundCallback cb);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_open(HandleRef engine, IntPtr uri);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_stop(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_pause(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_play(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_volume(HandleRef engine, int volume);
        
        [DllImport("libbanshee")]
        private static extern int gst_playback_get_volume(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern bool gst_playback_can_seek(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern void gst_playback_set_position(HandleRef engine, ulong time_ms);
        
        [DllImport("libbanshee")]
        private static extern ulong gst_playback_get_position(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern ulong gst_playback_get_duration(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern bool gst_playback_get_pipeline_elements(HandleRef engine, out IntPtr playbin,
            out IntPtr audiobin, out IntPtr audiotee);
            
        [DllImport("libbanshee")]
        private static extern void gst_playback_get_error_quarks(out uint core, out uint library, 
            out uint resource, out uint stream);
    }
}
