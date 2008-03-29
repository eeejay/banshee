//
// BlockDevice.cs
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
using System.Collections;
using System.Collections.Generic;

using Banshee.Hardware;

namespace Banshee.HalBackend
{
    public abstract class BlockDevice : Device, IBlockDevice
    {
        public static BlockDevice Resolve<T> (Hal.Manager manager, Hal.Device device) where T : IBlockDevice
        {
            if (device["info.category"] == "storage" && device.QueryCapability ("block") && 
                device.PropertyExists ("block.device")) {
                if (typeof (T) == typeof (ICdromDevice)) {
                    return CdromDevice.Resolve (manager, device);
                } else if (typeof (T) == typeof (IDiskDevice)) {
                    return DiskDevice.Resolve (manager, device);
                }
                
                return (BlockDevice)CdromDevice.Resolve (manager, device) 
                    ?? (BlockDevice)DiskDevice.Resolve (manager, device);
            }
            
            return null;
        }
        
        protected BlockDevice (Hal.Manager manager, Hal.Device device) : base (manager, device)
        {
        }
        
        public string DeviceNode {
            get { return HalDevice["block.device"]; }
        }
        
        public IEnumerable<IVolume> Volumes {
            get { return this; }
        }
        
        public IEnumerator<IVolume> GetEnumerator ()
        {
            foreach (Hal.Device hal_device in HalDevice.GetChildrenAsDevice (HalManager)) {
                Volume volume = Volume.Resolve (this, HalManager, hal_device);
                if (volume != null) {
                    yield return volume;
                }
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
        
        public override string ToString ()
        {
            return DeviceNode;
        }
    }
}
