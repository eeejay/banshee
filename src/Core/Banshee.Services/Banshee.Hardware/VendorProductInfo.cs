//
// VendorProductInfo.cs
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

namespace Banshee.Hardware
{
    public struct VendorProductInfo
    {
        public static VendorProductInfo Zero;

        public VendorProductInfo (string vendorName, string productName, int vendorProductId)
        {
            vendor_name = vendorName;
            product_name = productName;
            vendor_id = (short)(vendorProductId >> 16);
            product_id = (short)(vendorProductId & 0xffff);
        }

        public VendorProductInfo (string vendorName, string productName,
            short vendorId, short productId)
        {
            vendor_name = vendorName;
            product_name = productName;
            vendor_id = vendorId;
            product_id = productId;
        }

        public override int GetHashCode()
        {
            return VendorProductId;
        }

        public override string ToString()
        {
            return String.Format ("{0}, {1}; Vendor ID = {2}, Product ID = {3} ({4})",
                VendorName, ProductName, VendorId, ProductId, VendorProductId);
        }

        private string vendor_name;
        public string VendorName {
            get { return vendor_name; }
            set { vendor_name = value; }
        }

        private string product_name;
        public string ProductName {
            get { return product_name; }
            set { product_name = value; }
        }

        private short vendor_id;
        public short VendorId {
            get { return vendor_id; }
            set { vendor_id = value; }
        }

        private short product_id;
        public short ProductId {
            get { return product_id; }
            set { product_id = value; }
        }

        public int VendorProductId {
            get { return (int)(vendor_id << 16) | product_id; }
            set {
                vendor_id = (short)(value >> 16);
                product_id = (short)(value & 0xffff);
            }
        }
    }
}
