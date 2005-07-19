/***************************************************************************
 *  IpodCore.cs
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
using System.Collections;
using IPod;

namespace Sonance
{
	public class IpodCore
	{
		private static IpodCore instance;
		private DeviceEventListener listener;
		private Hashtable devices;
	
		public static IpodCore Instance
		{
			get {
				if(instance == null) 
					instance = new IpodCore();
				return instance;
			}
		}
	
		public IpodCore()
		{
			listener = new DeviceEventListener();
			listener.DeviceAdded += OnDeviceAdded;
			listener.DeviceRemoved += OnDeviceRemoved;
			
			devices = new Hashtable();
			
			foreach(Device device in Device.ListDevices())
				devices[device.VolumeId] = device;
		}
		
		private void OnDeviceAdded(object o, DeviceAddedArgs args)
		{
			if(devices[args.Udi] != null)
				devices[args.Udi] = new Device(args.Udi);
		}
		
		
		private void OnDeviceRemoved(object o, DeviceRemovedArgs args)
		{
			if(devices[args.Udi] != null)
				devices.Remove(args.Udi);
		}
		
		public void ListAll()
		{
			foreach(Device device in Device.ListDevices())
				device.Debug();
		}
	}
}
