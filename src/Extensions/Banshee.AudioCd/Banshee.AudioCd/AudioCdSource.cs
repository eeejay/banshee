//
// AudioCdSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Mono.Unix;

using Hyena;
using Banshee.Sources;
using Banshee.Collection;

namespace Banshee.AudioCd
{
    public class AudioCdSource : Source, ITrackModelSource, IUnmapableSource, IDisposable
    {
        private AudioCdService service;
        private AudioCdDisc disc;
        private MemoryTrackListModel track_model;
        
        public AudioCdSource (AudioCdService service, AudioCdDisc disc) 
            : base (Catalog.GetString ("Audio CD"), disc.Title, 200)
        {
            this.service = service;
            this.disc = disc;
            
            track_model = new MemoryTrackListModel ();
            
            Properties.SetStringList ("Icon.Name", "media-cdrom", "gnome-dev-cdrom-audio", "source-cd-audio");
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Eject Disc"));
        }
        
        public void Dispose ()
        {
        }
        
        public AudioCdDisc Disc {
            get { return disc; }
        }

#region Source Overrides

        public override void Rename (string newName)
        {
            base.Rename (newName);
        }

        public override bool CanSearch {
            get { return false; }
        }
        
        protected override string TypeUniqueId {
            get { return "audio-cd"; }
        }
        
        public override int Count {
            get { return track_model.Count; }
        }

#endregion
        
#region ITrackModelSource Implementation

        public TrackListModel TrackModel {
            get { return track_model; }
        }

        public AlbumListModel AlbumModel {
            get { return null; }
        }

        public ArtistListModel ArtistModel {
            get { return null; }
        }

        public void Reload ()
        {
            track_model.Reload ();
        }

        public void RemoveSelectedTracks ()
        {
        }

        public void DeleteSelectedTracks ()
        {
        }

        public bool CanRemoveTracks {
            get { return false; }
        }

        public bool CanDeleteTracks {
            get { return false; }
        }

        public bool ConfirmRemoveTracks {
            get { return false; }
        }
        
        public bool ShowBrowser {
            get { return false; }
        }
        
        public bool HasDependencies {
            get { return false; }
        }

#endregion

#region IUnmapableSource Implementation

        public bool Unmap ()
        {
            System.Threading.ThreadPool.QueueUserWorkItem (delegate {
                try {
                    disc.Volume.Unmount ();
                    disc.Volume.Eject ();
                } catch (Exception e) {
                    Log.Error (Catalog.GetString ("Could not eject Audio CD"), e.Message, true);
                    Log.Exception (e);
                }
            });
            
            service.UnmapDiscVolume (disc.Volume.Uuid);
            
            return true;
        }

        public bool CanUnmap {
            get { return true; }
        }

        public bool ConfirmBeforeUnmap {
            get { return false; }
        }

#endregion

    }
}
