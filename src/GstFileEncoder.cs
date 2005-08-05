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

namespace Banshee
{	
	internal delegate void GstFileEncoderProgressCallback(IntPtr encoder, 
		double progress);

	public class GstFileEncoder : FileEncoder, IDisposable
	{
		[DllImport("libgstmediaengine")]
		private static extern IntPtr gst_file_encoder_new();
		
		[DllImport("libgstmediaengine")]
		private static extern void gst_file_encoder_free(HandleRef encoder);
		
		[DllImport("libgstmediaengine")]
		private static extern bool gst_file_encoder_encode_file(
			HandleRef encoder, string input_file, string output_file, 
			EncodeFormat format, GstFileEncoderProgressCallback progress_cb);
	
		[DllImport("libgstmediaengine")]
		private static extern IntPtr gst_file_encoder_get_error(
			HandleRef encoder);
		
		[DllImport("libgstmediaengine")]
		private static extern void gst_file_encoder_encode_cancel(
			HandleRef encoder);
		
		private HandleRef encoderHandle;
		private GstFileEncoderProgressCallback ProgressCallback;
		
		public GstFileEncoder()
		{
			IntPtr ptr = gst_file_encoder_new();
			if(ptr == IntPtr.Zero)
				throw new NullReferenceException("Could not create encoder");
			
			ProgressCallback = new GstFileEncoderProgressCallback(
				OnEncoderProgress);
			encoderHandle = new HandleRef(this, ptr);
		}
		
		~GstFileEncoder()
		{
			Dispose();
		}
		
		public void Dispose()
		{
			gst_file_encoder_free(encoderHandle);
		}
		
		public override string Encode(string inputFile, EncodeFormat format)
		{
			string outputFile = GetBurnTempFile(inputFile, format);
			
			if(!gst_file_encoder_encode_file(encoderHandle, inputFile, 
				outputFile, format, ProgressCallback)) {
				IntPtr errPtr = gst_file_encoder_get_error(encoderHandle);
				string error = Marshal.PtrToStringAnsi(errPtr);
				throw new ApplicationException(String.Format(
					"Could not encode file: {0}", error));
			}
		
			return outputFile;
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
