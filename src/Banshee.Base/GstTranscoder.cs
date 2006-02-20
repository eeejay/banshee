/***************************************************************************
 *  GstTranscoder.cs
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
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Banshee.Base
{    
    internal delegate void GstTranscoderProgressCallback(IntPtr transcoder, double progress);

    public class GstTranscoder : Transcoder
    {
        [DllImport("libbanshee")]
        private static extern IntPtr gst_transcoder_new();
        
        [DllImport("libbanshee")]
        private static extern void gst_transcoder_free(HandleRef transcoder);
        
        [DllImport("libbanshee")]
        private static extern bool gst_transcoder_transcode(HandleRef encoder, IntPtr input_uri, 
            IntPtr output_uri, string encode_pipeline, GstTranscoderProgressCallback progress_cb);
    
        [DllImport("libbanshee")]
        private static extern IntPtr gst_transcoder_get_error(HandleRef transcoder);
        
        [DllImport("libbanshee")]
        private static extern void gst_transcoder_cancel(HandleRef transcoder);
        
        private HandleRef handle;
        private GstTranscoderProgressCallback ProgressCallback;
        
        public GstTranscoder()
        {
            IntPtr ptr = gst_transcoder_new();
            
            if(ptr == IntPtr.Zero) {
                throw new NullReferenceException(Catalog.GetString("Could not create transcoder"));
            }
            
            ProgressCallback = new GstTranscoderProgressCallback(OnTranscoderProgress);
            handle = new HandleRef(this, ptr);
        }
        
        public override void Dispose()
        {
            gst_transcoder_free(handle);
        }
        
        public override Uri Transcode(Uri inputUri, Uri outputUri, PipelineProfile profile)
        {
            IntPtr input_uri = GLib.Marshaller.StringToPtrGStrdup(inputUri.AbsoluteUri);
            IntPtr output_uri = GLib.Marshaller.StringToPtrGStrdup(outputUri.AbsoluteUri);

            bool have_error = !gst_transcoder_transcode(handle, input_uri, output_uri,
                profile.Pipeline, ProgressCallback);
            
            GLib.Marshaller.Free(input_uri);
            GLib.Marshaller.Free(output_uri);
            
            if(have_error) {
                IntPtr errPtr = gst_transcoder_get_error(handle);
                string error = Marshal.PtrToStringAnsi(errPtr);
                throw new ApplicationException(String.Format(
                    Catalog.GetString("Could not encode file: {0}"), error));
            }
            
            return outputUri;
        }
        
        public override void Cancel()
        {
            gst_transcoder_cancel(handle);
        }
        
        private void OnTranscoderProgress(IntPtr transcoder, double progress)
        {
            UpdateProgress(progress);
        }
    }
}
