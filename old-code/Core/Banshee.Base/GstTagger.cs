/***************************************************************************
 *  GstTagger.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Unix;

using Banshee.Base;
using Banshee.Collection;

namespace Banshee.Gstreamer
{
    public delegate void GstTaggerTagFoundCallback(string tagName, ref GLib.Value value, IntPtr userData);
    
    public class GstTagger : IDisposable
    {
        private delegate void GstTaggerErrorCallback(IntPtr tagger, IntPtr error, IntPtr debug);
        private delegate void GstTaggerFinishedCallback(IntPtr tagger);
    
        private HandleRef handle;
        
        private GstTaggerTagFoundCallback tag_found_callback;
        private GstTaggerErrorCallback error_callback;
        private GstTaggerFinishedCallback finished_callback;
        
        private string error_message;
        private string error_debug;
        
        private List<StreamTag> tags = new List<StreamTag>();
        
        public GstTagger()
        {
            Banshee.Gstreamer.Utilities.Initialize();
            Gnome.Vfs.Vfs.Initialize();
        
            handle = new HandleRef(this, gst_tagger_new());
            
            tag_found_callback = new GstTaggerTagFoundCallback(OnTagFound);
            error_callback = new GstTaggerErrorCallback(OnError);
            finished_callback = new GstTaggerFinishedCallback(OnFinished);
            
            gst_tagger_set_tag_found_callback(handle, tag_found_callback);
            gst_tagger_set_error_callback(handle, error_callback);
            gst_tagger_set_finished_callback(handle, finished_callback);
        }
        
        public bool ProcessTrack(TrackInfo track)
        {
            if(!ProcessUri(track.Uri)) {
                return false;
            }
            
            foreach(StreamTag tag in tags) {
                StreamTagger.TrackInfoMerge(track, tag);
            }
            
            return true;
        }
        
        public bool ProcessUri(SafeUri uri)
        {
            IntPtr uri_ptr = GLib.Marshaller.StringToPtrGStrdup(uri.AbsoluteUri);
            
            tags.Clear();
            error_message = null;
            error_debug = null;
                
            try {
                if(!gst_tagger_process_uri_and_block(handle, uri_ptr)) {
                    if(error_message != null) {
                        throw new ApplicationException(error_message);
                    }
                    
                    return false;
                }
                
                return true;
            } finally {
                GLib.Marshaller.Free(uri_ptr);
            }
        }
        
        public void Dispose()
        {
            gst_tagger_free(handle);
        }
        
        protected void OnTagFound(string tagName, ref GLib.Value value, IntPtr userData)
        {
            tags.Add(ProcessNativeTagResult(tagName, ref value));
        }
    
        protected void OnError(IntPtr tagger, IntPtr error, IntPtr debug)
        {
            error_message = GLib.Marshaller.Utf8PtrToString(error);
            error_debug = GLib.Marshaller.Utf8PtrToString(debug);
        }
        
        protected void OnFinished(IntPtr tagger)
        {
        }
        
        public IEnumerable<StreamTag> Tags {
            get { return tags; }
        }
        
        public string ErrorMessage {
            get { return error_message; }
        }
        
        public string ErrorDebug {
            get { return error_debug; }
        }
        
        public static StreamTag ProcessNativeTagResult(string tagName, ref GLib.Value valueRaw)
        {
            if(tagName == String.Empty || tagName == null) {
                return StreamTag.Zero;
            }
        
            object value = null;
            
            try {
                value = valueRaw.Val;
            } catch {
                return StreamTag.Zero;
            }
            
            if(value == null) {
                return StreamTag.Zero;
            }
            
            StreamTag item;
            item.Name = tagName;
            item.Value = value;
            
            return item;
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr gst_tagger_new();
        
        [DllImport("libbanshee")]
        private static extern void gst_tagger_free(HandleRef tagger);
        
        [DllImport("libbanshee")]
        private static extern bool gst_tagger_process_uri_and_block(HandleRef tagger, IntPtr uri);
        
        [DllImport("libbanshee")]
        private static extern void gst_tagger_set_tag_found_callback(HandleRef tagger, 
            GstTaggerTagFoundCallback callback);
        
        [DllImport("libbanshee")]
        private static extern void gst_tagger_set_error_callback(HandleRef tagger, 
            GstTaggerErrorCallback callback);
        
        [DllImport("libbanshee")]
        private static extern void gst_tagger_set_finished_callback(HandleRef tagger, 
            GstTaggerFinishedCallback callback);
    }
}
