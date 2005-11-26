/***************************************************************************
 *  HalCallbacks.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
 
using System;
using System.Runtime.InteropServices;

namespace Hal
{
    // Raw HAL Callbacks
    internal delegate void DeviceAddedCallback(IntPtr ctx, IntPtr udi);
    internal delegate void DeviceRemovedCallback(IntPtr ctx, IntPtr udi);
    internal delegate void DeviceNewCapabilityCallback(IntPtr ctx, IntPtr udi, IntPtr capability);
    internal delegate void DeviceLostCapabilityCallback(IntPtr ctx, IntPtr udi, IntPtr capability);
    internal delegate void DevicePropertyModifiedCallback(IntPtr ctx, IntPtr udi, 
        IntPtr key, bool is_removed, bool is_added);
    internal delegate void DeviceConditionCallback(IntPtr ctx, IntPtr udi, 
        IntPtr condition_name, IntPtr condition_details);
    
    // Managed Event Handlers
    public delegate void DeviceAddedHandler(object o, DeviceAddedArgs args);
    public delegate void DeviceRemovedHandler(object o, DeviceRemovedArgs args);
    public delegate void DeviceNewCapabilityHandler(object o, DeviceNewCapabilityArgs args);
    public delegate void DeviceLostCapabilityHandler(object o, DeviceLostCapabilityArgs args);
    public delegate void DevicePropertyModifiedHandler(object o, DevicePropertyModifiedArgs args);
    public delegate void DeviceConditionHandler(object o, DeviceConditionArgs args);
        
    public class DeviceAddedArgs : EventArgs 
    {
        public Device Device;
    }
    
    public class DeviceRemovedArgs : EventArgs
    {
        public Device Device;
    }
    
    public class DeviceNewCapabilityArgs : EventArgs
    {
        public Device Device;
        public string Capability;
    }
    
    public class DeviceLostCapabilityArgs : EventArgs
    {
        public Device Device;
        public string Capability;
    }
    
    public class DevicePropertyModifiedArgs : EventArgs
    {
        public Device Device;
        public string Key;
        public bool IsRemoved;
        public bool IsAdded;
    }
    
    public class DeviceConditionArgs : EventArgs
    {
        public Device Device;
        public string ConditionName;
        public string ConditionDetails;
    }
}
