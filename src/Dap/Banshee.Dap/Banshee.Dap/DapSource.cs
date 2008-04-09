//
// DapSource.cs
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

namespace Banshee.Dap
{
    public abstract class DapSource : RemovableSource
    {
        protected IDevice device;

        protected DapSource () : base ()
        {
        }

        protected override void Initialize ()
        {
            base.Initialize ();

            if (!String.IsNullOrEmpty (device.Vendor) && !String.IsNullOrEmpty (device.Product)) {
                string device_icon_name = (device.Vendor.Trim () + "-" + device.Product.Trim ()).Replace (' ', '-').ToLower ();
                Properties.SetStringList ("Icon.Name", device_icon_name, FallbackIcon);
            } else {
                Properties.SetStringList ("Icon.Name", FallbackIcon);
            }

            GenericName = IsMediaDevice ? Catalog.GetString ("Audio Player") : Catalog.GetString ("Media Device");
        }

        public bool Resolve (IDevice device)
        {
            this.device = device;
            type_unique_id = device.Uuid;
            return Initialize (device);
        }

        protected abstract bool Initialize (IDevice device);

        protected virtual bool IsMediaDevice {
            get { return device.MediaCapabilities != null; }
        }

        protected virtual string FallbackIcon {
            get { return IsMediaDevice ? "multimedia-player" : "harddrive"; }
        }

        protected IDeviceMediaCapabilities MediaCapabilities {
            get { return device.MediaCapabilities; }
        }
    }
}
