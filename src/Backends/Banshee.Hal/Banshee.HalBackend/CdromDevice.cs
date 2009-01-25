//
// CdromDevice.cs
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
using System.Runtime.InteropServices;
using Mono.Unix;

using Banshee.Hardware;

namespace Banshee.HalBackend
{
    public class CdromDevice : BlockDevice, ICdromDevice
    {
        public static CdromDevice Resolve (Hal.Manager manager, Hal.Device device)
        {
            if (device["storage.drive_type"] == "cdrom") {
                return new CdromDevice (manager, device);
            }
            
            return null;
        }
        
        private CdromDevice (Hal.Manager manager, Hal.Device device) : base (manager, device)
        {
        }
        
        [DllImport ("libc")]
        private static extern int ioctl (int device, IoctlOperation request, bool lockdoor); 

        private enum IoctlOperation {
            LockDoor = 0x5329
        }
        
        private bool is_door_locked = false;
        
        private bool LockDeviceNode (string device, bool lockdoor)
        {
            try {
                using (UnixStream stream = (new UnixFileInfo (device)).Open (
                    Mono.Unix.Native.OpenFlags.O_RDONLY | 
                    Mono.Unix.Native.OpenFlags.O_NONBLOCK)) {
                    bool success = ioctl (stream.Handle, IoctlOperation.LockDoor, lockdoor) == 0;
                    is_door_locked = success && lockdoor;
                    return success;
                }
            } catch {
                return false;
            }
        }
        
        public bool LockDoor ()
        {
            lock (this) {
                return LockDeviceNode (DeviceNode, true);
            }
        }
        
        public bool UnlockDoor ()
        {
            lock (this) {
                return LockDeviceNode (DeviceNode, false);
            }
        }
        
        // FIXME: This is incredibly lame, there must be a way to query the
        // device itself rather than hackisly attempting to keep track of it
        public bool IsDoorLocked {
            get { return is_door_locked; }
        }
    }
}
