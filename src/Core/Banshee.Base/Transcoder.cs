/***************************************************************************
 *  Transcoder.cs
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
using Banshee.AudioProfiles;

namespace Banshee.Base
{
    public delegate void TranscoderProgressHandler(object o, TranscoderProgressArgs args);
        
    public class TranscoderProgressArgs : EventArgs
    {
        public double Progress;
    }
        
    public abstract class Transcoder : IDisposable
    {        
        public event TranscoderProgressHandler Progress;
        public event EventHandler Finished;
        public event EventHandler Error;
        
        public abstract void BeginTranscode(SafeUri inputUri, SafeUri outputUri, Profile profile);
        public abstract void Cancel();
        public abstract bool IsTranscoding { get; }
        public abstract string ErrorMessage { get; }
        
        protected virtual void OnProgress(double progress)
        {
            TranscoderProgressHandler handler = Progress;
            if(handler != null) {
                TranscoderProgressArgs args = new TranscoderProgressArgs();
                args.Progress = progress;
                handler(this, args);
            }
        }
        
        protected virtual void OnFinished()
        {
            EventHandler handler = Finished;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        protected virtual void OnError()
        {
            EventHandler handler = Error;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public virtual void Dispose()
        {
        }
    }
}
