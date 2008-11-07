//
// DeviceMapper.cs
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

namespace Banshee.Dap.MassStorage
{
    internal static class DeviceMapper
    {
        private static DeviceMap [] devices = new DeviceMap [] {
            new DeviceMap (typeof (AndroidDevice), "HTC", "T-Mobile G1", 0x0bb4, 0x0c01)
        };
    
        public static MassStorageDevice Map (MassStorageSource source)
        {
            foreach (DeviceMap map in devices) {
                if (map.VendorProductInfo.VendorId == source.UsbDevice.VendorId && 
                    map.VendorProductInfo.ProductId == source.UsbDevice.ProductId) {
                    return (CustomMassStorageDevice)Activator.CreateInstance (map.DeviceType, 
                        new object [] { map.VendorProductInfo, source });
                }
            }
            
            return new MassStorageDevice (source);
        }
        
        private struct DeviceMap
        {
            public DeviceMap (Type type, VendorProductInfo vpi)
            {
                DeviceType = type;
                VendorProductInfo = vpi;
            }
            
            public DeviceMap (Type type, string vendor, string product, short vendorId, short productId)
            {
                DeviceType = type;
                VendorProductInfo = new VendorProductInfo (vendor, product, vendorId, productId);
            }
            
            public VendorProductInfo VendorProductInfo;
            public Type DeviceType;
        }
    }
}
