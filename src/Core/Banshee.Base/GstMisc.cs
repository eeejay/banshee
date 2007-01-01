
/***************************************************************************
 *  GstMisc.cs
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
using Banshee.Base;

namespace Banshee.Gstreamer
{
    public static class Utilities
    {
        [DllImport("libbanshee")]
        private static extern bool gstreamer_test_encoder(IntPtr encoder_pipeline);
        
        public static bool TestEncoder(string pipeline)
        {
            if(pipeline == null || pipeline == String.Empty) {
                return false;
            }
        
            IntPtr pipeline_ptr = GLib.Marshaller.StringToPtrGStrdup(pipeline);
            
            if(pipeline_ptr == IntPtr.Zero) {
                return false;
            }
            
            try {
                return gstreamer_test_encoder(pipeline_ptr);
            } finally {
                GLib.Marshaller.Free(pipeline_ptr);
            }
        }
        
        [DllImport("libbanshee")]
        private static extern bool gstreamer_test_pipeline(IntPtr pipeline);
        
        public static bool TestPipeline(string pipeline)
        {
            if(pipeline == null || pipeline == String.Empty) {
                return false;
            }
        
            IntPtr pipeline_ptr = GLib.Marshaller.StringToPtrGStrdup(pipeline);
            
            if(pipeline_ptr == IntPtr.Zero) {
                return false;
            }
            
            try {
                return gstreamer_test_pipeline(pipeline_ptr);
            } finally {
                GLib.Marshaller.Free(pipeline_ptr);
            }
        }
        
        [DllImport("libbanshee")]
        private static extern void gstreamer_initialize();
        
        public static void Initialize()
        {
            gstreamer_initialize();
        }
    }
}

