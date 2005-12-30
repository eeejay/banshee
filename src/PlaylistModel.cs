/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PlaylistModel.cs
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
using System.Collections;
using System.Threading;
using Gtk;
using Sql;

using Banshee.Base;

namespace Banshee
{
    public enum RepeatMode {
        None,
        All,
        Single
    }

    public class PlaylistModel : ListStore, IPlaybackModel
    {
        private static int uid;
        private TimeSpan totalDuration = new TimeSpan(0);
        
        private ArrayList trackInfoQueue;
        private bool trackInfoQueueLocked = false;
        private TreeIter playingIter;
        
        private RepeatMode repeat = RepeatMode.None;
        private bool shuffle = false;
        
        public Source Source;
        
        public event EventHandler Updated;
        
        public static int NextUid
        {
            get {
                return uid++;
            }
        }
        
        public PlaylistModel() : base(typeof(TrackInfo))
        {
            trackInfoQueue = new ArrayList();
            GLib.Timeout.Add(300, new GLib.TimeoutHandler(OnIdle));
        }
    
        private void SyncPlayingIter()
        {
            if(PlayerEngineCore.ActivePlayer.Track == null) {
                playingIter = TreeIter.Zero;
            } else {
                for(int i = 0, n = Count(); i < n; i++) {
                    TreeIter iter;
                    if(!IterNthChild(out iter, i))
                        continue;
                        
                    TrackInfo ti = IterTrackInfo(iter);
                    
                    if(PlayerEngineCore.ActivePlayer.Track.Uid == ti.Uid) {
                        playingIter = iter;
                        break;
                    }
                }
            }
        }
    
        // --- Load Queue and Additions ---
    
        private bool OnIdle()
        {
            QueueSync();
            return true;
        }

        private void QueueSync()
        {
            if(trackInfoQueue.Count <= 0)
                return;
            
            trackInfoQueueLocked = true;
                
            foreach(TrackInfo ti in trackInfoQueue)
                AddTrack(ti);

            trackInfoQueue.Clear();
            trackInfoQueueLocked = false;
            SyncPlayingIter();
            
            return;
        }
            
        public void QueueAddTrack(TrackInfo ti)
        {
            while(trackInfoQueueLocked);
            trackInfoQueue.Add(ti);
        }

        private void OnLoaderHaveTrackInfo(object o, HaveTrackInfoArgs args)
        {
            QueueAddTrack(args.TrackInfo);
        }

        public void AddTrack(TrackInfo ti)
        {
            AddTrack(ti, true);
        }
        
        public void AddTrack(TrackInfo ti, bool raiseUpdate)
        {
            if(ti == null)
                return;

            totalDuration += ti.Duration;
            ti.TreeIter = AppendValues(ti);
            
            if(raiseUpdate) {
                RaiseUpdated(this, new EventArgs());
            }
        }
        
        public void LoadFromLocalQueue()
        {
            ClearModel();
            foreach(TrackInfo ti in LocalQueueSource.Instance.Tracks) {
                AddTrack(ti, false);
            }
        }

        public void LoadFromPlaylist(string name)
        {
            ClearModel();
            PlaylistLoadTransaction loader = new PlaylistLoadTransaction(name);
            loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
            PlayerCore.TransactionManager.Register(loader);
        }
        
        public void LoadFromLibrary()
        {
            ClearModel();
            
            foreach(TrackInfo ti in Globals.Library.Tracks.Values) {
                AddTrack(ti, false);
            }
            
            SyncPlayingIter();
            RaiseUpdated(this, new EventArgs());
        }
        
        public void LoadFromDapSource(DapSource dapSource)
        {
            ClearModel();
            
            foreach(TrackInfo track in dapSource.Device) {
                AddTrack(track);
            }
            
            SyncPlayingIter();
        }
        
        public void LoadFromAudioCdSource(AudioCdSource cdSource)
        {    
            ClearModel();
            
            AudioCdDisk disk = cdSource.Disk;
            
            foreach(AudioCdTrackInfo track in disk.Tracks) {
                AddTrack(track);
            }
            
            SyncPlayingIter();
        }

        // --- Helper Methods ---
        
        public TrackInfo IterTrackInfo(TreeIter iter)
        {
            object o = GetValue(iter, 0);
            if(o != null) {
              return o as TrackInfo;
            }
            
            return null;
        }
        
        public TrackInfo PathTrackInfo(TreePath path)
        {
            TreeIter iter;
            
            if(!GetIter(out iter, path))
                return null;
                
            return IterTrackInfo(iter);
        }
        
        // --- Playback Methods ---
        
        public void PlayPath(TreePath path)
        {
            TrackInfo ti = PathTrackInfo(path);
            if(ti == null)
                return;
                
            PlayerCore.UserInterface.PlayFile(ti);
            GetIter(out playingIter, path);
        }
        
        public void PlayIter(TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            if(ti == null)
                return;
                
            if(ti.CanPlay) {
                PlayerCore.UserInterface.PlayFile(ti);
                playingIter = iter;
            } else {
                playingIter = iter;
                Continue();
            }
        }
        
        // --- IPlaybackModel 
        
        public void PlayPause()
        {
        
        }
        
        public void Advance()
        {
            ChangeDirection(true);
        }

        public void Regress()
        {
            ChangeDirection(false);    
        }

        public void Continue()
        {
            Advance();
        }
        
