/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  GstFileEncoder.cs
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

namespace Banshee.Base
{	
	internal delegate void GstFileEncoderProgressCallback(IntPtr encoder, 
		double progress);

	public class GstFileEncoder : FileEncoder
	{
		[DllImport("libbanshee")]
		private static extern IntPtr gst_file_encoder_new();
		
		[DllImport("libbanshee")]
		private static extern void gst_file_encoder_free(HandleRef encoder);
		
		[DllImport("libbanshee")]
		private static extern bool gst_file_encoder_encode_file(
			HandleRef encoder, IntPtr input_file, IntPtr output_file, 
			string encode_pipeline, GstFileEncoderProgressCallback progress_cb);
	
		[DllImport("libbanshee")]
		private static extern IntPtr gst_file_encoder_get_error(
			HandleRef encoder);
		
		[DllImport("libbanshee")]
		private static extern void gst_file_encoder_encode_cancel(
			HandleRef encoder);
		
		private HandleRef encoderHandle;
		private GstFileEncoderProgressCallback ProgressCallback;
		
		public GstFileEncoder()
		{
			IntPtr ptr = gst_file_encoder_new();
			if(ptr == IntPtr.Zero)
				throw new NullReferenceException(Catalog.GetString("Could not create encoder"));
			
			ProgressCallback = new GstFileEncoderProgressCallback(
				OnEncoderProgress);
			encoderHandle = new HandleRef(this, ptr);
		}
		
		public override void Dispose()
		{
			gst_file_encoder_free(encoderHandle);
		}
		
		public override Uri Encode(Uri inputUri, Uri outputUri, PipelineProfile profile)
		{
			IntPtr input_uri = GLib.Marshaller.StringToPtrGStrdup(inputUri.AbsoluteUri);
			IntPtr output_uri = GLib.Marshaller.StringToPtrGStrdup(outputUri.AbsoluteUri);

			bool have_error = !gst_file_encoder_encode_file(encoderHandle, input_uri, output_uri,
				profile.Pipeline, ProgressCallback);
			
			GLib.Marshaller.Free(input_uri);
			GLib.Marshaller.Free(output_uri);
			
			if(have_error) {
				IntPtr errPtr = gst_file_encoder_get_error(encoderHandle);
				string error = Marshal.PtrToStringAnsi(errPtr);
				throw new ApplicationException(String.Format(
					Catalog.GetString("Could not encode file: {0}"), error));
			}
			
			return outputUri;
		}
		
		public override void Cancel()
		{
			gst_file_encoder_encode_cancel(encoderHandle);
		}
		
		private void OnEncoderProgress(IntPtr encoder, double progress)
		{
			UpdateProgress(progress);
		}
	}
}
