//
// PodSleuthDeviceProvider.cs
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

namespace Banshee.Dap.Ipod
{
    public class PodSleuthDeviceProvider : ICustomDeviceProvider
    {
        public T GetCustomDevice<T> (T device) where T : class, IDevice
        {
            IDiskDevice disk_device = device as IDiskDevice;
            IVolume volume = device as IVolume;
            
            if (volume != null && CheckStorageCaps (volume.Parent) && CheckVolume (volume)) {
                return AsPodSleuthDevice<T> (volume, device);
            } else if (disk_device == null || !CheckStorageCaps (device)) {
                return device;
            }
            
            foreach (IVolume child_volume in disk_device.Volumes) {
                if (CheckVolume (child_volume)) {
                    return AsPodSleuthDevice<T> (child_volume, device);
                }
            }
            
            return device;
        }
        
        public void Dispose ()
        {
        }
        
        private T AsPodSleuthDevice<T> (IVolume volume, T original)
        {
            try {
                return (T)((IDevice)(new PodSleuthDevice (volume)));
            } catch (Exception e) {
                Hyena.Log.Exception (e);
                return original;
            }
        }
        
        private bool CheckStorageCaps (IDevice device)
        {
            return device != null && device.MediaCapabilities != null && device.MediaCapabilities.IsType ("ipod");
        }
        
        private bool CheckVolume (IVolume volume)
        {
            return volume.PropertyExists ("org.podsleuth.version");
        }
    }
}
