
/***************************************************************************
 *  GstPlayerEngine.cs
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
using System.Runtime.InteropServices;
using Mono.Unix;

using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.MediaEngine.Gstreamer
{    
#if GSTREAMER_0_10
    internal delegate void GstPlaybackEosCallback(IntPtr engine);
    internal delegate void GstPlaybackErrorCallback(IntPtr engine, IntPtr error, IntPtr debug);
    internal delegate void GstPlaybackStateChangedCallback(IntPtr engine, 
        int old_state, int new_state, int pending_state);
    internal delegate void GstPlaybackIterateCallback(IntPtr engine);

    public class GstPlayer : IPlayerEngine
    {
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
        private static extern void gst_playback_set_position(HandleRef engine, ulong time_ms);
        
        [DllImport("libbanshee")]
        private static extern ulong gst_playback_get_position(HandleRef engine);
        
        [DllImport("libbanshee")]
        private static extern ulong gst_playback_get_duration(HandleRef engine);
        
        private HandleRef handle;
        private TrackInfo track;
        private bool playing;
        
        public event PlayerEngineErrorHandler Error;
        public event PlayerEngineVolumeChangedHandler VolumeChanged;
        public event PlayerEngineIterateHandler Iterate;
        public event EventHandler EndOfStream;
        
        private GstPlaybackEosCallback eos_callback;
        private GstPlaybackErrorCallback error_callback;
        private GstPlaybackStateChangedCallback state_changed_callback;
        private GstPlaybackIterateCallback iterate_callback;
        
        public void Initialize()
        {
            IntPtr ptr = gst_playback_new();
            
            if(ptr == IntPtr.Zero) {
                throw new ApplicationException(Catalog.GetString("Could not initialize GStreamer library"));
            }
            
            handle = new HandleRef(this, ptr);
            
            
            eos_callback = new GstPlaybackEosCallback(OnEos);
            error_callback = new GstPlaybackErrorCallback(OnError);
            state_changed_callback = new GstPlaybackStateChangedCallback(OnStateChanged);
            iterate_callback = new GstPlaybackIterateCallback(OnIterate);
            
            gst_playback_set_eos_callback(handle, eos_callback);
            gst_playback_set_iterate_callback(handle, iterate_callback);
            gst_playback_set_error_callback(handle, error_callback);
            gst_playback_set_state_changed_callback(handle, state_changed_callback);
            
            playing = false;
        }
        
        public void TestInitialize()
        {
            Initialize();
            Dispose();
        }
        
        public void Dispose()
        {
            gst_playback_free(handle);
        }
        
        public bool Open(TrackInfo track, Uri uri)
        {
            if(!track.CanPlay) {
                InvokeEndOfStream();
                return false;
            }
            
            this.track = track;
            
            IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup(uri.AbsoluteUri);
            gst_playback_open(handle, uri_ptr);
            GLib.Marshaller.Free(uri_ptr);
            
            return true;
        }
        
        public void Close()
        {
            track = null;
            playing = false;
            gst_playback_stop(handle);
        }
        
        public void Play()
        {
            playing = true;
            gst_playback_play(handle);
        }
        
        public void Pause()
        {
            playing = false;
            gst_playback_pause(handle);
        }
        
        public bool Loaded {
            get {
                return track != null;
            }
        }
        
        public bool Playing {
            get {
                return playing;
            }
        }
        
        public ushort Volume {
            get {
                return (ushort)gst_playback_get_volume(handle);
            }
            
            set {
                gst_playback_set_volume(handle, (int)value);
            }
        }
        
        public uint Position {
            get {
                return (uint)gst_playback_get_position(handle) / 1000;
            }
            
            set {
                gst_playback_set_position(handle, (ulong)value * 1000);
            }
        }
        
        public uint Length {
            get {
                return (uint)gst_playback_get_duration(handle) / 1000;
            }
        }
        
        public TrackInfo Track {
            get {
                return track;
            }
        }
        
        private void OnEos(IntPtr engine)
        {
            InvokeEndOfStream();
        }
        
        private void OnIterate(IntPtr engine)
        {
            InvokeIterate(Position);
        }
        
        private void OnError(IntPtr engine, IntPtr error, IntPtr debug)
        {
            Close();
            
            string error_message = error == IntPtr.Zero
                ? Catalog.GetString("Unknown Error")
                : GLib.Marshaller.Utf8PtrToString(error);
                
            if(debug != IntPtr.Zero) {
                Console.Error.WriteLine("GST-DEBUG: {0}: {1}", error_message, 
                    GLib.Marshaller.Utf8PtrToString(debug));
            } 
            
            InvokeError(error_message);
        }
        
        private void OnStateChanged(IntPtr engine, int new_state, int old_state, int pending_state)
        {
        }
        
        private void InvokeError(string error)
        {
            PlayerEngineErrorHandler handler = Error;
            if(handler != null) {
                PlayerEngineErrorArgs args = new PlayerEngineErrorArgs();
                args.Error = error;
                handler(this, args);
            }
        }
            
        private void InvokeVolumeChanged(int volume)
        {
            PlayerEngineVolumeChangedHandler handler = VolumeChanged;
            if(handler != null) {
                PlayerEngineVolumeChangedArgs args = new PlayerEngineVolumeChangedArgs();
                args.Volume = (ushort)volume;
                handler(this, args);
            }
        } 
        
        private void InvokeIterate(ulong position)
        {
            PlayerEngineIterateHandler handler = Iterate;
            if(handler != null) {
                PlayerEngineIterateArgs args = new PlayerEngineIterateArgs();
                args.Position = (uint)position;
                handler(this, args);
            }
        }
        
        private void InvokeEndOfStream()
        {
            Close();
            
            EventHandler handler = EndOfStream;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public string [] SupportedExtensions { get { return null; } }

        public string ConfigName     { get { return "gstreamer"; } }
        public string EngineName     { get { return "GStreamer 0.10"; } }
        public string EngineLongName { get { return Catalog.GetString("GStreamer 0.10 Engine"); } }
        public int MajorVersion      { get { return 0; } }
        public int MinorVersion      { get { return 1; } }
        
        public string AuthorName     { get { return "Aaron Bockover"; } }
        public string AuthorEmail    { get { return "aaron@aaronbock.net"; } }
    
        public string EngineDetails {
            get {
                return Catalog.GetString(
                    "GStreamer is a multimedia framework for playing and " +
                    "manipulating media. Any GStreamer plugin " +
                    "that is available will work through this engine.");
            }
        } 
    }
#else
    internal delegate void GpeErrorCallback(IntPtr engine, IntPtr error);
    internal delegate void GpeIterateCallback(IntPtr engine, int position,
        int length);
    internal delegate void GpeEndOfStreamCallback(IntPtr engine);

    public class GstPlayer : IPlayerEngine
    {
        [DllImport("libbanshee")]
        private static extern IntPtr gpe_new();
        
        [DllImport("libbanshee")]
        private static extern void gpe_free(HandleRef handle);
        
        /*[DllImport("libgstmediaengine")]
        private static extern void gpe_set_end_of_stream_handler(
            HandleRef handle, GpeEndOfStreamCallback cb);
            
        [DllImport("libgstmediaengine")]
        private static extern void gpe_set_error_handler(
            HandleRef handle, GpeErrorCallback cb);
            
        [DllImport("libgstmediaengine")]
        private static extern void gpe_set_iterate_handler(
            HandleRef handle, GpeIterateCallback cb);*/

        [DllImport("libbanshee")]
        private static extern bool gpe_open(HandleRef handle, string file);
        
        [DllImport("libbanshee")]
        private static extern void gpe_play(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern void gpe_pause(HandleRef handle);
    
        [DllImport("libbanshee")]
        private static extern void gpe_stop(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern void gpe_set_volume(HandleRef handle,
            int volume);
        
        [DllImport("libbanshee")]
        private static extern int gpe_get_volume(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern void gpe_set_position(HandleRef handle,
            int position);
            
        [DllImport("libbanshee")]
        private static extern int gpe_get_position(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern int gpe_get_length(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern bool gpe_is_eos(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern IntPtr gpe_get_error(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern bool gpe_have_error(HandleRef handle);
        
        public event PlayerEngineErrorHandler Error;
        public event PlayerEngineVolumeChangedHandler VolumeChanged;
        public event PlayerEngineIterateHandler Iterate;
        public event EventHandler EndOfStream;
        
        private HandleRef handle;
        private bool loaded;
        private bool playing;
        
        private TrackInfo track;
        
        private bool finalized;
        private bool timeoutCancelRequest;
        
        public void Initialize()
        {
            IntPtr ptr = gpe_new();
            handle = new HandleRef(this, ptr);
            //gpe_set_end_of_stream_handler(handle, OnEndOfStream);
            //gpe_set_error_handler(handle, OnError);
            //gpe_set_iterate_handler(handle, OnIterate);
            
            timeoutCancelRequest = false;
            finalized = false;
            
            GLib.Timeout.Add(250, OnTimeout);
        }
        
        public void TestInitialize()
        {
            IntPtr ptr = gpe_new();
            if (ptr == IntPtr.Zero)
                throw new ApplicationException(Catalog.GetString("Could not initialize GStreamer library"));
            handle = new HandleRef(this, ptr);
            Dispose();
        }
            
        public void Dispose()
        {
            if(!finalized) {
                finalized = true;
                timeoutCancelRequest = true;
                Close();
                gpe_free(handle);
            }
        }
            
        public bool Open(TrackInfo ti, Uri uri)
        {
            if(loaded || playing)
                Close();
            
            if(!ti.CanPlay) {
                EmitEndOfStream();
                return false;
            }
            
            loaded = gpe_open(handle, uri.AbsoluteUri);
            
            if(loaded)
                track = ti;
            else
                track = null;
            
            return loaded;
        }
        
        public void Close()
        {
            gpe_stop(handle);
            loaded = false;
            playing = false;
        }
            
        public void Play()
        {
            gpe_play(handle);
            playing = true;
        }
        
        public void Pause()
        {
                gpe_pause(handle);
                playing = false;
            }
               
           public bool Playing 
           { 
               get {
                   return playing;
               } 
           }
           
           public bool HasFile 
           { 
               get {
                   return loaded;
               } 
           }
           
           public bool Loaded
           {
               get {
                   return loaded;
               }
           }
           
           public uint Position 
           { 
               get { 
                   return (uint)gpe_get_position(handle);
               } 
               
               set {
                   gpe_set_position(handle, (int)value);            
               }
           }
           
           public uint Length
           {
               get {
                   return (uint)gpe_get_length(handle);
               }
           }
           
        public ushort Volume 
        {
            get { 
                return (ushort)gpe_get_volume(handle);
            }
            
            set { 
                gpe_set_volume(handle, (int)value);
                
                if(VolumeChanged != null) {
                    PlayerEngineVolumeChangedArgs args = 
                        new PlayerEngineVolumeChangedArgs();
                    args.Volume = value;
                    EmitVolumeChanged(args);
                }
            }
        }
        
            public TrackInfo Track
        {
            get {
                return track;
            }
        }

        private bool OnTimeout()
        {
            if(timeoutCancelRequest) 
                return false;
                
            if(!loaded)
                return true;
                
            if(gpe_have_error(handle)) {
                Close();
                IntPtr errorPtr = gpe_get_error(handle);
                PlayerEngineErrorArgs errargs = new PlayerEngineErrorArgs();
                errargs.Error = Marshal.PtrToStringAnsi(errorPtr);
                EmitError(errargs);
                EmitEndOfStream();
                playing = false;
                return true;
            }
            
            PlayerEngineIterateArgs iterargs = new PlayerEngineIterateArgs();
            iterargs.Position = Position;
            EmitIterate(iterargs);
            
            if(gpe_is_eos(handle) && playing) {
                playing = false;
                EmitEndOfStream();
            }
            
            return true;
        }

        /*private void OnEndOfStream(IntPtr engine)
        {
            EmitEndOfStream();
        }
        
        private void OnError(IntPtr engine, IntPtr messagePtr)
        {
            PlayerEngineErrorArgs args = new PlayerEngineErrorArgs();
            args.Error = Marshal.PtrToStringAnsi(messagePtr);
            EmitError(args);
        }
        
        private void OnIterate(IntPtr engine, int position, int total)
        {
            PlayerEngineIterateArgs args = new PlayerEngineIterateArgs();
            args.Position = position;
            EmitIterate(args);
        }*/
            
        private void EmitError(PlayerEngineErrorArgs args)
        {
            PlayerEngineErrorHandler handler = Error;
            if(handler != null)
                handler(this, args);
        }
            
        private void EmitVolumeChanged(PlayerEngineVolumeChangedArgs args)
        {
            PlayerEngineVolumeChangedHandler handler = VolumeChanged;
            if(handler != null)
                handler(this, args);
        } 
        
        private void EmitIterate(PlayerEngineIterateArgs args)
        {
            PlayerEngineIterateHandler handler = Iterate;
            if(handler != null)
                handler(this, args);
        }
        
        private void EmitEndOfStream()
        {
            EventHandler handler = EndOfStream;
            if(handler != null)
                handler(this, new EventArgs());
        }

        private static string [] supported_extensions = {
            "wma"
        };

        public string [] SupportedExtensions {
            get {
                return supported_extensions;
            }
        } 

        public string ConfigName     { get { return "gstreamer"; } }
        public string EngineName     { get { return "GStreamer"; } }
        public string EngineLongName { get { return Catalog.GetString("GStreamer Engine"); } }
        public int MajorVersion      { get { return 0; } }
        public int MinorVersion      { get { return 1; } }
        
        public string AuthorName     { get { return "Aaron Bockover"; } }
        public string AuthorEmail    { get { return "aaron@aaronbock.net"; } }
    
        public string EngineDetails
        {
            get {
                return Catalog.GetString(
                    "GStreamer is a multimedia framework for playing and " +
                    "manipulating media. Any GStreamer plugin " +
                    "that is available will work through this engine.");
            }
        }    
    }
#endif
}
