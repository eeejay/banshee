using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mtp
{
    public abstract class AbstractTrackList
    {
        private bool saved;
        private List<int> track_ids;
		private MtpDevice device;

        public abstract uint Count { get; protected set; }
        public abstract string Name { get; set; }
        protected abstract IntPtr TracksPtr { get; set; }

        protected abstract int Create ();
        protected abstract int Update ();

        public bool Saved { get { return saved; } }
        protected MtpDevice Device { get { return device; } }

        public IList<int> TrackIds {
            get { return track_ids; }
        }

        public AbstractTrackList (MtpDevice device, string name)
        {
            this.device = device;
            track_ids = new List<int> ();
        }

        internal AbstractTrackList (MtpDevice device, IntPtr tracks, uint count)
        {
            this.device = device;
            this.saved = true;

            if (tracks != IntPtr.Zero) {
                int [] vals = new int [count];
                Marshal.Copy ((IntPtr)tracks, (int[])vals, 0, (int)count);
                track_ids = new List<int> (vals);
            } else {
                track_ids = new List<int> ();
            }
        }

        public void AddTrack (Track track)
        {
            AddTrack ((int)track.FileId);
        }

        public void AddTrack (int track_id)
        {
            track_ids.Add (track_id);
            Count++;
        }

        public void RemoveTrack (Track track)
        {
            RemoveTrack ((int)track.FileId);
        }
        
        public void RemoveTrack (int track_id)
        {
            track_ids.Remove (track_id);
            Count--;
        }

        public void ClearTracks ()
        {
            track_ids.Clear ();
            Count = 0;
        }

        public virtual void Save ()
        {
            Count = (uint) track_ids.Count;
            Console.WriteLine ("saving {0} {1} with {2} tracks", this.GetType(), this.Name, this.Count);

            if (TracksPtr != IntPtr.Zero) {
                Marshal.FreeHGlobal (TracksPtr);
                TracksPtr = IntPtr.Zero;
            }

            if (Count == 0) {
                TracksPtr = IntPtr.Zero;
            } else {
                TracksPtr = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (int)) * (int)Count);
                Marshal.Copy (track_ids.ToArray (), 0, TracksPtr, (int)Count);
            }

            if (saved) {
                saved = Update () == 0;
            } else {
                saved = Create () == 0;
            }

            if (TracksPtr != IntPtr.Zero) {
                Marshal.FreeHGlobal (TracksPtr);
                TracksPtr = IntPtr.Zero;
            }
        }
    }
}
