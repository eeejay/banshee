/***************************************************************************
 *  MtpDeviceId.cs
 *
 *  Copyright (C) 2006 Novell and Patrick van Staveren
 *  Written by Patrick van Staveren (trick@vanstaveren.us)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

namespace Banshee.Dap.Mtp
{
    public class DeviceId
    {
        private static DeviceId [] device_id_list = {
            /*
                Device list taken from libgphoto2/camlibs/library.c starting around line 580
                Last updated: 2006-05-07

                Please note: just beause your device exists in this list, does not mean
                it will work (flawlessly) with Banshee.
                Many players are still in the progress of being supported.
                
                There are known issues with the iRiver players (2006-May)

                         Full Device Name                               Display/Short Name      Vendor ID   Product ID */
            new DeviceId("iRiver T10 (alternative)",                    "iRiver T10",           0x4102,     0x1113),
            new DeviceId("iRiver T20 FM",                               "iRiver T20 FM",        0x4102,     0x1114),
            new DeviceId("iRiver U10",                                  "iRiver U10",           0x4102,     0x1116),
            new DeviceId("iRiver T10",                                  "iRiver T10",           0x4102,     0x1117),
            new DeviceId("iRiver T20",                                  "iRiver T20",           0x4102,     0x1118),
            new DeviceId("iRiver T30",                                  "iRiver T30",           0x4102,     0x1119),
            new DeviceId("iRiver H10",                                  "iRiver H10",           0x4102,     0x2102),
            new DeviceId("iRiver Portable Media Center",                "iRiver PMC",           0x1006,     0x4002),
            new DeviceId("iRiver Portable Media Center",                "iRiver PMC",           0x1006,     0x4003),
            new DeviceId("Phillips HDD6320",                            "HDD6320",              0x0471,     0x01eb),
            new DeviceId("Phillips HDD6130/17",                         "HDD6130/17",           0x0471,     0x014c),
            new DeviceId("Creative Zen Vision",                         "Zen Vision",           0x041e,     0x411f),
            new DeviceId("Creative Portable Media Center",              "Media Center",         0x041e,     0x4123),
            new DeviceId("Creative Zen Xtra",                           "Zen Xtra",             0x041e,     0x4128),
            new DeviceId("Second Generation Dell DJ",                   "Dell DJ",              0x041e,     0x412f),
            new DeviceId("Creative Zen Micro",                          "Zen Micro",            0x041e,     0x4130),
            new DeviceId("Creative Zen Touch",                          "Zen Touch",            0x041e,     0x4131),
            new DeviceId("Dell Pocket DJ",                              "Pocket DJ",            0x041e,     0x4132),
            new DeviceId("Creative Zen Sleek",                          "Zen Sleek",            0x041e,     0x4137),
            new DeviceId("Creative Zen MicroPhoto",                     "Zen MicroPhoto",       0x041e,     0x413c),
            new DeviceId("Creative Zen Sleek Photo",                    "Zen Sleek Photo",      0x041e,     0x413d),
            new DeviceId("Creative Zen Vision:M",                       "Zen Vision:M",         0x041e,     0x413e),
            new DeviceId("Samsung YP-T7J",                              "Samsung YP-T7J",       0x04e8,     0x5047),
            new DeviceId("Samsung YH-999 Portable Media Center",        "Samsung YH-999",       0x04e8,     0x5a0f),
            new DeviceId("Samsung YH-925",                              "Samsung YH-925",       0x04e8,     0x502f),
            new DeviceId("Dell DJ Ditty",                               "DJ Ditty",             0x413c,     0x4500),
            new DeviceId("Toshiba Gigabeat",                            "Gigabeat",             0x0930,     0x000c),
            new DeviceId("JVC Aleno XA-HD500",                          "JVC Aleno",            0x04f1,     0x6105),
            new DeviceId("Intel Bandon Portable Media Center",          "Intel Bandon",         0x045e,     0x00c9),
            new DeviceId("Sandisk Sansa e200",                          "Sansa e200",           0x0781,     0x7420)
        };

        public static DeviceId [] ListAll {
            get {
                return device_id_list;
            }
        }

        public static bool IsMtpDevice(short vendorId, short productId) {
            return GetDeviceId(vendorId, productId) != null;
        }

        public static DeviceId GetDeviceId(short vendorId, short productId)
        {
            foreach(DeviceId id in ListAll) {
                if(id.VendorId == vendorId && id.ProductId == productId) {
                    return id;
                }
            }
            return null;
        }

        private string name;
        private string display_name;
        private short vendor_id;
        private short product_id;

        private DeviceId(string name, string displayName, short vendorId, short productId)
        {
            this.name = name;
            this.display_name = displayName;
            this.vendor_id = vendorId;
            this.product_id = productId;
        }

        public string Name {
            get {
                return name;
            }
        }

        public string DisplayName {
            get {
                return display_name;
            }
        }

        public short VendorId {
            get {
                return vendor_id;
            }
        }

        public short ProductId {
            get {
                return product_id;
            }
        }
    }
}
