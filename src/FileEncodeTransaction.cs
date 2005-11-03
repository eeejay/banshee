/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
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
using Mono.Unix;
using Nautilus;

namespace Banshee
{
    public delegate void FileEncodeCompleteHandler(object o, FileEncodeCompleteArgs args);

    public class FileEncodeCompleteArgs : EventArgs
    {
        public TrackInfo Track;
        public Uri InputUri;
        public Uri EncodedFileUri;
    }

    public class FileEncodeAction
    {
        private Hashtable tracks = new Hashtable();
        private FileEncoder encoder;
        private PipelineProfile profile;
        private int encodedFilesFinished;
        
        private ActiveUserEvent user_event;
        
        public event FileEncodeCompleteHandler FileEncodeComplete; 
        public event EventHandler Finished;
        public event EventHandler Canceled;
        
        private const int progressPrecision = 1000;
        
        public FileEncodeAction(PipelineProfile profile)
        {
            this.profile = profile;
            user_event = new ActiveUserEvent(Catalog.GetString("File Encoder"));
            user_event.CancelRequested += OnCancelRequested;
            user_event.Icon = Gdk.Pixbuf.LoadFromResource("encode-action-24.png");
            user_event.Message = Catalog.GetString("Initializing Encoder...");
        }
        
        public void AddTrack(TrackInfo track, Uri outputUri)
        {
            tracks[track] = outputUri;
        }
        
        public void AddTrack(Uri inputUri, Uri outputUri)
        {
            tracks[inputUri] = outputUri;
        }
        
        public void Run()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(ThreadedRun);
        }
        
        public void ThreadedRun(object o)
        {
            encoder = new GstFileEncoder();
            encoder.Progress += OnEncoderProgress;
            
            foreach(object obj in tracks.Keys) {
                Uri outputUri = tracks[obj] as Uri;
                Uri inputUri = null;
                
                if(obj is TrackInfo) {
                    user_event.Message = String.Format(Catalog.GetString("Encoding {0} - {1} ..."),
                        (obj as TrackInfo).Artist, (obj as TrackInfo).Title);
                    inputUri = (obj as TrackInfo).Uri;
                } else if(obj is Uri) {
                    user_event.Message = Catalog.GetString("Encoding files...");
                    inputUri = obj as Uri;
                }
                
                if(user_event.IsCancelRequested) {
                    break;
                }
                    
                try {
                    Uri encUri = encoder.Encode(inputUri, outputUri, profile);
                       
                    FileEncodeCompleteHandler handler = FileEncodeComplete;
                    if(handler != null) {
                        FileEncodeCompleteArgs args = new FileEncodeCompleteArgs();
                        if(obj is TrackInfo) {
                          args.Track = obj as TrackInfo;
                          args.InputUri = (obj as TrackInfo).Uri;
                        } else if(obj is Uri) {
                          args.InputUri = (obj as Uri);
                        }
                        
                        args.EncodedFileUri = encUri;
                        handler(this, args);
                    }
                } catch(Exception e) {
                    Console.WriteLine("Could not encode '{0}': {1}", inputUri.AbsoluteUri, 
                        e.Message);
                }
                    
                encodedFilesFinished++;
            }
            
            user_event.Dispose();
            
            if(Finished != null) {
                Finished(this, new EventArgs());
            }
        }
        
        private void OnCancelRequested(object o, EventArgs args)
        {
            if(Canceled != null) {
                Canceled(this, new EventArgs());
            }
        
            if(encoder == null) {
                return;
            }
            
            encoder.Cancel();
        }
        
        private void OnEncoderProgress(object o, FileEncoderProgressArgs args)
        {
            user_event.Progress = ((double)(progressPrecision * encodedFilesFinished) +
                (args.Progress * (double)progressPrecision)) / 
                (double)(progressPrecision * tracks.Count);
        }
    }
}
