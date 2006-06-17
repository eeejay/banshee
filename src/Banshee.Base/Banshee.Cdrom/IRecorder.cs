/***************************************************************************
 *  IRecorder.cs
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

namespace Banshee.Cdrom
{
    public interface IRecorder : IDrive
    {
        event ActionChangedHandler ActionChanged;
        event ProgressChangedHandler ProgressChanged;
        event InsertMediaRequestHandler InsertMediaRequest;
        event EventHandler WarnDataLoss;
        
        void ClearTracks();
        void AddTrack(RecorderTrack track);
        void RemoveTrack(RecorderTrack track);
        
        RecorderResult WriteTracks(int speed, bool eject);
        bool CancelWrite(bool skipIfDangerous);
        
        bool IsWriting { get; }
    }
    
    public delegate void ActionChangedHandler(object o, ActionChangedArgs args);
    public delegate void ProgressChangedHandler(object o, ProgressChangedArgs args);
    public delegate void InsertMediaRequestHandler(object o, InsertMediaRequestArgs args);
    
    public sealed class ActionChangedArgs : EventArgs
    {
        private RecorderAction action;
        private DriveMediaType media_type;
        
        public ActionChangedArgs(RecorderAction action, DriveMediaType mediaType)
        {
            this.action = action;
            this.media_type = mediaType;
        }
        
        public RecorderAction Action {
            get { return action; }
        }
        
        public DriveMediaType MediaType {
            get { return media_type; }
        }
    }
    
    public sealed class ProgressChangedArgs : EventArgs
    {
        private double fraction;
        
        public ProgressChangedArgs(double fraction)
        {
            this.fraction = fraction;
        }
        
        public double Fraction {
            get { return fraction; }
        }
    }
    
    public sealed class InsertMediaRequestArgs : EventArgs
    {
        private bool busy;
        private bool can_rewrite;
        private bool is_reload;
        
        public InsertMediaRequestArgs(bool busy, bool canRewrite, bool isReload)
        {
            this.busy = busy;
            this.can_rewrite = canRewrite;
            this.is_reload = isReload;
        }
        
        public bool Busy {
            get { return busy; }
        }
        
        public bool CanRewrite {
            get { return can_rewrite; }
        }
        
        public bool IsReload {
            get { return is_reload; }
        }
    }
}
