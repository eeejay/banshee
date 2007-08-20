/***************************************************************************
 *  NautilusRecorder.cs
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

using Lnb=Banshee.Cdrom.Nautilus.Interop;
using Hal;

using Banshee.Base;
using Banshee.Cdrom;

namespace Banshee.Cdrom.Nautilus
{
    public class NautilusRecorder : NautilusDrive, IRecorder
    {
        private List<RecorderTrack> tracks = new List<RecorderTrack>();
        private object burn_recorder_mutex = new object();
        private Lnb.BurnRecorder burn_recorder = null;
        
        public event ActionChangedHandler ActionChanged;
        public event ProgressChangedHandler ProgressChanged;
        public event InsertMediaRequestHandler InsertMediaRequest;
        public event EventHandler WarnDataLoss;
        
        internal NautilusRecorder(Device device, Lnb.BurnDrive drive) : base(device, drive)
        {
        }
        
        public void ClearTracks()
        {
            tracks.Clear();
        }
        
        public void AddTrack(RecorderTrack track)
        {
            tracks.Add(track);
        }
        
        public void RemoveTrack(RecorderTrack track)
        {
            tracks.Remove(track);
        }
        
        public bool CancelWrite(bool skipIfDangerous)
        {
            return IsWriting ? burn_recorder.Cancel(skipIfDangerous) : false;
        }
        
        public RecorderResult WriteTracks(int speed, bool eject)
        {
            lock(burn_recorder_mutex) {
                burn_recorder = new Lnb.BurnRecorder();
                
                foreach(RecorderTrack track in tracks) {
                    Lnb.BurnRecorderTrackType nautilus_type;
                    
                    switch(track.Type) {
                        case RecorderTrackType.Data:
                            nautilus_type = Lnb.BurnRecorderTrackType.Data;
                            break;
                        case RecorderTrackType.Cue:
                            nautilus_type = Lnb.BurnRecorderTrackType.Cue;
                            break;
                        case RecorderTrackType.Audio:
                        default:
                            nautilus_type = Lnb.BurnRecorderTrackType.Audio;
                            break;
                    }
                    
                    burn_recorder.AddTrack(new Lnb.BurnRecorderTrack(track.FileName, nautilus_type));
                }
                
                Lnb.BurnRecorderWriteFlags flags = Lnb.BurnRecorderWriteFlags.Debug;
                
                if(eject) {
                    flags |= Lnb.BurnRecorderWriteFlags.Eject;
                }
                
                if(Environment.GetEnvironmentVariable("BURNER_SIMULATE") != null) {
                    Console.Error.WriteLine("** Simulating CD Record **");
                    flags |= Lnb.BurnRecorderWriteFlags.DummyWrite;
                }
                
                try {
                    burn_recorder.ActionChanged += OnActionChanged;
                    burn_recorder.ProgressChanged += OnProgressChanged;
                    burn_recorder.InsertMediaRequest += OnInsertMediaRequest;
                    burn_recorder.WarnDataLoss += OnWarnDataLoss;
                    
                    switch(burn_recorder.WriteTracks(new Lnb.BurnDrive(Drive.Device), speed, flags)) {
                        case Lnb.BurnRecorderResult.Cancel:
                            return RecorderResult.Canceled;
                        case Lnb.BurnRecorderResult.Finished:
                            return RecorderResult.Finished;
                        case Lnb.BurnRecorderResult.Retry:
                            return RecorderResult.Retry;
                        case Lnb.BurnRecorderResult.Error:
                        default:
                            return RecorderResult.Error;
                    }
                } finally {
                    burn_recorder.ActionChanged -= OnActionChanged;
                    burn_recorder.ProgressChanged -= OnProgressChanged;
                    burn_recorder.InsertMediaRequest -= OnInsertMediaRequest;
                    burn_recorder.WarnDataLoss -= OnWarnDataLoss;
                }
            }
        }
        
        internal virtual void OnActionChanged(object o, Lnb.ActionChangedArgs args)
        {
            ActionChangedHandler handler = ActionChanged;
            if(handler != null) {
                RecorderAction type;
                switch(args.Action) {
                    case Lnb.BurnRecorderActions.Blanking:
                        type = RecorderAction.Blanking;
                        break;
                    case Lnb.BurnRecorderActions.Fixating:
                        type = RecorderAction.Fixating;
                        break;
                    case Lnb.BurnRecorderActions.Writing:
                        type = RecorderAction.Writing;
                        break;
                    case Lnb.BurnRecorderActions.PreparingWrite:
                    default:
                        type = RecorderAction.PreparingWrite;
                        break;
                }
                
                DriveMediaType media_type;
                switch(args.Media) {
                    case Lnb.BurnRecorderMedia.Dvd:
                        media_type = DriveMediaType.DVD;
                        break;
                    case Lnb.BurnRecorderMedia.Cd:
                    default:
                        media_type = DriveMediaType.CD;
                        break;
                }
                
                handler(this, new ActionChangedArgs(type, media_type));
            }
        }
        
        internal virtual void OnProgressChanged(object o, Lnb.ProgressChangedArgs args)
        {
            ProgressChangedHandler handler = ProgressChanged;
            if(handler != null) {
                handler(this, new ProgressChangedArgs(args.Fraction));
            }
        }
        
        internal virtual void OnInsertMediaRequest(object o, Lnb.InsertMediaRequestArgs args)
        {
            InsertMediaRequestHandler handler = InsertMediaRequest;
            if(handler != null) {
                handler(this, new InsertMediaRequestArgs(args.Busy, args.CanRewrite, args.IsReload));
            }
        }
        
        internal virtual void OnWarnDataLoss(object o, Lnb.WarnDataLossArgs args)
        {
            EventHandler handler = WarnDataLoss;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public bool IsWriting {
            get { return burn_recorder != null; }
        }
    }
}
