/***************************************************************************
 *  FileEncodeTransaction.cs
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
using System.Collections;
using Nautilus;

namespace Sonance
{
	public delegate void FileEncodeCompleteHandler(object o, 
		FileEncodeCompleteArgs args);

	public class FileEncodeCompleteArgs : EventArgs
	{
		public TrackInfo Track;
		public string EncodedFilePath;
	}

	public class FileEncodeTransaction : LibraryTransaction
	{
		private ArrayList tracks = new ArrayList();
		private FileEncoder.EncodeFormat format;
		private FileEncoder encoder;
		private int encodedFilesFinished;
		
		public event FileEncodeCompleteHandler FileEncodeComplete; 
		
		private const int progressPrecision = 1000;
		
		public override string Name {
			get {
				return "File Encoder";
			}
		}
		
		public FileEncodeTransaction(FileEncoder.EncodeFormat format)
		{
			this.format = format;
			showCount = false;
			statusMessage = "Initializing Encoder...";
		}
		
		public void AddTrack(TrackInfo track)
		{
			tracks.Add(track);
		}
		
		public override void Run()
		{
			encoder = new GstFileEncoder();
			encoder.Progress += OnEncoderProgress;
			
			foreach(TrackInfo ti in tracks) {
				statusMessage = "Encoding " + ti.Artist + " - " + 
					ti.Title + "...";
				
				if(cancelRequested)
					break;
					
				try {
					string encPath = encoder.Encode(ti.Uri, format);
				
					FileEncodeCompleteHandler handler = FileEncodeComplete;
					if(handler != null) {
						FileEncodeCompleteArgs args = 
							new FileEncodeCompleteArgs();
						args.Track = ti;
						args.EncodedFilePath = encPath;
						handler(this, args);
					}
				} catch(Exception e) {
					Console.WriteLine("Could not encode '{0}': {1}",
						ti.Uri, e.Message);
				}
					
					
				encodedFilesFinished++;
			}
		}
		
		protected override void CancelAction()
		{
			if(encoder == null)
				return;
				
			encoder.Cancel();
		}
		
		private void OnEncoderProgress(object o, FileEncoderProgressArgs args)
		{
			totalCount = progressPrecision * tracks.Count;
			currentCount = (progressPrecision * encodedFilesFinished) + 
				(long)(args.Progress * (double)progressPrecision);
		}
	}
}
