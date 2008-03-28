//
// Volume.cs
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

using Banshee.Hardware;

namespace Banshee.HalBackend
{
    public class Volume : Device, IVolume
    {
        public static Volume Resolve (BlockDevice parent, Hal.Manager manager, Hal.Device device)
        {
            if (!device.IsVolume) {
                return null;
            }
            
            return parent is ICdromDevice 
                ? DiscVolume.Resolve (parent, manager, device)
                : new Volume (parent, manager, device);
        }
        
        private BlockDevice parent;
        
        protected Volume (BlockDevice parent, Hal.Manager manager, Hal.Device device) : base (manager, device)
        {
            this.parent = parent;
        }

        public string MountPoint {
            get { return HalDevice["volume.mount_point"]; }
        }

        public bool IsMounted {
            get { return HalDevice.GetPropertyBoolean ("volume.is_mounted"); }
        }

        public bool IsMountedReadOnly {
            get { return HalDevice.GetPropertyBoolean ("volume.is_mounted_read_only"); }
        }

        public ulong Capacity {
            get { return HalDevice.GetPropertyUInt64 ("volume.size"); }
        }

        public long Available {
            get {
                if (!IsMounted) {
                    return -1;
                }
                
                try {
                    Mono.Unix.Native.Statvfs statvfs_info;
                    if (Mono.Unix.Native.Syscall.statvfs (MountPoint, out statvfs_info) != -1) {
                        return ((long)statvfs_info.f_bavail) * ((long)statvfs_info.f_bsize);
                    }
                } catch {
                }
                
                return -1;
            }
        }
        
        public IBlockDevice Parent {
            get { return parent; }
        }
        
        public override string ToString ()
        {
            if (IsMounted) {
                return String.Format ("`{0}': mounted {1} volume at {2} with {3} bytes free (of {4})", 
                    Name, IsMountedReadOnly ? "read only" : "read/write", MountPoint, Available, Capacity);
            }
            
            return String.Format ("`{0}': not mounted (capacity: {1} bytes)", Name, Capacity);
        }
    }
}