        private void ChangeDirection(bool forward)
        {
            // TODO: Implement random playback without repeating a track 
            // until all Tracks have been played first (see Legacy Sonance)
            
            TreePath currentPath;
            TreeIter currentIter, nextIter = TreeIter.Zero;
            TrackInfo currentTrack = null, nextTrack;
            
            try {
                currentPath = GetPath(playingIter);
            } catch(NullReferenceException) {
                currentPath = null;
            }
            
            if(currentPath == null) {
                if(shuffle) {
                    if(!GetRandomTrackIter(out nextIter))
                        return;
                } else if(!GetIterFirst(out nextIter)) {
                    return;
                }

                PlayIter(nextIter);
                return;
            }
        
            int count = Count();
            int index = FindIndex(currentPath);
            bool lastTrack = index == count - 1;
        
            if(count <= 0 || index >= count || index < 0)
                return;
                
            currentTrack = PathTrackInfo(currentPath);
            currentIter = playingIter;
        
            if(repeat == RepeatMode.Single) {
                nextIter = currentIter;
            } else if(forward) {
                if(lastTrack && repeat == RepeatMode.All) {
                    if(!IterNthChild(out nextIter, 0))
                        return;
                } else if(shuffle) {
                    if(!GetRandomTrackIter(out nextIter))
                        return;
                } else {                
                    currentPath.Next();                
                    if(!GetIter(out nextIter, currentPath))
                        return;
                }
                
                nextTrack = IterTrackInfo(nextIter);
                nextTrack.PreviousTrack = currentIter;
            } else {
                if(currentTrack.PreviousTrack.Equals(TreeIter.Zero)) {
                    if(index > 0 && currentPath.Prev()) {
                        if(!GetIter(out nextIter, currentPath)) {
                            return;
                        }
                    } else {
                        return;
                    }
                } else {
                    nextIter = currentTrack.PreviousTrack;
                }
            }
            
            if(!nextIter.Equals(TreeIter.Zero))
                PlayIter(nextIter);
        }

        public int Count()
        {
            return IterNChildren();
        }
        
        private int FindIndex(TreePath a)
        {
            TreeIter iter;
            TreePath b;
            int i, n;
    
            for(i = 0, n = Count(); i < n; i++) {
                IterNthChild(out iter, i);
                b = GetPath(iter);
                if(a.Compare(b) == 0) 
                    return i;
            }
    
            return -1;
        }

        private bool GetRandomTrackIter(out TreeIter iter)
        {
            int randIndex = Globals.Random.Next(0, Count());
            return IterNthChild(out iter, randIndex);
        }
        
        public void ClearModel()
        {
            PlayerCore.TransactionManager.Cancel(typeof(FileLoadTransaction));
            trackInfoQueue.Clear();
        
            totalDuration = new TimeSpan(0);
            playingIter = TreeIter.Zero;
            Clear();
                
            if(Updated != null && ThreadAssist.InMainThread)
                Updated(this, new EventArgs());
        }
        
        /*public void RemoveTrack(TreePath path)
        {
            TrackInfo ti = PathTrackInfo(path);
            TreeIter iter;
            
            if(!GetIter(out iter, path))
                return;
                
            if(Source.Type == SourceType.Playlist 
                || Source.Type == SourceType.Library) {
                Statement query = new Delete("PlaylistEntries")
                    + new Where("TrackID", Op.EqualTo, ti.TrackId);
                try {
                    Core.Library.Db.Execute(query);
                } catch(Exception) {}
                
                
                if(Source.Type == SourceType.Library) {
                    Statement query2 = new Delete("Tracks")
                        + new Where("TrackID", Op.EqualTo, ti.TrackId);
                    try {
                        Core.Library.Db.Execute(query2);
                    } catch(Exception) {}
                }
            }
            
            RemoveTrack(ref iter, ti);
        }
        
        private void RemoveTrack(ref TreeIter iter, TrackInfo ti)
        {
            randomQueue.Remove(iter);
            Remove(ref iter);
        }
        */
        
        public void RemoveTrack(ref TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            totalDuration -= ti.Duration;
            ti.TreeIter = TreeIter.Zero;
            Remove(ref iter);
            RaiseUpdated(this, new EventArgs());
        }
        
        // --- Event Raise Handlers ---

        private void RaiseUpdated(object o, EventArgs args)
        {
            EventHandler handler = Updated;
            if(handler != null)
                handler(o, args);
        }
        
        public TimeSpan TotalDuration 
        {
            get {
                return totalDuration;
            }
        }
        
        public TreePath PlayingPath
        {
            get {
                try {
                    return playingIter.Equals(TreeIter.Zero) 
                        ? null : GetPath(playingIter);
                } catch(NullReferenceException) {
                    return null;
                }
            }
        }
        
        public TreeIter PlayingIter
        {
            get {
                return playingIter;
            }
        }
        
        public RepeatMode Repeat {
            set {
                repeat = value;
            }
            
            get {
                return repeat;
            }
        }
        
        public bool Shuffle {
            set {
                shuffle = value;
            }
            
            get {
                return shuffle;
            }
        }
        
        public TrackInfo FirstTrack {
          get {
              TreeIter iter = TreeIter.Zero;
              if(GetIterFirst(out iter) && !iter.Equals(TreeIter.Zero))
                  return IterTrackInfo(iter);
              return null;
          }
       }
    }
}
