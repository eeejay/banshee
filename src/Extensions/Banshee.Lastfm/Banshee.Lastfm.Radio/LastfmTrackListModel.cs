//
// LastfmTrackListModell.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

using Banshee.Collection;
 
namespace Banshee.Lastfm.Radio
{
    public class LastfmTrackListModel : TrackListModel, IEnumerable<TrackInfo>
    {
        private List<TrackInfo> tracks = new List<TrackInfo> ();

        public LastfmTrackListModel () : base ()
        {
        }

        public override void Clear()
        {
            tracks.Clear ();
        }
        
        public override void Reload()
        {
            OnReloaded ();
        }

        public void Add (TrackInfo track)
        {
            tracks.Add (track);
        }

        public void Remove (TrackInfo track)
        {
            tracks.Remove (track);
        }

        public bool Contains (TrackInfo track)
        {
            return IndexOf (track) != -1;
        }
    
        public override TrackInfo this[int index] {
            get { return (index < tracks.Count) ? tracks[index] : null; }
        }

        public override int Count { 
            get { return tracks.Count; }
        }
        
        public override int IndexOf (TrackInfo track)
        {
            return tracks.IndexOf (track);
        }

        public IEnumerator<TrackInfo> GetEnumerator ()
        {
            foreach (TrackInfo track in tracks) {
                yield return track;
            }
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
    }
}
