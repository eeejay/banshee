using System;
using Hal;
using Banshee.Base;

namespace Banshee.Dap
{
    public class DeviceEventListener
    {
        public DeviceEventListener () 
        {
            HalCore.DeviceAdded += delegate (object o, DeviceAddedArgs args) {
                //args.Device.Print();
            };
            
            HalCore.DeviceRemoved += delegate (object o, DeviceRemovedArgs args) {
                //args.Device.Print();
            };
        }
    }
}
