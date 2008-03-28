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
        public static BlockDevice Resolve<T> (Hal.Device device) where T : IBlockDevice
        {
            if (device["info.category"] == "storage" && device.QueryCapability ("block") && 
                device.PropertyExists ("block.device")) {
                if (typeof (T) == typeof (ICdromDevice)) {
                    return CdromDevice.Resolve (device);
                } else if (typeof (T) == typeof (IDiskDevice)) {
                    return DiskDevice.Resolve (device);
                }
                
                return (BlockDevice)CdromDevice.Resolve (device) ?? (BlockDevice)DiskDevice.Resolve (device);
            }
            
            return null;
        }
        
        internal BlockDevice (Hal.Device device) : base (device)
        {
        }
        
        public string DeviceNode {
            get { return HalDevice.Udi; }
        }
        
        public IEnumerable<IVolume> Volumes {
            get { return this; }
        }
        
        public IEnumerator<IVolume> GetEnumerator ()
        {
            return null;
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
