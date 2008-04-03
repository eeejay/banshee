//
// RemovableSource.cs
//
// Author:
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
using System.Collections.Generic;
using System.Threading;
using Mono.Unix;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Hardware;

namespace Banshee.Dap.MassStorage
{
    public abstract class RemovableSource : PrimarySource, IUnmapableSource, IDisposable
    {
        public RemovableSource (string name, string generic_name, string uuid) : base (generic_name, name, uuid, 175)
        {
            Name = name;

            Properties.SetString ("UnmapSourceActionIconName", "media-eject");
            AfterInitialized ();
        }

        public override string GenericName {
            get { return base.GenericName; }
            set {
                base.GenericName = value;
                Properties.SetString ("DeleteTracksActionLabel", String.Format (Catalog.GetString ("Delete From {0}"), value));
                Properties.SetString ("UnmapSourceActionLabel", String.Format (Catalog.GetString ("Eject {0}"), value));
            }
        }

        public void Dispose ()
        {
            ServiceManager.SourceManager.RemoveSource (this);
        }
        
        public override bool CanRemoveTracks {
            get { return !IsReadOnly; }
        }

        public override bool CanDeleteTracks {
            get { return !IsReadOnly; }
        }

        /*public double DiskUsageFraction {
            get { return (double)device.StorageUsed / (double)device.StorageCapacity; }
        }*/

#region IUnmapableSource Implementation

        public bool Unmap ()
        {
            DatabaseTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
            if (track != null && track.PrimarySourceId == this.DbId) {
                ServiceManager.PlayerEngine.Close ();
            }
            
            SetStatus (String.Format (Catalog.GetString ("Ejecting {0}..."), GenericName), false);
        
            ThreadPool.QueueUserWorkItem (delegate {
                try {
                    Eject ();

                    ThreadAssist.ProxyToMain (delegate {
                        Dispose ();
                    });
                } catch (Exception e) {
                    ThreadAssist.ProxyToMain (delegate {
                        SetStatus (String.Format (Catalog.GetString ("Could not eject {0}: {1}"), GenericName, e.Message), true);
                    });
                    
                    Log.Exception (e);
                }
            });
            
            return true;
        }

        public bool CanUnmap {
            get { return true; }
        }

        public bool ConfirmBeforeUnmap {
            get { return false; }
        }

#endregion

#region Members Subclasses Should Override

        protected virtual void Eject ()
        {
        }

        protected abstract bool IsReadOnly { get; }

#endregion

    }
}
