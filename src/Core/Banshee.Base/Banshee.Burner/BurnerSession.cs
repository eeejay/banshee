/***************************************************************************
 *  BurnerSession.cs
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

using Banshee.Cdrom;
using Banshee.Collection;
using Banshee.Base;

namespace Banshee.Burner
{
    internal class BurnerSessionTrack
    {
        public TrackInfo Track;
        public string WriteFromPath;
        public string DiscPath;
    }
        
    public class BurnerSession
    {
        private IRecorder recorder;
        private string disc_name;
        private int write_speed;
        private bool eject_when_finished;
        private string disc_format;
        
        private List<BurnerSessionTrack> tracks = new List<BurnerSessionTrack>();
        
        public event EventHandler RecorderMediaChanged;

        public void ClearTracks()
        {
            tracks.Clear();
        }

        public void AddTrack(TrackInfo track, string discPath)
        {
            BurnerSessionTrack session_track = new BurnerSessionTrack();
            session_track.Track = track;
            session_track.DiscPath = discPath;
            session_track.WriteFromPath = null;
            tracks.Add(session_track);
        }
        
        public bool Record()
        {
            BurnerSessionPreparer preparer = new BurnerSessionPreparer(this);
            return preparer.Record();
        }
        
        private void OnRecorderMediaChanged(object o, MediaArgs args)
        {
            EventHandler handler = RecorderMediaChanged;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }

        public IRecorder Recorder {
            get { return recorder; }
            set {
                if(recorder == value) {
                    return;
                } else if(recorder != null) {
                    recorder.MediaAdded -= OnRecorderMediaChanged;
                    recorder.MediaRemoved -= OnRecorderMediaChanged;
                }
                
                recorder = value;
                recorder.MediaAdded += OnRecorderMediaChanged;
                recorder.MediaRemoved += OnRecorderMediaChanged;
                
                OnRecorderMediaChanged(null, null);
            }
        }
        
        public string DiscName {
            get { return disc_name; }
            set { disc_name = value; }
        }
        
        public int WriteSpeed {
            get { return write_speed; }
            set { write_speed = value; }
        }
        
        public bool EjectWhenFinished {
            get { return eject_when_finished; }
            set { eject_when_finished = value; }
        }
        
        public string DiscFormat {
            get { return disc_format; }
            set { disc_format = value; }
        }
        
        internal List<BurnerSessionTrack> Tracks {
            get { return tracks; }
        }
    }
}
