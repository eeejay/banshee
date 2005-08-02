/***************************************************************************
 *  FileEncoder.cs
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
using System.IO;

namespace Sonance
{
	public delegate void FileEncoderProgressHandler(object o, 
		FileEncoderProgressArgs args);
		
	public class FileEncoderProgressArgs : EventArgs
	{
		public double Progress;
	}
		
	public abstract class FileEncoder
	{		
		public event FileEncoderProgressHandler Progress;
	
		public enum EncodeFormat : uint {
			Wav = 0,
			Mp3,
			Aac
		};
	
		public abstract string Encode(string inputFile, EncodeFormat format);
		public abstract void Cancel();
		
		protected string GetBurnTempFile(string inputFile, EncodeFormat format)
		{
			string ext;

			switch(format) {
				case EncodeFormat.Mp3:
					ext = "mp3";
					break;
				case EncodeFormat.Aac:
					ext = "mp4";
					break;
				case EncodeFormat.Wav:
				default:
					ext = "wav";
					break;
			} 

			return Paths.TempDir + "/"  + 
				Path.GetFileNameWithoutExtension(inputFile) + "." + ext;
		}
		
		protected void UpdateProgress(double progress)
		{
			FileEncoderProgressHandler handler = Progress;
			if(handler != null) {
				FileEncoderProgressArgs args = new FileEncoderProgressArgs();
				args.Progress = progress;
				handler(this, args);
			}
		}
	}
}
