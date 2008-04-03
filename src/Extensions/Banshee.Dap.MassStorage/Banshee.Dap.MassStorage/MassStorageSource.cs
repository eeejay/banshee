//
// MassStorageSource.cs
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
    public class MassStorageSource : DapSource
    {
        protected IVolume volume;
        public IVolume Volume {
            get { return volume; }
        }

        public MassStorageSource (IVolume volume) : base (Catalog.GetString ("Media"), volume.Name ?? "Media" , volume.Uuid)
        {
            this.volume = volume;

            Properties.SetStringList ("Icon.Name", "harddrive");

            // TODO differentiate between Audio Players and normal Disks, and include the size, eg "2GB Audio Player"?
            //GenericName = Catalog.GetString ("Audio Player");

            // TODO construct device-specific icon name as preferred icon
            //Properties.SetStringList ("Icon.Name", "media-player");

            ThreadPool.QueueUserWorkItem (delegate {
                DatabaseImportManager importer = new DatabaseImportManager (this);
                importer.KeepUserJobHidden = true;
                SetStatus (String.Format (Catalog.GetString ("Loading {0}"), Name), false);
                importer.ImportFinished += delegate  { HideStatus (); };
                importer.QueueSource (new string [] { volume.MountPoint });
            });
        }
        
        public override long BytesUsed {
            get { return BytesCapacity - volume.Available; }
        }
        
        public override long BytesCapacity {
            get { return (long) volume.Capacity; }
        }

        protected override bool IsReadOnly {
            get { return volume.IsReadOnly; }
        }

        protected override void Eject ()
        {
            if (volume.CanUnmount)
                volume.Unmount ();

            if (volume.CanEject)
                volume.Eject ();
        }
    }
}
