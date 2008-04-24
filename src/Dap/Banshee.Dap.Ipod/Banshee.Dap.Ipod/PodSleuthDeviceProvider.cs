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
            
            if (disk_device == null || device.MediaCapabilities == null || !device.MediaCapabilities.IsType ("ipod")) {
                return device;
            }
            
            foreach (IVolume volume in disk_device.Volumes) {
                if (volume.PropertyExists ("org.podsleuth.version")) {
                    try {
                        return (T)((IDevice)(new PodSleuthDevice (volume)));
                    } catch (Exception e) {
                        Hyena.Log.Exception (e);
                        return device;
                    }
                }
            }
            
            return device;
        }
    }
}
