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
using Mono.Addins;

using Banshee.Hardware;

namespace Banshee.Dap.MassStorage
{
    internal static class DeviceMapper
    {
        public static MassStorageDevice Map (MassStorageSource source)
        {
            foreach (VendorProductDeviceNode node in AddinManager.GetExtensionNodes (
                "/Banshee/Dap/MassStorage/Device")) {
                short vendor_id = (short)source.UsbDevice.VendorId;
                short product_id = (short)source.UsbDevice.ProductId;
                
                if (node.Matches (vendor_id, product_id)) {
                    MassStorageDevice device = (MassStorageDevice)node.CreateInstance (); 
                    device.VendorProductInfo = new VendorProductInfo (
                        node.VendorName, node.ProductName,
                        vendor_id, product_id);
                    device.Source = source;
                    return device;
                }
            }
            
            return new MassStorageDevice (source);
        }
    }
}
