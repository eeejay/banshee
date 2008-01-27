/***************************************************************************
 *  PodcastPlaylistModel.cs
 *
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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

using Gtk;
using Banshee.Base;

namespace Banshee.Plugins.Podcast.UI
{
    internal class PodcastPlaylistModel : ListStore
    {
        private ArrayList add_queue;
        private ArrayList remove_queue;

        public PodcastPlaylistModel () : base(typeof(PodcastInfo))
        {
            add_queue = new ArrayList ();
            remove_queue = new ArrayList ();
        }

        public PodcastInfo IterPodcastInfo (TreeIter iter)
        {
            object o = GetValue(iter, 0);

            if (o != null)
            {
                return o as PodcastInfo;
            }

            return null;
        }

        public PodcastInfo PathPodcastInfo (TreePath path)
        {
            if (path == null)
            {
                return null;
            }

            TreeIter iter;

            if(!GetIter (out iter, path))
            {
                return null;
            }

            return IterPodcastInfo(iter);
        }

        public PodcastInfo[] PathPodcastInfo (TreePath[] paths)
        {
            if (paths == null)
            {
                return null;
            }

            ArrayList podcasts = new ArrayList (paths.Length);
            PodcastInfo tmp_podcast;

            foreach (TreePath path in paths)
            {
                tmp_podcast = PathPodcastInfo (path);

                if (tmp_podcast != null)
                {
                    podcasts.Add (tmp_podcast);
                }
            }

            if (podcasts.Count <= 0)
            {
                return null;
            }
            else
            {
                return podcasts.ToArray (typeof (PodcastInfo)) as PodcastInfo[];
            }
        }

        public TrackInfo IterTrackInfo (TreeIter iter)
        {
            PodcastInfo pi = IterPodcastInfo (iter);

            if (pi != null)
            {
                return pi.Track;
            }

            return null;
        }

        public TrackInfo PathTrackInfo (TreePath path)
        {
            TreeIter iter;

            if (!GetIter (out iter, path))
            {
                return null;
            }

            return IterTrackInfo (iter);
        }

        public void PlayPath (TreePath path)
        {
            PodcastInfo pi = PathPodcastInfo (path);

            if (pi == null)
            {
                return;
            }

            if (pi.Track != null)
            {
                Play (pi.Track);
            } else if (pi.IsDownloaded && 
                !String.IsNullOrEmpty (pi.LocalPath)) { 
                Banshee.Web.Browser.Open (pi.LocalPath);
            } else if (pi.Url != null && !pi.IsDownloaded) {
                Play (pi.Url);
            }
        }

        public void PlayIter (TreeIter iter)
        {
            TrackInfo ti = IterTrackInfo(iter);
            Play (ti);
        }

        private void Play (TrackInfo ti)
        {
            if (ti.CanPlay)
            {
                PlayerEngineCore.OpenPlay (ti);
            }
        }

        private void Play (SafeUri uri)
        {
            PlayerEngineCore.Open (uri);

            ThreadAssist.Spawn ( delegate {
                                     PlayerEngineCore.Play ();
                                 });
        }

        public void ClearModel ()
        {
            Clear();
        }

        public void QueueAdd (PodcastInfo pi)
        {
            lock (add_queue.SyncRoot)
            {
                add_queue.Add (pi);
            }

            GLib.Idle.Add (PumpAddQueue);
        }

        public void QueueAdd (ICollection podcasts)
        {
            lock (add_queue.SyncRoot)
            {
                foreach (PodcastInfo pi in podcasts)
                {
                    add_queue.Add (pi);
                }
            }

            GLib.Idle.Add (PumpAddQueue);
        }

        private void AddPodcast (PodcastInfo pi)
        {
            if (pi == null)
            {
                return;
            }

            pi.TreeIter = AppendValues (pi);
        }

        private void AddPodcasts (ICollection podcasts)
        {
            if (podcasts == null)
            {
                return;
            }

            foreach (PodcastInfo pi in podcasts)
            {
                AddPodcast (pi);
            }
        }

        public void QueueRemove (PodcastInfo pi)
        {
            lock (remove_queue.SyncRoot)
            {
                remove_queue.Add (pi);
            }

            GLib.Idle.Add (PumpRemoveQueue);
        }

        public void QueueRemove (ICollection podcasts)
        {
            lock (remove_queue.SyncRoot)
            {
                foreach (PodcastInfo pi in podcasts)
                {
                    remove_queue.Add (pi);
                }
            }

            GLib.Idle.Add (PumpRemoveQueue);
        }

        private void RemovePodcast (PodcastInfo pi)
        {
            if (pi != null)
            {
                TreeIter iter = pi.TreeIter;
                pi.TreeIter = TreeIter.Zero;

                if (IterIsValid (iter))
                {
                    Remove (ref iter);
                }
            }
        }

        private void RemovePodcasts (ICollection podcasts)
        {
            if (podcasts == null)
            {
                return;
            }

            foreach (PodcastInfo pi in podcasts)
            {
                RemovePodcast (pi);
            }
        }

        private delegate void SinglePodcastAction (PodcastInfo pi);
        private delegate void MultiplePodcastAction (ICollection podcasts);

        private bool PumpAddQueue ()
        {
            return PumpQueue (add_queue, AddPodcast, AddPodcasts);
        }

        private bool PumpRemoveQueue ()
        {
            return PumpQueue (remove_queue, RemovePodcast, RemovePodcasts);
        }

        private bool PumpQueue (ArrayList queue,
                                SinglePodcastAction spa, MultiplePodcastAction mpa)
        {
            PodcastInfo pi = null;
            ICollection podcasts = null;

            int range_upper = -1;

            lock (queue.SyncRoot)
            {

                if (queue.Count == 0)
                {
                    return false;
                }

                int queue_count = queue.Count;
		// A count of 32 caused relatively skipless audio playback 
		// while synchronizing large feeds (200-300 episodes).
                range_upper = (queue_count >= 32) ? 31 : queue_count;

                if (queue_count == 1)
                {
                    pi = queue [0] as PodcastInfo;
                }
                else if (queue_count > 1)
                {
                    podcasts = queue.GetRange (0, range_upper);
                }
            }

            if (pi != null)
            {
                spa (pi);
            }
            else if (podcasts != null)
            {
                mpa (podcasts);
            }

            lock (queue.SyncRoot)
            {
                queue.RemoveRange (0, range_upper);

                if (queue.Count == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
